using System;
using System.Drawing;
using System.Windows.Forms;

namespace MN_Barcode.WinForms
{
    public static class UiHelpers
    {
        public static Panel CreateCardPanel(string title, Color? accentColor = null)
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Margin = new Padding(10)
            };

            Label lbl = new Label
            {
                Text = title,
                Font = Theme.H2,
                ForeColor = Theme.TextStrong,
                Dock = DockStyle.Top,
                Height = 50,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 10, 0, 0)
            };
            panel.Controls.Add(lbl);

            if (accentColor.HasValue)
            {
                panel.Paint += (s, e) => Theme.PaintCard(panel, e, accentColor);
            }

            return panel;
        }

        public static DataGridView CreateGrid()
        {
            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Theme.Surface,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true
            };

            grid.RowTemplate.Height = 45;
            grid.DefaultCellStyle.Font = Theme.Body;
            grid.DefaultCellStyle.ForeColor = Theme.TextDark;
            grid.DefaultCellStyle.SelectionBackColor = Theme.Border;
            grid.DefaultCellStyle.SelectionForeColor = Theme.TextDark;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Theme.Background;

            grid.ColumnHeadersHeight = 45;
            grid.ColumnHeadersDefaultCellStyle.Font = Theme.H2;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextMuted;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.Border;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.GridColor = Theme.Border;

            return grid;
        }

        public static Button CreateActionButton(string text, Color backColor, EventHandler onClick = null)
        {
            Button btn = new Button
            {
                Text = text,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(Theme.FontName, 11, FontStyle.Bold),
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;

            if (onClick != null)
                btn.Click += onClick;

            return btn;
        }

        public static Panel StyleHeaderPanel(Panel header, string title)
        {
            header.BackColor = Theme.Sidebar;
            header.Dock = DockStyle.Top;
            header.Height = 70;

            Label lblTitle = new Label
            {
                Text = title,
                Font = Theme.H1,
                ForeColor = Color.White,
                Location = new Point(30, 20),
                AutoSize = true
            };
            header.Controls.Add(lblTitle);

            return header;
        }
    }
}
