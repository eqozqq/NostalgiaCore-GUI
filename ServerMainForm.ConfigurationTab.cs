using System;
using System.Windows.Forms;

namespace NostalgiaCoreGUI
{
    public partial class ServerMainForm
    {
        internal void InitializeConfigTab()
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
            Label propertyLabel = new Label { Text = label, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft, Padding = new Padding(5) };
            TextBox propertyTextBox = new TextBox { Text = defaultValue, Dock = DockStyle.Fill, Margin = new Padding(5), Tag = propertyName };
            propertyTextBox.TextChanged += ServerPropertyTextBox_TextChanged;
            serverPropertiesControls[propertyName] = propertyTextBox;
            table.Controls.Add(propertyLabel, 0, rowIndex);
            table.Controls.Add(propertyTextBox, 1, rowIndex);
        }

        private void AddServerPropertyComboBox(TableLayoutPanel table, int rowIndex, string propertyName, string label, string[] options, string defaultValue)
        {
            Label propertyLabel = new Label { Text = label, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft, Padding = new Padding(5) };
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
            Label propertyLabel = new Label { Text = label, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft, Padding = new Padding(5) };
            TextBox propertyTextBox = new TextBox { Text = defaultValue, Dock = DockStyle.Fill, Margin = new Padding(5), Tag = propertyName };
            propertyTextBox.TextChanged += ExtraPropertyTextBox_TextChanged;
            extraPropertiesControls[propertyName] = propertyTextBox;
            table.Controls.Add(propertyLabel, 0, rowIndex);
            table.Controls.Add(propertyTextBox, 1, rowIndex);
        }

        private void AddExtraPropertyComboBox(TableLayoutPanel table, int rowIndex, string propertyName, string label, string[] options, string defaultValue)
        {
            Label propertyLabel = new Label { Text = label, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft, Padding = new Padding(5) };
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
    }
}