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
        internal TabPage mainTabPage;
        internal TabPage configTabPage;
        internal TabPage playersTabPage;
        internal Dictionary<string, TextBox> serverPropertiesControls = new Dictionary<string, TextBox>();
        internal Dictionary<string, TextBox> extraPropertiesControls = new Dictionary<string, TextBox>();
        internal Dictionary<string, ComboBox> serverPropertiesComboBoxes = new Dictionary<string, ComboBox>();
        internal Dictionary<string, ComboBox> extraPropertiesComboBoxes = new Dictionary<string, ComboBox>();
        internal Dictionary<string, string> serverPropertiesValues = new Dictionary<string, string>();
        internal Dictionary<string, string> extraPropertiesValues = new Dictionary<string, string>();
        private bool isInitializing = true;
        internal ListView playersListView;
        internal ContextMenuStrip playersContextMenu;
        
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
            InitializeFilesTab();
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
        
        internal void LoadServerProperties()
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
        
        internal void LoadExtraProperties()
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
        
        internal void ServerPropertyTextBox_TextChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;
            TextBox textBox = (TextBox)sender;
            string propertyName = (string)textBox.Tag;
            string propertyValue = textBox.Text;
            serverPropertiesValues[propertyName] = propertyValue;
            SaveServerProperties();
        }
        
        internal void ServerPropertyComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;
            ComboBox comboBox = (ComboBox)sender;
            string propertyName = (string)comboBox.Tag;
            string propertyValue = comboBox.SelectedItem.ToString();
            serverPropertiesValues[propertyName] = propertyValue;
            SaveServerProperties();
        }
        
        internal void ExtraPropertyTextBox_TextChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;
            TextBox textBox = (TextBox)sender;
            string propertyName = (string)textBox.Tag;
            string propertyValue = textBox.Text;
            extraPropertiesValues[propertyName] = propertyValue;
            SaveExtraProperties();
        }
        
        internal void ExtraPropertyComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isInitializing) return;
            ComboBox comboBox = (ComboBox)sender;
            string propertyName = (string)comboBox.Tag;
            string propertyValue = comboBox.SelectedItem.ToString();
            extraPropertiesValues[propertyName] = propertyValue;
            SaveExtraProperties();
        }
        
        internal void SaveServerProperties()
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
        
        internal void SaveExtraProperties()
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
        
        internal void ExecuteServerCommand(string command)
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