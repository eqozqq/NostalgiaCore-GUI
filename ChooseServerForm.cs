using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;

namespace NostalgiaCoreGUI
{
    public class RoundedListBox : ListBox
    {
        
        public int CornerRadius { get; set; } = 10;
        public Padding TextPadding { get; set; } = new Padding(5, 3, 5, 3);
        public RoundedListBox()
        {
            this.BorderStyle = BorderStyle.None;
            this.DrawMode = DrawMode.OwnerDrawFixed;
            this.ItemHeight = 20;
        }
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index >= 0 && e.Index < this.Items.Count)
            {
                string text = this.Items[e.Index].ToString();
                Rectangle paddedBounds = new Rectangle(e.Bounds.X + TextPadding.Left, e.Bounds.Y + TextPadding.Top, e.Bounds.Width - TextPadding.Left - TextPadding.Right, e.Bounds.Height - TextPadding.Top - TextPadding.Bottom);
                using (SolidBrush brush = new SolidBrush(e.ForeColor))
                {
                    e.Graphics.DrawString(text, e.Font, brush, paddedBounds);
                }
            }
            e.DrawFocusRectangle();
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRegion();
        }
        private void UpdateRegion()
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                Rectangle rect = this.ClientRectangle;
                int diameter = CornerRadius * 2;
                path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
                path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
                path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                this.Region = new Region(path);
            }
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateRegion();
        }
    }

    public partial class ChooseServerForm : Form
    {
        private RoundedListBox recentListBox;
        private const string RecentFilePath = "recent.txt";

        public ChooseServerForm()
        {
            InitializeComponent();
            LoadRecentServers();
            this.Icon = new Icon(new MemoryStream(Properties.Resources.icon));
        }

        private void InitializeComponent()
        {
            this.Text = "NostalgiaCore-GUI";
            this.Size = new Size(400, 350);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            TableLayoutPanel mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            Label titleLabel = new Label
            {
                Text = "NostalgiaCore-GUI",
                Font = new Font("Arial", 24, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            Button chooseServerButton = new Button
            {
                Text = "Choose server directory",
                Dock = DockStyle.Fill,
                Margin = new Padding(50, 10, 50, 10)
            };
            chooseServerButton.Click += ChooseServerButton_Click;
            Button installServerButton = new Button
            {
                Text = "Install server into new directory",
                Dock = DockStyle.Fill,
                Margin = new Padding(50, 10, 50, 10)
            };
            installServerButton.Click += InstallServerButton_Click;
            recentListBox = new RoundedListBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(20, 5, 20, 5)
            };
            recentListBox.DoubleClick += RecentListBox_DoubleClick;
            recentListBox.MouseDown += RecentListBox_MouseDown;
            ContextMenuStrip recentContextMenu = new ContextMenuStrip();
            ToolStripMenuItem deleteItem = new ToolStripMenuItem("Delete");
            deleteItem.Click += DeleteRecentItem_Click;
            recentContextMenu.Items.Add(deleteItem);
            recentListBox.ContextMenuStrip = recentContextMenu;
            mainPanel.Controls.Add(titleLabel, 0, 0);
            mainPanel.Controls.Add(chooseServerButton, 0, 1);
            mainPanel.Controls.Add(installServerButton, 0, 2);
            mainPanel.Controls.Add(recentListBox, 0, 3);
            this.Controls.Add(mainPanel);
        }

        private void ChooseServerButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select NostalgiaCore server directory";
                folderDialog.ShowNewFolderButton = true;
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = folderDialog.SelectedPath;
                    if (!Directory.Exists(selectedPath))
                    {
                        MessageBox.Show("Not a directory!", "Invalid selection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    if (!File.Exists(Path.Combine(selectedPath, "start.cmd")))
                    {
                        MessageBox.Show("Could not find a NostalgiaCore installation in the selected directory! Please click the \"Install server into new directory\" button if you wish to install a server there instead.", "Invalid selection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    string phpPath = Path.Combine(selectedPath, "bin", "php", "php.exe");
                    if (!File.Exists(phpPath))
                    {
                        MessageBox.Show("PHP binaries not found in the server directory.", "Missing PHP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    AddRecentServer(selectedPath);
                    ServerMainForm serverForm = new ServerMainForm(selectedPath);
                    serverForm.Show();
                    this.Hide();
                }
            }
        }

        private void InstallServerButton_Click(object sender, EventArgs e)
        {
            InstallServerForm installForm = new InstallServerForm(this);
            installForm.Show();
            this.Hide();
        }

        private void RecentListBox_DoubleClick(object sender, EventArgs e)
        {
            if (recentListBox.SelectedItem != null)
            {
                string selectedPath = recentListBox.SelectedItem.ToString();
                if (!Directory.Exists(selectedPath))
                {
                    MessageBox.Show("Directory not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    RemoveRecentServer(selectedPath);
                    return;
                }
                if (!File.Exists(Path.Combine(selectedPath, "start.cmd")))
                {
                    MessageBox.Show("The selected directory does not contain a valid NostalgiaCore installation.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string phpPath = Path.Combine(selectedPath, "bin", "php", "php.exe");
                if (!File.Exists(phpPath))
                {
                    MessageBox.Show("PHP binaries not found in the server directory.", "Missing PHP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                ServerMainForm serverForm = new ServerMainForm(selectedPath);
                serverForm.Show();
                this.Hide();
            }
        }

        private void RecentListBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int index = recentListBox.IndexFromPoint(e.Location);
                if (index != ListBox.NoMatches)
                {
                    recentListBox.SelectedIndex = index;
                }
            }
        }

        private void DeleteRecentItem_Click(object sender, EventArgs e)
        {
            if (recentListBox.SelectedItem != null)
            {
                string selectedPath = recentListBox.SelectedItem.ToString();
                RemoveRecentServer(selectedPath);
            }
        }

        private void AddRecentServer(string path)
        {
            List<string> recents = new List<string>();
            if (File.Exists(RecentFilePath))
                recents.AddRange(File.ReadAllLines(RecentFilePath));
            if (!recents.Contains(path))
            {
                recents.Insert(0, path);
                SaveRecentServers(recents);
            }
            LoadRecentServers();
        }

        private void RemoveRecentServer(string path)
        {
            List<string> recents = new List<string>();
            if (File.Exists(RecentFilePath))
                recents.AddRange(File.ReadAllLines(RecentFilePath));
            if (recents.Contains(path))
            {
                recents.Remove(path);
                SaveRecentServers(recents);
            }
            LoadRecentServers();
        }

        private void LoadRecentServers()
        {
            recentListBox.Items.Clear();
            if (File.Exists(RecentFilePath))
            {
                string[] lines = File.ReadAllLines(RecentFilePath);
                foreach (string line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        recentListBox.Items.Add(line.Trim());
                }
            }
            recentListBox.Visible = recentListBox.Items.Count > 0;
        }

        private void SaveRecentServers(List<string> servers)
        {
            File.WriteAllLines(RecentFilePath, servers);
        }
    }
}