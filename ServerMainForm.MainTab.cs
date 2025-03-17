using System.Drawing;
using System.Windows.Forms;

namespace NostalgiaCoreGUI
{
    public partial class ServerMainForm
    {
        internal void InitializeMainTab()
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
                Location = new System.Drawing.Point(10, 10),
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
    }
}