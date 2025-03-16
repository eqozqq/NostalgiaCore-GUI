using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NostalgiaCoreGUI
{
    public class PlayerInfo
    {
        public string Username { get; set; }
        public string PositionLevel { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public string Gamemode { get; set; }
        public string Health { get; set; }
        public string LastIP { get; set; }
        public string LastID { get; set; }
    }

    public partial class ServerMainForm : Form
    {
        private string serverPath;
        private Process serverProcess;
        private bool isServerRunning = false;
        private Thread outputReaderThread;
        private bool continueReading = true;
        private TextBox commandInputBox;
        private RichTextBox consoleBox;
        private Button startStopButton;
        private TabControl tabControl;
        private TabPage mainTabPage;
        private TabPage configTabPage;
        private TabPage playersTabPage;
        private Dictionary<string, TextBox> serverPropertiesControls = new Dictionary<string, TextBox>();
        private Dictionary<string, TextBox> extraPropertiesControls = new Dictionary<string, TextBox>();
        private Dictionary<string, ComboBox> serverPropertiesComboBoxes = new Dictionary<string, ComboBox>();
        private Dictionary<string, ComboBox> extraPropertiesComboBoxes = new Dictionary<string, ComboBox>();
        private Dictionary<string, string> serverPropertiesValues = new Dictionary<string, string>();
        private Dictionary<string, string> extraPropertiesValues = new Dictionary<string, string>();
        private bool isInitializing = true;
        private ListView playersListView;
        private ContextMenuStrip playersContextMenu;

        public ServerMainForm(string path)
        {
            serverPath = path;
            InitializeComponent();
            LoadServerProperties();
            LoadExtraProperties();
            isInitializing = false;
            this.Icon = new Icon(new MemoryStream(Properties.Resources.icon));
        }

        private void InitializeComponent()
        {
            this.Text = "NostalgiaCore Server";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(800, 500);
            this.FormClosing += ServerMainForm_FormClosing;
            mainTabPage = new TabPage("Main");
            configTabPage = new TabPage("Configuration");
            playersTabPage = new TabPage("Players");
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                TabPages = { mainTabPage, configTabPage, playersTabPage }
            };
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            InitializeMainTab();
            InitializeConfigTab();
            InitializePlayersTab();
            this.Controls.Add(tabControl);
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == configTabPage)
            {
                LoadServerProperties();
                LoadExtraProperties();
            }
            else if(tabControl.SelectedTab == playersTabPage)
            {
                LoadPlayers();
            }
        }

        private void InitializeMainTab()
        {
            TableLayoutPanel mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2
            };
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Panel leftPanel = new Panel { Dock = DockStyle.Fill };
            startStopButton = new Button
            {
                Text = "Start Server",
                Location = new Point(10, 10),
                Width = 130,
                Height = 40
            };
            startStopButton.Click += StartStopButton_Click;
            leftPanel.Controls.Add(startStopButton);
            TableLayoutPanel rightPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            consoleBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.White,
                Font = new Font("Consolas", 10),
                BorderStyle = BorderStyle.None
            };
            commandInputBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                BorderStyle = BorderStyle.FixedSingle,
                Enabled = false
            };
            commandInputBox.KeyDown += CommandInputBox_KeyDown;
            rightPanel.Controls.Add(consoleBox, 0, 0);
            rightPanel.Controls.Add(commandInputBox, 0, 1);
            mainPanel.Controls.Add(leftPanel, 0, 0);
            mainPanel.Controls.Add(rightPanel, 1, 0);
            mainTabPage.Controls.Add(mainPanel);
        }

        private void InitializeConfigTab()
        {
            TabControl configTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                TabPages = { new TabPage("Server Properties"), new TabPage("Extra Properties") }
            };
            configTabControl.SelectedIndexChanged += ConfigTabControl_SelectedIndexChanged;
            InitializeServerPropertiesTab(configTabControl.TabPages[0]);
            InitializeExtraPropertiesTab(configTabControl.TabPages[1]);
            configTabPage.Controls.Add(configTabControl);
        }

        private void InitializePlayersTab()
        {
            playersListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true
            };
            playersListView.Columns.Add("Player", -2, HorizontalAlignment.Left);
            playersListView.MouseClick += PlayersListView_MouseClick;
            playersListView.DoubleClick += PlayersListView_DoubleClick;
            playersContextMenu = new ContextMenuStrip();
            playersContextMenu.Items.Add("Ban").Click += BanMenu_Click;
            playersContextMenu.Items.Add("Unban").Click += UnbanMenu_Click;
            playersContextMenu.Items.Add("Kick").Click += KickMenu_Click;
            playersContextMenu.Items.Add("Op").Click += OpMenu_Click;
            playersContextMenu.Items.Add("De-op").Click += DeopMenu_Click;
            playersContextMenu.Items.Add("Whitelist add").Click += WhitelistAddMenu_Click;
            playersContextMenu.Items.Add("Whitelist remove").Click += WhitelistRemoveMenu_Click;
            playersContextMenu.Items.Add("Ban IP").Click += delegate(object s, EventArgs e) { BanIPMenu_Click(s, e); };
            playersContextMenu.Items.Add("Unban IP").Click += UnbanIPMenu_Click;
            playersListView.ContextMenuStrip = playersContextMenu;
            playersTabPage.Controls.Add(playersListView);
            LoadPlayers();
        }

        private void ConfigTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            TabControl tc = (TabControl)sender;
            if (tc.SelectedIndex == 0)
                LoadServerProperties();
            else if (tc.SelectedIndex == 1)
                LoadExtraProperties();
        }

        private void InitializeServerPropertiesTab(TabPage tabPage)
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            TableLayoutPanel table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 30,
                AutoSize = true,
                Padding = new Padding(10),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            AddServerPropertyRow(table, 0, "server-name", "Server Name:", "");
            AddServerPropertyRow(table, 1, "description", "Description:", "");
            AddServerPropertyRow(table, 2, "motd", "MOTD:", "");
            AddServerPropertyRow(table, 3, "server-ip", "Server IP:", "");
            AddServerPropertyRow(table, 4, "server-port", "Server Port:", "");
            AddServerPropertyRow(table, 5, "server-type", "Server Type:", "");
            AddServerPropertyRow(table, 6, "memory-limit", "Memory Limit:", "");
            AddServerPropertyComboBox(table, 7, "white-list", "White List:", new string[] { "on", "off" }, "");
            AddServerPropertyComboBox(table, 8, "announce-player-achievements", "Announce Achievements:", new string[] { "on", "off" }, "");
            AddServerPropertyRow(table, 9, "spawn-protection", "Spawn Protection:", "");
            AddServerPropertyRow(table, 10, "view-distance", "View Distance:", "");
            AddServerPropertyRow(table, 11, "max-players", "Max Players:", "");
            AddServerPropertyComboBox(table, 12, "allow-flight", "Allow Flight:", new string[] { "on", "off" }, "");
            AddServerPropertyComboBox(table, 13, "spawn-animals", "Spawn Animals:", new string[] { "on", "off" }, "");
            AddServerPropertyComboBox(table, 14, "spawn-mobs", "Spawn Mobs:", new string[] { "on", "off" }, "");
            AddServerPropertyRow(table, 15, "mobs-amount", "Mobs Amount:", "");
            AddServerPropertyRow(table, 16, "gamemode", "Gamemode:", "");
            AddServerPropertyComboBox(table, 17, "hardcore", "Hardcore:", new string[] { "on", "off" }, "");
            AddServerPropertyComboBox(table, 18, "pvp", "PvP:", new string[] { "on", "off" }, "");
            AddServerPropertyRow(table, 19, "difficulty", "Difficulty:", "");
            AddServerPropertyRow(table, 20, "generator-settings", "Generator Settings:", "");
            AddServerPropertyRow(table, 21, "level-name", "Level Name:", "");
            AddServerPropertyRow(table, 22, "level-seed", "Level Seed:", "");
            AddServerPropertyRow(table, 23, "level-type", "Level Type:", "");
            AddServerPropertyComboBox(table, 24, "enable-query", "Enable Query:", new string[] { "on", "off" }, "");
            AddServerPropertyComboBox(table, 25, "enable-rcon", "Enable RCON:", new string[] { "on", "off" }, "");
            AddServerPropertyRow(table, 26, "rcon.password", "RCON Password:", "");
            AddServerPropertyComboBox(table, 27, "auto-save", "Auto Save:", new string[] { "on", "off" }, "");
            AddServerPropertyComboBox(table, 28, "enable-mob-ai", "Enable Mob AI:", new string[] { "on", "off" }, "");
            AddServerPropertyRow(table, 29, "abort-reading-after-N-packets", "Abort Reading After N Packets:", "");
            panel.Controls.Add(table);
            tabPage.Controls.Add(panel);
        }

        private void InitializeExtraPropertiesTab(TabPage tabPage)
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            TableLayoutPanel table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 19,
                AutoSize = true,
                Padding = new Padding(10),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            AddExtraPropertyRow(table, 0, "version", "Version:", "");
            AddExtraPropertyComboBox(table, 1, "enable-nether-reactor", "Enable Nether Reactor:", new string[] { "on", "off" }, "");
            AddExtraPropertyComboBox(table, 2, "enable-explosions", "Enable Explosions:", new string[] { "on", "off" }, "");
            AddExtraPropertyComboBox(table, 3, "save-player-data", "Save Player Data:", new string[] { "on", "off" }, "");
            AddExtraPropertyComboBox(table, 4, "save-console-data", "Save Console Data:", new string[] { "on", "off" }, "");
            AddExtraPropertyComboBox(table, 5, "query-plugins", "Query Plugins:", new string[] { "on", "off" }, "");
            AddExtraPropertyComboBox(table, 6, "discord-msg", "Discord Messages:", new string[] { "on", "off" }, "");
            AddExtraPropertyComboBox(table, 7, "discord-ru-smiles", "Discord RU Smiles:", new string[] { "on", "off" }, "");
            AddExtraPropertyRow(table, 8, "discord-webhook-url", "Discord Webhook URL:", "");
            AddExtraPropertyRow(table, 9, "discord-bot-name", "Discord Bot Name:", "");
            AddExtraPropertyComboBox(table, 10, "despawn-mobs", "Despawn Mobs:", new string[] { "on", "off" }, "");
            AddExtraPropertyRow(table, 11, "mob-despawn-ticks", "Mob Despawn Ticks:", "");
            AddExtraPropertyComboBox(table, 12, "16x16x16_chunk_sending", "16x16x16 Chunk Sending:", new string[] { "on", "off" }, "");
            AddExtraPropertyComboBox(table, 13, "experimental-mob-ai", "Experimental Mob AI:", new string[] { "on", "off" }, "");
            AddExtraPropertyComboBox(table, 14, "force-20-tps", "Force 20 TPS:", new string[] { "on", "off" }, "");
            AddExtraPropertyComboBox(table, 15, "enable-mob-pushing", "Enable Mob Pushing:", new string[] { "on", "off" }, "");
            AddExtraPropertyComboBox(table, 16, "keep-chunks-loaded", "Keep Chunks Loaded:", new string[] { "on", "off" }, "");
            AddExtraPropertyComboBox(table, 17, "use-experimental-hotbar", "Use Experimental Hotbar:", new string[] { "on", "off" }, "");
            AddExtraPropertyComboBox(table, 18, "keep-items-on-death", "Keep Items On Death:", new string[] { "on", "off" }, "");
            panel.Controls.Add(table);
            tabPage.Controls.Add(panel);
        }

        private void AddServerPropertyRow(TableLayoutPanel table, int rowIndex, string propertyName, string label, string defaultValue)
        {
            Label propertyLabel = new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(5) };
            TextBox propertyTextBox = new TextBox { Text = defaultValue, Dock = DockStyle.Fill, Margin = new Padding(5), Tag = propertyName };
            propertyTextBox.TextChanged += ServerPropertyTextBox_TextChanged;
            serverPropertiesControls[propertyName] = propertyTextBox;
            table.Controls.Add(propertyLabel, 0, rowIndex);
            table.Controls.Add(propertyTextBox, 1, rowIndex);
        }

        private void AddServerPropertyComboBox(TableLayoutPanel table, int rowIndex, string propertyName, string label, string[] options, string defaultValue)
        {
            Label propertyLabel = new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(5) };
            ComboBox propertyComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Margin = new Padding(5), Tag = propertyName };
            propertyComboBox.Items.AddRange(options);
            if (!string.IsNullOrEmpty(defaultValue) && Array.IndexOf(options, defaultValue) >= 0)
                propertyComboBox.SelectedIndex = Array.IndexOf(options, defaultValue);
            else if (propertyComboBox.Items.Count > 0)
                propertyComboBox.SelectedIndex = 0;
            propertyComboBox.SelectedIndexChanged += ServerPropertyComboBox_SelectedIndexChanged;
            serverPropertiesComboBoxes[propertyName] = propertyComboBox;
            table.Controls.Add(propertyLabel, 0, rowIndex);
            table.Controls.Add(propertyComboBox, 1, rowIndex);
        }

        private void AddExtraPropertyRow(TableLayoutPanel table, int rowIndex, string propertyName, string label, string defaultValue)
        {
            Label propertyLabel = new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(5) };
            TextBox propertyTextBox = new TextBox { Text = defaultValue, Dock = DockStyle.Fill, Margin = new Padding(5), Tag = propertyName };
            propertyTextBox.TextChanged += ExtraPropertyTextBox_TextChanged;
            extraPropertiesControls[propertyName] = propertyTextBox;
            table.Controls.Add(propertyLabel, 0, rowIndex);
            table.Controls.Add(propertyTextBox, 1, rowIndex);
        }

        private void AddExtraPropertyComboBox(TableLayoutPanel table, int rowIndex, string propertyName, string label, string[] options, string defaultValue)
        {
            Label propertyLabel = new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(5) };
            ComboBox propertyComboBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Margin = new Padding(5), Tag = propertyName };
            propertyComboBox.Items.AddRange(options);
            if (!string.IsNullOrEmpty(defaultValue) && Array.IndexOf(options, defaultValue) >= 0)
                propertyComboBox.SelectedIndex = Array.IndexOf(options, defaultValue);
            else if (propertyComboBox.Items.Count > 0)
                propertyComboBox.SelectedIndex = 0;
            propertyComboBox.SelectedIndexChanged += ExtraPropertyComboBox_SelectedIndexChanged;
            extraPropertiesComboBoxes[propertyName] = propertyComboBox;
            table.Controls.Add(propertyLabel, 0, rowIndex);
            table.Controls.Add(propertyComboBox, 1, rowIndex);
        }

        private void ServerPropertyTextBox_TextChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;
            TextBox textBox = (TextBox)sender;
            string propertyName = (string)textBox.Tag;
            string propertyValue = textBox.Text;
            serverPropertiesValues[propertyName] = propertyValue;
            SaveServerProperties();
        }

        private void ServerPropertyComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;
            ComboBox comboBox = (ComboBox)sender;
            string propertyName = (string)comboBox.Tag;
            string propertyValue = comboBox.SelectedItem.ToString();
            serverPropertiesValues[propertyName] = propertyValue;
            SaveServerProperties();
        }

        private void ExtraPropertyTextBox_TextChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;
            TextBox textBox = (TextBox)sender;
            string propertyName = (string)textBox.Tag;
            string propertyValue = textBox.Text;
            extraPropertiesValues[propertyName] = propertyValue;
            SaveExtraProperties();
        }

        private void ExtraPropertyComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;
            ComboBox comboBox = (ComboBox)sender;
            string propertyName = (string)comboBox.Tag;
            string propertyValue = comboBox.SelectedItem.ToString();
            extraPropertiesValues[propertyName] = propertyValue;
            SaveExtraProperties();
        }

        private void LoadServerProperties()
        {
            isInitializing = true;
            serverPropertiesValues.Clear();
            string serverPropertiesPath = Path.Combine(serverPath, "server.properties");
            if (File.Exists(serverPropertiesPath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(serverPropertiesPath);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                            continue;
                        int separatorIndex = line.IndexOf('=');
                        if (separatorIndex > 0)
                        {
                            string key = line.Substring(0, separatorIndex).Trim();
                            string value = line.Substring(separatorIndex + 1).Trim();
                            serverPropertiesValues[key] = value;
                            if (serverPropertiesControls.ContainsKey(key))
                                serverPropertiesControls[key].Text = value;
                            else if (serverPropertiesComboBoxes.ContainsKey(key))
                            {
                                ComboBox comboBox = serverPropertiesComboBoxes[key];
                                int index = comboBox.Items.IndexOf(value);
                                if (index >= 0)
                                    comboBox.SelectedIndex = index;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading server.properties: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            foreach (var kvp in serverPropertiesControls)
                kvp.Value.Visible = serverPropertiesValues.ContainsKey(kvp.Key);
            foreach (var kvp in serverPropertiesComboBoxes)
                kvp.Value.Visible = serverPropertiesValues.ContainsKey(kvp.Key);
            isInitializing = false;
        }

        private void LoadExtraProperties()
        {
            isInitializing = true;
            extraPropertiesValues.Clear();
            string extraPropertiesPath = Path.Combine(serverPath, "extra.properties");
            if (File.Exists(extraPropertiesPath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(extraPropertiesPath);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                            continue;
                        int separatorIndex = line.IndexOf('=');
                        if (separatorIndex > 0)
                        {
                            string key = line.Substring(0, separatorIndex).Trim();
                            string value = line.Substring(separatorIndex + 1).Trim();
                            extraPropertiesValues[key] = value;
                            if (extraPropertiesControls.ContainsKey(key))
                                extraPropertiesControls[key].Text = value;
                            else if (extraPropertiesComboBoxes.ContainsKey(key))
                            {
                                ComboBox comboBox = extraPropertiesComboBoxes[key];
                                int index = comboBox.Items.IndexOf(value);
                                if (index >= 0)
                                    comboBox.SelectedIndex = index;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading extra.properties: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            foreach (var kvp in extraPropertiesControls)
                kvp.Value.Visible = extraPropertiesValues.ContainsKey(kvp.Key);
            foreach (var kvp in extraPropertiesComboBoxes)
                kvp.Value.Visible = extraPropertiesValues.ContainsKey(kvp.Key);
            isInitializing = false;
        }

        private void SaveServerProperties()
        {
            string serverPropertiesPath = Path.Combine(serverPath, "server.properties");
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("#Properties Config file");
                sb.AppendLine();
                sb.AppendLine("#" + DateTime.Now.ToString("ddd MMM dd HH:mm:ss 'GMT' yyyy"));
                sb.AppendLine();
                foreach (var pair in serverPropertiesValues)
                    sb.AppendLine(pair.Key + "=" + pair.Value);
                File.WriteAllText(serverPropertiesPath, sb.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving server.properties: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveExtraProperties()
        {
            string extraPropertiesPath = Path.Combine(serverPath, "extra.properties");
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("#Properties Config file");
                sb.AppendLine();
                sb.AppendLine("#" + DateTime.Now.ToString("ddd MMM dd HH:mm:ss 'GMT' yyyy"));
                sb.AppendLine();
                foreach (var pair in extraPropertiesValues)
                    sb.AppendLine(pair.Key + "=" + pair.Value);
                File.WriteAllText(extraPropertiesPath, sb.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving extra.properties: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CommandInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && isServerRunning && serverProcess != null && !serverProcess.HasExited)
            {
                string command = commandInputBox.Text;
                if (!string.IsNullOrEmpty(command))
                {
                    try
                    {
                        serverProcess.StandardInput.WriteLine(command);
                        serverProcess.StandardInput.Flush();
                        commandInputBox.Clear();
                        AppendToConsole("> " + command + "\r\n", Color.Cyan);
                    }
                    catch (Exception ex)
                    {
                        AppendToConsole("Error sending command: " + ex.Message + "\r\n", Color.Red);
                    }
                }
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void StartStopButton_Click(object sender, EventArgs e)
        {
            if (!isServerRunning)
                StartServer();
            else
                StopServer();
        }

        private void StartServer()
        {
            try
            {
                string startBatPath = Path.Combine(serverPath, "start.cmd");
                if (!File.Exists(startBatPath))
                {
                    AppendToConsole("Error: start.cmd not found in server directory.\r\n", Color.Red);
                    return;
                }
                serverProcess = new Process();
                serverProcess.StartInfo.FileName = startBatPath;
                serverProcess.StartInfo.WorkingDirectory = serverPath;
                serverProcess.StartInfo.UseShellExecute = false;
                serverProcess.StartInfo.RedirectStandardOutput = true;
                serverProcess.StartInfo.RedirectStandardError = true;
                serverProcess.StartInfo.RedirectStandardInput = true;
                serverProcess.StartInfo.CreateNoWindow = true;
                serverProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                serverProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                serverProcess.EnableRaisingEvents = true;
                serverProcess.Exited += ServerProcess_Exited;
                serverProcess.Start();
                isServerRunning = true;
                startStopButton.Text = "Stop Server";
                commandInputBox.Enabled = true;
                AppendToConsole("Server starting...\r\n", Color.Green);
                continueReading = true;
                outputReaderThread = new Thread(ReadServerOutput);
                outputReaderThread.IsBackground = true;
                outputReaderThread.Start();
            }
            catch (Exception ex)
            {
                AppendToConsole("Error starting server: " + ex.Message + "\r\n", Color.Red);
            }
        }

        private void StopServer()
        {
            if (serverProcess != null && !serverProcess.HasExited)
            {
                try
                {
                    AppendToConsole("Stopping server...\r\n", Color.Yellow);
                    serverProcess.StandardInput.WriteLine("stop");
                    serverProcess.StandardInput.Flush();
                    if (!serverProcess.WaitForExit(10000))
                        serverProcess.Kill();
                }
                catch (Exception ex)
                {
                    AppendToConsole("Error stopping server: " + ex.Message + "\r\n", Color.Red);
                }
            }
        }

        private void ServerProcess_Exited(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ServerProcess_Exited(sender, e)));
                return;
            }
            isServerRunning = false;
            startStopButton.Text = "Start Server";
            commandInputBox.Enabled = false;
            AppendToConsole("Server stopped.\r\n", Color.Yellow);
            continueReading = false;
        }

        private void ReadServerOutput()
        {
            char[] buffer = new char[1024];
            int bytesRead;
            try
            {
                while (continueReading && !serverProcess.HasExited)
                {
                    bytesRead = serverProcess.StandardOutput.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string output = new string(buffer, 0, bytesRead);
                        this.Invoke(new Action(() => { AppendToConsole(output, GetColorForOutput(output)); }));
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!this.IsDisposed && continueReading)
                    this.Invoke(new Action(() => AppendToConsole("Error reading server output: " + ex.Message + "\r\n", Color.Red)));
            }
        }

        private Color GetColorForOutput(string output)
        {
            if (output.Contains("ERROR") || output.Contains("Exception") || output.Contains("Error"))
                return Color.Red;
            else if (output.Contains("WARNING") || output.Contains("Warning"))
                return Color.Yellow;
            else if (output.Contains("INFO") || output.Contains("Info"))
                return Color.Cyan;
            else if (output.Contains("Player connected:") || output.Contains("joined the game"))
                return Color.LightGreen;
            else if (output.Contains("Player disconnected:") || output.Contains("left the game"))
                return Color.Orange;
            else
                return Color.White;
        }

        private void AppendToConsole(string text, Color color)
        {
            consoleBox.SelectionStart = consoleBox.TextLength;
            consoleBox.SelectionLength = 0;
            consoleBox.SelectionColor = color;
            consoleBox.AppendText(text);
            consoleBox.SelectionStart = consoleBox.TextLength;
            consoleBox.ScrollToCaret();
        }

        private void ServerMainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isServerRunning)
            {
                DialogResult result = MessageBox.Show("Server is still running. Do you want to stop it before closing?", "Server Running", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    StopServer();
                    Thread.Sleep(2000);
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }
            continueReading = false;
            if (serverProcess != null && !serverProcess.HasExited)
            {
                try { serverProcess.Kill(); } catch { }
            }
            if (outputReaderThread != null && outputReaderThread.IsAlive)
                outputReaderThread.Join(2000);
            Environment.Exit(0);
        }

        private void LoadPlayers()
        {
            playersListView.Items.Clear();
            string playersDir = Path.Combine(serverPath, "players");
            if (Directory.Exists(playersDir))
            {
                string[] playerFiles = Directory.GetFiles(playersDir, "*.yml");
                foreach (string file in playerFiles)
                {
                    try
                    {
                        PlayerInfo player = new PlayerInfo();
                        bool inPosition = false;
                        string[] lines = File.ReadAllLines(file);
                        foreach (string rawLine in lines)
                        {
                            string line = rawLine.Trim();
                            if (line.StartsWith("---") || line.StartsWith("..."))
                                continue;
                            if (line.StartsWith("position:"))
                            {
                                inPosition = true;
                                continue;
                            }
                            if (line.StartsWith("spawn:"))
                            {
                                inPosition = false;
                                continue;
                            }
                            if (inPosition)
                            {
                                int sep = line.IndexOf(':');
                                if (sep > 0)
                                {
                                    string key = line.Substring(0, sep).Trim().Replace("\"", "");
                                    string value = line.Substring(sep + 1).Trim();
                                    if (key == "level" && string.IsNullOrEmpty(player.PositionLevel))
                                        player.PositionLevel = value;
                                    else if (key == "x" && player.X == 0)
                                    {
                                        double.TryParse(value, out double x);
                                        player.X = x;
                                    }
                                    else if ((key == "y" || key == "'y'" || key == "\"y\"") && player.Y == 0)
                                    {
                                        double.TryParse(value, out double y);
                                        player.Y = y;
                                    }
                                    else if (key == "z" && player.Z == 0)
                                    {
                                        double.TryParse(value, out double z);
                                        player.Z = z;
                                    }
                                }
                            }
                            else
                            {
                                int sep = line.IndexOf(':');
                                if (sep > 0)
                                {
                                    string key = line.Substring(0, sep).Trim();
                                    string value = line.Substring(sep + 1).Trim();
                                    if (key == "caseusername")
                                        player.Username = value;
                                    else if (key == "gamemode")
                                        player.Gamemode = value;
                                    else if (key == "health")
                                        player.Health = value;
                                    else if (key == "lastIP")
                                        player.LastIP = value;
                                    else if (key == "lastID")
                                        player.LastID = value;
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(player.Username))
                        {
                            ListViewItem item = new ListViewItem(player.Username);
                            item.Tag = player;
                            playersListView.Items.Add(item);
                        }
                    }
                    catch { }
                }
            }
        }

        private void PlayersListView_DoubleClick(object sender, EventArgs e)
        {
            if (playersListView.SelectedItems.Count > 0)
            {
                ListViewItem item = playersListView.SelectedItems[0];
                PlayerInfo player = item.Tag as PlayerInfo;
                string info = "Username: " + player.Username + "\r\n" +
                              "Position: Level=" + player.PositionLevel + " X=" + player.X + " Y=" + player.Y + " Z=" + player.Z + "\r\n" +
                              "Gamemode: " + player.Gamemode + "\r\n" +
                              "Health: " + player.Health + "\r\n" +
                              "LastIP: " + player.LastIP + "\r\n" +
                              "LastID: " + player.LastID;
                MessageBox.Show(info, "Player Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void PlayersListView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (playersListView.FocusedItem != null && playersListView.FocusedItem.Bounds.Contains(e.Location))
                    playersContextMenu.Show(Cursor.Position);
            }
        }

        private void BanMenu_Click(object sender, EventArgs e)
        {
            if (playersListView.SelectedItems.Count > 0)
            {
                ListViewItem item = playersListView.SelectedItems[0];
                PlayerInfo player = item.Tag as PlayerInfo;
                if (isServerRunning)
                    ExecuteServerCommand("ban add " + player.Username);
                else
                {
                    string bannedFile = Path.Combine(serverPath, "banned.txt");
                    List<string> banned = new List<string>();
                    if (File.Exists(bannedFile))
                        banned.AddRange(File.ReadAllLines(bannedFile));
                    if (!banned.Contains(player.Username))
                    {
                        banned.Add(player.Username);
                        File.WriteAllLines(bannedFile, banned);
                    }
                }
            }
        }

        private void UnbanMenu_Click(object sender, EventArgs e)
        {
            if (playersListView.SelectedItems.Count > 0)
            {
                ListViewItem item = playersListView.SelectedItems[0];
                PlayerInfo player = item.Tag as PlayerInfo;
                if (isServerRunning)
                    ExecuteServerCommand("ban remove " + player.Username);
                else
                {
                    string bannedFile = Path.Combine(serverPath, "banned.txt");
                    if (File.Exists(bannedFile))
                    {
                        List<string> banned = new List<string>(File.ReadAllLines(bannedFile));
                        if (banned.Contains(player.Username))
                        {
                            banned.Remove(player.Username);
                            File.WriteAllLines(bannedFile, banned);
                        }
                    }
                }
            }
        }

        private void KickMenu_Click(object sender, EventArgs e)
        {
            if (playersListView.SelectedItems.Count > 0)
            {
                ListViewItem item = playersListView.SelectedItems[0];
                PlayerInfo player = item.Tag as PlayerInfo;
                if (isServerRunning)
                    ExecuteServerCommand("kick " + player.Username);
                else
                    MessageBox.Show("Kick command is not available when server is offline.", "Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpMenu_Click(object sender, EventArgs e)
        {
            if (playersListView.SelectedItems.Count > 0)
            {
                ListViewItem item = playersListView.SelectedItems[0];
                PlayerInfo player = item.Tag as PlayerInfo;
                if (isServerRunning)
                    ExecuteServerCommand("op " + player.Username);
                else
                {
                    string opsFile = Path.Combine(serverPath, "ops.txt");
                    List<string> ops = new List<string>();
                    if (File.Exists(opsFile))
                        ops.AddRange(File.ReadAllLines(opsFile));
                    if (!ops.Contains(player.Username))
                    {
                        ops.Add(player.Username);
                        File.WriteAllLines(opsFile, ops);
                    }
                }
            }
        }

        private void DeopMenu_Click(object sender, EventArgs e)
        {
            if (playersListView.SelectedItems.Count > 0)
            {
                ListViewItem item = playersListView.SelectedItems[0];
                PlayerInfo player = item.Tag as PlayerInfo;
                if (isServerRunning)
                    ExecuteServerCommand("deop " + player.Username);
                else
                {
                    string opsFile = Path.Combine(serverPath, "ops.txt");
                    if (File.Exists(opsFile))
                    {
                        List<string> ops = new List<string>(File.ReadAllLines(opsFile));
                        if (ops.Contains(player.Username))
                        {
                            ops.Remove(player.Username);
                            File.WriteAllLines(opsFile, ops);
                        }
                    }
                }
            }
        }

        private void WhitelistAddMenu_Click(object sender, EventArgs e)
        {
            if (playersListView.SelectedItems.Count > 0)
            {
                ListViewItem item = playersListView.SelectedItems[0];
                PlayerInfo player = item.Tag as PlayerInfo;
                if (isServerRunning)
                    ExecuteServerCommand("whitelist add " + player.Username);
                else
                {
                    string whitelistFile = Path.Combine(serverPath, "white-list.txt");
                    List<string> whitelist = new List<string>();
                    if (File.Exists(whitelistFile))
                        whitelist.AddRange(File.ReadAllLines(whitelistFile));
                    if (!whitelist.Contains(player.Username))
                    {
                        whitelist.Add(player.Username);
                        File.WriteAllLines(whitelistFile, whitelist);
                    }
                }
            }
        }

        private void WhitelistRemoveMenu_Click(object sender, EventArgs e)
        {
            if (playersListView.SelectedItems.Count > 0)
            {
                ListViewItem item = playersListView.SelectedItems[0];
                PlayerInfo player = item.Tag as PlayerInfo;
                if (isServerRunning)
                    ExecuteServerCommand("whitelist remove " + player.Username);
                else
                {
                    string whitelistFile = Path.Combine(serverPath, "white-list.txt");
                    if (File.Exists(whitelistFile))
                    {
                        List<string> whitelist = new List<string>(File.ReadAllLines(whitelistFile));
                        if (whitelist.Contains(player.Username))
                        {
                            whitelist.Remove(player.Username);
                            File.WriteAllLines(whitelistFile, whitelist);
                        }
                    }
                }
            }
        }

        private void BanIPMenu_Click(object sender, EventArgs e)
        {
            if (playersListView.SelectedItems.Count > 0)
            {
                ListViewItem item = playersListView.SelectedItems[0];
                PlayerInfo player = item.Tag as PlayerInfo;
                if (isServerRunning)
                    ExecuteServerCommand("banip add " + player.Username);
                else
                {
                    string bannedIpsFile = Path.Combine(serverPath, "banned-ips.txt");
                    List<string> bannedIps = new List<string>();
                    if (File.Exists(bannedIpsFile))
                        bannedIps.AddRange(File.ReadAllLines(bannedIpsFile));
                    if (!bannedIps.Contains(player.LastIP))
                    {
                        bannedIps.Add(player.LastIP);
                        File.WriteAllLines(bannedIpsFile, bannedIps);
                    }
                }
            }
        }

        private void UnbanIPMenu_Click(object sender, EventArgs e)
        {
            if (playersListView.SelectedItems.Count > 0)
            {
                ListViewItem item = playersListView.SelectedItems[0];
                PlayerInfo player = item.Tag as PlayerInfo;
                if (isServerRunning)
                    ExecuteServerCommand("banip remove " + player.Username);
                else
                {
                    string bannedIpsFile = Path.Combine(serverPath, "banned-ips.txt");
                    if (File.Exists(bannedIpsFile))
                    {
                        List<string> bannedIps = new List<string>(File.ReadAllLines(bannedIpsFile));
                        if (bannedIps.Contains(player.LastIP))
                        {
                            bannedIps.Remove(player.LastIP);
                            File.WriteAllLines(bannedIpsFile, bannedIps);
                        }
                    }
                }
            }
        }

        private void ExecuteServerCommand(string command)
        {
            try
            {
                if (serverProcess != null && !serverProcess.HasExited)
                {
                    serverProcess.StandardInput.WriteLine(command);
                    serverProcess.StandardInput.Flush();
                    AppendToConsole("> " + command + "\r\n", Color.Cyan);
                }
            }
            catch (Exception ex)
            {
                AppendToConsole("Error executing command: " + ex.Message + "\r\n", Color.Red);
            }
        }
    }
}