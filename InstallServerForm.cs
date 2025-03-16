using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows.Forms;

namespace NostalgiaCoreGUI
{
    public partial class InstallServerForm : Form
    {
        private Form parentForm;
        private string selectedPath;
        private string selectedVersion;
        private readonly string[] availableVersions = new string[]
        {
            "NostalgiaCore_1.1.0_01",
            "NostalgiaCore_1.1.0",
            "NostalgiaCore_1.1.0beta4",
            "NostalgiaCore_1.1.0beta3",
            "NostalgiaCore_1.1.0beta2",
            "NostalgiaCore_1.1.0beta1",
            "NostalgiaCore-Backport",
            "NostalgiaCore_0.9-0.10"
        };

        private readonly string[] downloadUrls = new string[]
        {
            "https://github.com/kotyaralih/NostalgiaCore/archive/refs/tags/NostalgiaCore_1.1.0_01.zip",
            "https://github.com/kotyaralih/NostalgiaCore/archive/refs/tags/NostalgiaCore_1.1.0.zip",
            "https://github.com/kotyaralih/NostalgiaCore/archive/refs/tags/NostalgiaCore_1.1.0beta4.zip",
            "https://github.com/kotyaralih/NostalgiaCore/archive/refs/tags/NostalgiaCore_1.1.0beta3.zip",
            "https://github.com/kotyaralih/NostalgiaCore/archive/refs/tags/NostalgiaCore_1.1.0beta2.zip",
            "https://github.com/kotyaralih/NostalgiaCore/archive/refs/tags/NostalgiaCore_1.1.0beta1.zip",
            "https://github.com/oldminecraftcommunity/NostalgiaCore-Backport/archive/refs/heads/main.zip",
            "https://github.com/oldminecraftcommunity/NostalgiaCore-0.9-0.10/archive/refs/heads/master.zip"
        };

        private const string phpDownloadUrl = "https://github.com/kotyaralih/NostalgiaCore/releases/download/NostalgiaCore_1.1.0_01/PHP_Windows_x64.zip";

        private Label statusLabel;
        private ProgressBar progressBar;
        private BackgroundWorker worker;
        private Button installButton;

        public InstallServerForm(Form parent)
        {
            parentForm = parent;
            InitializeComponent();
            this.Icon = new Icon(new MemoryStream(Properties.Resources.icon));
        }

        private void InitializeComponent()
        {
            this.Text = "Install NostalgiaCore Server";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.FormClosed += InstallServerForm_FormClosed;

            TableLayoutPanel mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 1
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

            Label titleLabel = new Label
            {
                Text = "Install NostalgiaCore Server",
                Font = new Font("Arial", 18, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            Panel locationPanel = new Panel { Dock = DockStyle.Fill };
            Label locationLabel = new Label
            {
                Text = "Installation Directory:",
                AutoSize = true,
                Location = new Point(10, 15)
            };
            TextBox locationTextBox = new TextBox
            {
                ReadOnly = true,
                Location = new Point(150, 12),
                Width = 300
            };
            Button browseButton = new Button
            {
                Text = "Browse...",
                Location = new Point(460, 10),
                Width = 80
            };
            browseButton.Click += (s, e) =>
            {
                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Select installation directory for NostalgiaCore server";
                    folderDialog.ShowNewFolderButton = true;

                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        selectedPath = folderDialog.SelectedPath;
                        locationTextBox.Text = selectedPath;
                    }
                }
            };
            locationPanel.Controls.Add(locationLabel);
            locationPanel.Controls.Add(locationTextBox);
            locationPanel.Controls.Add(browseButton);

            Panel versionPanel = new Panel { Dock = DockStyle.Fill };
            Label versionLabel = new Label
            {
                Text = "Server Version:",
                AutoSize = true,
                Location = new Point(10, 15)
            };
            ComboBox versionComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(150, 12),
                Width = 300
            };
            versionComboBox.Items.AddRange(availableVersions);
            versionComboBox.SelectedIndex = 0;
            versionComboBox.SelectedIndexChanged += (s, e) =>
            {
                selectedVersion = availableVersions[versionComboBox.SelectedIndex];
            };
            selectedVersion = availableVersions[0];
            versionPanel.Controls.Add(versionLabel);
            versionPanel.Controls.Add(versionComboBox);

            statusLabel = new Label
            {
                Text = "Ready to install",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            progressBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(50, 10, 50, 10),
                Style = ProgressBarStyle.Continuous,
                Value = 0
            };

            installButton = new Button
            {
                Text = "Install Server",
                Dock = DockStyle.Fill,
                Margin = new Padding(200, 10, 200, 10)
            };
            installButton.Click += InstallButton_Click;

