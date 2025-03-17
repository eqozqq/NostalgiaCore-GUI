using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace NostalgiaCoreGUI
{
    public partial class ServerMainForm
    {
        private TabPage filesTabPage;
        private Panel topPanel;
        private Button btnUploadFiles;
        private Button btnUploadFolder;
        private Button btnDelete;
        private Button btnCopy;
        private Button btnCompress;
        private Button btnBack;
        private Button btnForward;
        private Button btnReload;
        private CheckBox chkSelectAll;
        private ListView fileListView;
        private ContextMenuStrip fileContextMenu;
        private string currentPath;
        private Stack<string> backStack = new Stack<string>();
        private Stack<string> forwardStack = new Stack<string>();

        internal void InitializeFilesTab()
        {
            filesTabPage = new TabPage("Files");
            currentPath = serverPath;
            topPanel = new Panel { Dock = DockStyle.Top, Height = 40 };
            btnBack = new Button { Text = "←", Left = 5, Top = 5, Width = 40 };
            btnForward = new Button { Text = "→", Left = 50, Top = 5, Width = 40 };
            btnReload = new Button { Text = "⟳", Left = 95, Top = 5, Width = 40 };
            btnUploadFiles = new Button { Text = "Upload Files", Left = 140, Top = 5, Width = 100 };
            btnUploadFolder = new Button { Text = "Upload Folder", Left = 245, Top = 5, Width = 100 };
            btnDelete = new Button { Text = "Delete", Left = 350, Top = 5, Width = 110, Visible = false };
            btnCopy = new Button { Text = "Copy", Left = 465, Top = 5, Width = 80, Visible = false };
            btnCompress = new Button { Text = "Compress", Left = 550, Top = 5, Width = 120, Visible = false };
            chkSelectAll = new CheckBox { Text = "Select All", Left = 675, Top = 10, Width = 80 };
            btnBack.Click += BtnBack_Click;
            btnForward.Click += BtnForward_Click;
            btnReload.Click += BtnReload_Click;
            btnUploadFiles.Click += BtnUploadFiles_Click;
            btnUploadFolder.Click += BtnUploadFolder_Click;
            btnDelete.Click += BtnDeleteSelected_Click;
            btnCopy.Click += BtnCopy_Click;
            btnCompress.Click += BtnCreateArchive_Click;
            chkSelectAll.CheckedChanged += ChkSelectAll_CheckedChanged;
            topPanel.Controls.Add(btnBack);
            topPanel.Controls.Add(btnForward);
            topPanel.Controls.Add(btnReload);
            topPanel.Controls.Add(btnUploadFiles);
            topPanel.Controls.Add(btnUploadFolder);
            topPanel.Controls.Add(btnDelete);
            topPanel.Controls.Add(btnCopy);
            topPanel.Controls.Add(btnCompress);
            topPanel.Controls.Add(chkSelectAll);
            fileListView = new ListView { Dock = DockStyle.Fill, View = View.Details, CheckBoxes = true };
            fileListView.Columns.Add("Name", 250);
            fileListView.Columns.Add("Date Modified", 150);
            fileListView.Columns.Add("Size", 100);
            fileListView.FullRowSelect = true;
            fileListView.MultiSelect = true;
            fileListView.HideSelection = false;
            fileListView.DoubleClick += FileListView_DoubleClick;
            fileListView.MouseClick += FileListView_MouseClick;
            fileListView.ItemChecked += FileListView_ItemChecked;
            fileContextMenu = new ContextMenuStrip();
            fileContextMenu.Items.Add("Open").Click += FileOpen_Click;
            fileContextMenu.Items.Add("Rename").Click += FileRename_Click;
            fileContextMenu.Items.Add("Delete").Click += FileDelete_Click;
            fileContextMenu.Items.Add("Compress").Click += FileCompress_Click;
            fileContextMenu.Items.Add("Extract").Click += FileExtract_Click;
            fileListView.ContextMenuStrip = fileContextMenu;
            Panel mainPanel = new Panel { Dock = DockStyle.Fill };
            mainPanel.Controls.Add(fileListView);
            mainPanel.Controls.Add(topPanel);
            filesTabPage.Controls.Add(mainPanel);
            tabControl.TabPages.Add(filesTabPage);
            LoadFileList();
        }

        private void LoadFileList()
        {
            fileListView.Items.Clear();
            DirectoryInfo di = new DirectoryInfo(currentPath);
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                ListViewItem lvi = new ListViewItem(dir.Name);
                lvi.SubItems.Add(dir.LastWriteTime.ToString("G"));
                lvi.SubItems.Add("Folder");
                lvi.Tag = dir.FullName;
                fileListView.Items.Add(lvi);
            }
            foreach (FileInfo file in di.GetFiles())
            {
                ListViewItem lvi = new ListViewItem(file.Name);
                lvi.SubItems.Add(file.LastWriteTime.ToString("G"));
                lvi.SubItems.Add(FormatSize(file.Length));
                lvi.Tag = file.FullName;
                fileListView.Items.Add(lvi);
            }
            UpdateActionButtons();
        }

        private string FormatSize(long bytes)
        {
            if (bytes < 1024)
                return bytes + " B";
            double kb = bytes / 1024.0;
            if (kb < 1024)
                return kb.ToString("F2") + " KB";
            double mb = kb / 1024.0;
            if (mb < 1024)
                return mb.ToString("F2") + " MB";
            double gb = mb / 1024.0;
            return gb.ToString("F2") + " GB";
        }

        private void UpdateActionButtons()
        {
            bool hasSelection = fileListView.CheckedItems.Count > 0;
            btnDelete.Visible = hasSelection;
            btnCopy.Visible = hasSelection;
            if (hasSelection)
            {
                bool allArchives = true;
                foreach (ListViewItem item in fileListView.CheckedItems)
                {
                    string path = item.Tag.ToString();
                    if (!File.Exists(path) || Path.GetExtension(path).ToLower() != ".zip")
                    {
                        allArchives = false;
                        break;
                    }
                }
                btnCompress.Text = allArchives ? "Extract" : "Compress";
                btnCompress.Visible = true;
            }
            else
            {
                btnCompress.Visible = false;
            }
        }

        private void FileListView_DoubleClick(object sender, EventArgs e)
        {
            if (fileListView.SelectedItems.Count == 0)
                return;
            ListViewItem item = fileListView.SelectedItems[0];
            string path = item.Tag.ToString();
            if (Directory.Exists(path))
            {
                backStack.Push(currentPath);
                forwardStack.Clear();
                currentPath = path;
                LoadFileList();
            }
            else if (File.Exists(path))
            {
                OpenFile(path);
            }
        }

        private void FileListView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (fileListView.FocusedItem != null && fileListView.FocusedItem.Bounds.Contains(e.Location))
                {
                    string path = fileListView.FocusedItem.Tag.ToString();
                    if (File.Exists(path) && Path.GetExtension(path).ToLower() == ".zip")
                        fileContextMenu.Items[4].Enabled = true;
                    else
                        fileContextMenu.Items[4].Enabled = false;
                    fileContextMenu.Show(Cursor.Position);
                }
            }
        }

        private void FileOpen_Click(object sender, EventArgs e)
        {
            if (fileListView.SelectedItems.Count == 0)
                return;
            ListViewItem item = fileListView.SelectedItems[0];
            string path = item.Tag.ToString();
            if (Directory.Exists(path))
            {
                backStack.Push(currentPath);
                forwardStack.Clear();
                currentPath = path;
                LoadFileList();
            }
            else if (File.Exists(path))
            {
                OpenFile(path);
            }
        }

        private void OpenFile(string path)
        {
            string ext = Path.GetExtension(path);
            if (string.IsNullOrEmpty(ext))
                System.Diagnostics.Process.Start("notepad.exe", path);
            else
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true });
                }
                catch
                {
                    System.Diagnostics.Process.Start("notepad.exe", path);
                }
            }
        }

        private void FileRename_Click(object sender, EventArgs e)
        {
            if (fileListView.SelectedItems.Count == 0)
                return;
            ListViewItem item = fileListView.SelectedItems[0];
            string path = item.Tag.ToString();
            string input = Interaction.InputBox("New name:", "Rename", Path.GetFileName(path));
            if (!string.IsNullOrWhiteSpace(input))
            {
                string newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), input);
                if (File.Exists(path))
                    File.Move(path, newPath);
                else if (Directory.Exists(path))
                    Directory.Move(path, newPath);
                item.Text = input;
                item.Tag = newPath;
                LoadFileList();
            }
        }

        private void FileDelete_Click(object sender, EventArgs e)
        {
            if (fileListView.SelectedItems.Count == 0)
                return;
            ListViewItem item = fileListView.SelectedItems[0];
            string path = item.Tag.ToString();
            if (File.Exists(path))
                File.Delete(path);
            else if (Directory.Exists(path))
                Directory.Delete(path, true);
            LoadFileList();
        }

        private void FileCompress_Click(object sender, EventArgs e)
        {
            if (fileListView.SelectedItems.Count == 0)
                return;
            ListViewItem item = fileListView.SelectedItems[0];
            string path = item.Tag.ToString();
            string zipPath = path + ".zip";
            if (File.Exists(path))
            {
                using (FileStream zipToOpen = new FileStream(zipPath, FileMode.Create))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                    {
                        AddFileToZip(archive, path, System.IO.Path.GetFileName(path));
                    }
                }
            }
            else if (Directory.Exists(path))
            {
                ZipFile.CreateFromDirectory(path, zipPath);
            }
            LoadFileList();
        }

        private void FileExtract_Click(object sender, EventArgs e)
        {
            if (fileListView.SelectedItems.Count == 0)
                return;
            foreach (ListViewItem item in fileListView.CheckedItems)
            {
                string path = item.Tag.ToString();
                if (File.Exists(path) && Path.GetExtension(path).ToLower() == ".zip")
                {
                    string extractDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), System.IO.Path.GetFileNameWithoutExtension(path));
                    if (!Directory.Exists(extractDir))
                        Directory.CreateDirectory(extractDir);
                    ZipFile.ExtractToDirectory(path, extractDir);
                }
            }
            LoadFileList();
        }

        private void BtnUploadFiles_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Multiselect = true };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                foreach (string file in ofd.FileNames)
                {
                    string dest = System.IO.Path.Combine(currentPath, System.IO.Path.GetFileName(file));
                    File.Copy(file, dest, true);
                }
                LoadFileList();
            }
        }

        private void BtnUploadFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                string folderName = new DirectoryInfo(fbd.SelectedPath).Name;
                string destDir = System.IO.Path.Combine(currentPath, folderName);
                CopyDirectory(fbd.SelectedPath, destDir);
                LoadFileList();
            }
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            if (backStack.Count > 0)
            {
                forwardStack.Push(currentPath);
                currentPath = backStack.Pop();
                LoadFileList();
            }
        }

        private void BtnForward_Click(object sender, EventArgs e)
        {
            if (forwardStack.Count > 0)
            {
                backStack.Push(currentPath);
                currentPath = forwardStack.Pop();
                LoadFileList();
            }
        }

        private void BtnReload_Click(object sender, EventArgs e)
        {
            LoadFileList();
        }

        private void BtnDeleteSelected_Click(object sender, EventArgs e)
        {
            if (fileListView.CheckedItems.Count == 0)
                return;
            foreach (ListViewItem item in fileListView.CheckedItems)
            {
                string source = item.Tag.ToString();
                if (File.Exists(source))
                    File.Delete(source);
                else if (Directory.Exists(source))
                    Directory.Delete(source, true);
            }
            LoadFileList();
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            if (fileListView.CheckedItems.Count == 0)
                return;
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                foreach (ListViewItem item in fileListView.CheckedItems)
                {
                    string source = item.Tag.ToString();
                    string dest = System.IO.Path.Combine(fbd.SelectedPath, System.IO.Path.GetFileName(source));
                    if (File.Exists(source))
                        File.Copy(source, dest, true);
                    else if (Directory.Exists(source))
                        CopyDirectory(source, dest);
                }
            }
        }

        private void BtnCreateArchive_Click(object sender, EventArgs e)
        {
            if (fileListView.CheckedItems.Count == 0)
                return;
            if (btnCompress.Text == "Extract")
            {
                BtnExtractSelected();
            }
            else
            {
                string defaultName = "Archive.zip";
                if (fileListView.CheckedItems.Count == 1)
                {
                    string source = fileListView.CheckedItems[0].Tag.ToString();
                    defaultName = Path.GetFileNameWithoutExtension(source) + ".zip";
                }
                SaveFileDialog sfd = new SaveFileDialog { Filter = "Zip files|*.zip", FileName = defaultName };
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    using (FileStream zipToCreate = new FileStream(sfd.FileName, FileMode.Create))
                    using (ZipArchive archive = new ZipArchive(zipToCreate, ZipArchiveMode.Create))
                    {
                        foreach (ListViewItem item in fileListView.CheckedItems)
                        {
                            string source = item.Tag.ToString();
                            if (File.Exists(source))
                                AddFileToZip(archive, source, Path.GetFileName(source));
                            else if (Directory.Exists(source))
                                AddDirectoryToZip(archive, source, Path.GetFileName(source));
                        }
                    }
                }
            }
        }

        private void BtnExtractSelected()
        {
            foreach (ListViewItem item in fileListView.CheckedItems)
            {
                string source = item.Tag.ToString();
                if (File.Exists(source) && Path.GetExtension(source).ToLower() == ".zip")
                {
                    string extractDir = Path.Combine(Path.GetDirectoryName(source), Path.GetFileNameWithoutExtension(source));
                    if (!Directory.Exists(extractDir))
                        Directory.CreateDirectory(extractDir);
                    ZipFile.ExtractToDirectory(source, extractDir);
                }
            }
            LoadFileList();
        }

        private void ChkSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            foreach (ListViewItem item in fileListView.Items)
                item.Checked = chkSelectAll.Checked;
            UpdateActionButtons();
        }

        private void FileListView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            UpdateActionButtons();
        }

        private void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            foreach (string directory in Directory.GetDirectories(sourceDir))
            {
                string destDir = Path.Combine(targetDir, Path.GetFileName(directory));
                CopyDirectory(directory, destDir);
            }
        }

        private void AddFileToZip(ZipArchive archive, string source, string entryName)
        {
            using (FileStream fs = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                ZipArchiveEntry entry = archive.CreateEntry(entryName);
                using (var entryStream = entry.Open())
                    fs.CopyTo(entryStream);
            }
        }

        private void AddDirectoryToZip(ZipArchive archive, string sourceDir, string entryName)
        {
            DirectoryInfo di = new DirectoryInfo(sourceDir);
            foreach (FileInfo file in di.GetFiles())
            {
                string relativePath = Path.Combine(entryName, file.Name);
                AddFileToZip(archive, file.FullName, relativePath);
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                string relPath = Path.Combine(entryName, dir.Name);
                AddDirectoryToZip(archive, dir.FullName, relPath);
            }
        }
    }
}
