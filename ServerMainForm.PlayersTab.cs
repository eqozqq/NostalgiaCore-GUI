using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace NostalgiaCoreGUI
{
    public partial class ServerMainForm
    {
        internal void InitializePlayersTab()
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

        internal void LoadPlayers()
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
    }

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
}