            mainPanel.Controls.Add(titleLabel, 0, 0);
            mainPanel.Controls.Add(locationPanel, 0, 1);
            mainPanel.Controls.Add(versionPanel, 0, 2);
            mainPanel.Controls.Add(statusLabel, 0, 3);
            mainPanel.Controls.Add(progressBar, 0, 3);
            mainPanel.Controls.Add(installButton, 0, 4);

            progressBar.Visible = false;

            this.Controls.Add(mainPanel);

            worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        }

        private void InstallButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedPath))
            {
                MessageBox.Show("Please select an installation directory.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (Directory.Exists(selectedPath) && Directory.GetFiles(selectedPath).Length > 0)
            {
                DialogResult result = MessageBox.Show("The selected directory is not empty. Continue anyway?",
                    "Directory Not Empty", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.No)
                {
                    return;
                }
            }

            installButton.Visible = false;
            statusLabel.Visible = false;
            progressBar.Visible = true;
            progressBar.Value = 0;

            int versionIndex = Array.IndexOf(availableVersions, selectedVersion);
            string downloadUrl = downloadUrls[versionIndex];

            worker.RunWorkerAsync(new InstallationInfo
            {
                ServerDownloadUrl = downloadUrl,
                PhpDownloadUrl = phpDownloadUrl,
                InstallPath = selectedPath,
                VersionName = selectedVersion
            });
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            InstallationInfo info = (InstallationInfo)e.Argument;
            try
            {
                Directory.CreateDirectory(info.InstallPath);

                worker.ReportProgress(10, "Downloading NostalgiaCore...");
                string serverZipPath = Path.Combine(Path.GetTempPath(), "nostalgiacore_server.zip");
                DownloadFile(info.ServerDownloadUrl, serverZipPath);

                worker.ReportProgress(40, "Extracting NostalgiaCore...");
                ExtractZipFile(serverZipPath, info.InstallPath);
                File.Delete(serverZipPath);

                worker.ReportProgress(60, "Downloading PHP binaries...");
                string phpZipPath = Path.Combine(Path.GetTempPath(), "php_binaries.zip");
                DownloadFile(info.PhpDownloadUrl, phpZipPath);

                worker.ReportProgress(80, "Extracting PHP binaries...");
                string binPath = Path.Combine(info.InstallPath, "bin");
                Directory.CreateDirectory(binPath);
                ExtractZipFile(phpZipPath, binPath);
                File.Delete(phpZipPath);

                worker.ReportProgress(100, "Installation completed successfully!");
                e.Result = info.InstallPath;
            }
            catch (Exception ex)
            {
                worker.ReportProgress(0, "Error: " + ex.Message);
                e.Result = null;
            }
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            statusLabel.Text = e.UserState.ToString();
            statusLabel.Visible = true;
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result != null)
            {
                string serverPath = (string)e.Result;
                MessageBox.Show("NostalgiaCore server has been successfully installed!", "Installation Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ServerMainForm serverForm = new ServerMainForm(serverPath);
                serverForm.Show();
                this.Close();
            }
            else
            {
                progressBar.Visible = false;
                installButton.Visible = true;
                MessageBox.Show("Installation failed. Please try again.", "Installation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DownloadFile(string url, string destinationPath)
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(url, destinationPath);
            }
        }

        private void ExtractZipFile(string zipPath, string extractPath)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                string rootFolderName = null;
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (rootFolderName == null && entry.FullName.Contains('/'))
                    {
                        rootFolderName = entry.FullName.Substring(0, entry.FullName.IndexOf('/'));
                    }
                    if (rootFolderName != null && entry.FullName.StartsWith(rootFolderName + "/"))
                    {
                        string relativePath = entry.FullName.Substring(rootFolderName.Length + 1);
                        if (string.IsNullOrEmpty(relativePath))
                            continue;
                        string destPath = Path.Combine(extractPath, relativePath);
                        string destDir = Path.GetDirectoryName(destPath);
                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                            Directory.CreateDirectory(destDir);
                        if (!string.IsNullOrEmpty(Path.GetFileName(destPath)))
                            entry.ExtractToFile(destPath, true);
                    }
                    else if (rootFolderName == null)
                    {
                        string destPath = Path.Combine(extractPath, entry.FullName);
                        string destDir = Path.GetDirectoryName(destPath);
                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                            Directory.CreateDirectory(destDir);
                        if (!string.IsNullOrEmpty(Path.GetFileName(destPath)))
                            entry.ExtractToFile(destPath, true);
                    }
                }
            }
        }

        private void InstallServerForm_FormClosed(object sender, FormClosedEventArgs e) {}

        private class InstallationInfo
        {
            public string ServerDownloadUrl { get; set; }
            public string PhpDownloadUrl { get; set; }
            public string InstallPath { get; set; }
            public string VersionName { get; set; }
        }
    }
}