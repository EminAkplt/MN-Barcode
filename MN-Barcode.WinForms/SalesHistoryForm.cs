using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MN_Barcode.Business;
using MN_Barcode.Entities;

namespace MN_Barcode.WinForms
{
    public class SalesHistoryForm : Form
    {
        private SaleService _saleService;
        private DataGridView _gridSales;
        private DateTimePicker _dtStart;
        private DateTimePicker _dtEnd;
        private Label _lblTotalRevenue;
        private TextBox _txtSearch;

        private static readonly Color[] RowColors = new[]
        {
            Color.FromArgb(240, 248, 255),  // Alice Blue
            Color.FromArgb(240, 255, 240),  // Honeydew
            Color.FromArgb(255, 250, 240)   // Floral White
        };

        public SalesHistoryForm()
        {
            _saleService = new SaleService();
            this.DoubleBuffered = true;
            InitUI();
            this.Shown += (s, e) => LoadData();
        }

        private void InitUI()
        {
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;

            // ====== HEADER: Title + Summary + Filter ======
            Panel header = new Panel { Dock = DockStyle.Top, Height = 140, BackColor = Color.White };
            this.Controls.Add(header);
            header.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(226, 232, 240), 1))
                    e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };

            // Title + Total (top row)
            Label title = new Label { Text = "Satış Geçmişi", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), AutoSize = true };
            title.Location = new Point(24, 12);
            header.Controls.Add(title);

            _lblTotalRevenue = new Label { Text = "Toplam: ₺0.00", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.FromArgb(22, 163, 74), AutoSize = true };
            _lblTotalRevenue.Location = new Point(header.Width - 250, 12);
            header.Controls.Add(_lblTotalRevenue);

            // Filter label
            Label filterLabel = new Label { Text = "Filtreleme:", Font = new Font("Segoe UI", 10, FontStyle.Regular), ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true };
            filterLabel.Location = new Point(24, 50);
            header.Controls.Add(filterLabel);

            // Date pickers + search
            _dtStart = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 120,
                Height = 32,
                Font = new Font("Segoe UI", 10),
                Value = DateTime.Today.AddDays(-7),
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            _dtStart.ValueChanged += (s, e) => LoadData();
            _dtStart.Location = new Point(120, 48);
            header.Controls.Add(_dtStart);

            Label sep = new Label { Text = "–", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true };
            sep.Location = new Point(250, 54);
            header.Controls.Add(sep);

            _dtEnd = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 120,
                Height = 32,
                Font = new Font("Segoe UI", 10),
                Value = DateTime.Today,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            _dtEnd.ValueChanged += (s, e) => LoadData();
            _dtEnd.Location = new Point(270, 48);
            header.Controls.Add(_dtEnd);

            _txtSearch = new TextBox
            {
                Width = 260,
                Height = 32,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            _txtSearch.GotFocus += (s, e) =>
            {
                if (_txtSearch.Text == "Barkod veya ürün adı yazın...")
                {
                    _txtSearch.Text = "";
                    _txtSearch.ForeColor = Color.FromArgb(30, 41, 59);
                }
            };
            _txtSearch.LostFocus += (s, e) =>
            {
                if (_txtSearch.Text == "")
                {
                    _txtSearch.Text = "Barkod veya ürün adı yazın...";
                    _txtSearch.ForeColor = Color.FromArgb(100, 116, 139);
                }
            };
            _txtSearch.Text = "Barkod veya ürün adı yazın...";
            _txtSearch.TextChanged += (s, e) => FilterData();
            _txtSearch.Location = new Point(410, 48);
            header.Controls.Add(_txtSearch);

            // ====== GRID ======
            _gridSales = CreateGrid();
            _gridSales.Columns.Add("Product", "ÜRÜN ADI"); _gridSales.Columns["Product"].FillWeight = 200;
            _gridSales.Columns.Add("Barcode", "BARKOD"); _gridSales.Columns["Barcode"].Width = 110;
            _gridSales.Columns.Add("SaleId", "FİŞ NO"); _gridSales.Columns["SaleId"].Width = 90;
            _gridSales.Columns.Add("Date", "TARİH"); _gridSales.Columns["Date"].Width = 150;
            _gridSales.Columns.Add("Price", "TUTAR"); _gridSales.Columns["Price"].Width = 110; _gridSales.Columns["Price"].DefaultCellStyle.Format = "C2";
            _gridSales.Dock = DockStyle.Fill;
            _gridSales.BackgroundColor = Color.FromArgb(248, 250, 252);
            this.Controls.Add(_gridSales);
        }


        private DataGridView CreateGrid()
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
                ReadOnly = true,
                RowTemplate = { Height = 36 }
            };
            grid.DefaultCellStyle.Font = Theme.Body;
            grid.DefaultCellStyle.ForeColor = Theme.TextDark;
            grid.DefaultCellStyle.BackColor = Theme.Surface;
            grid.ColumnHeadersHeight = 36;
            grid.ColumnHeadersDefaultCellStyle.Font = Theme.Caption;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextMuted;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.Surface;
            grid.EnableHeadersVisualStyles = false;
            return grid;
        }

        private void LoadData()
        {
            DateTime start = _dtStart.Value.Date;
            DateTime end = _dtEnd.Value.Date.AddDays(1).AddSeconds(-1);

            _gridSales.Rows.Clear();
            var details = _saleService.GetSalesHistoryWithDetails(start, end);

            var grouped = details.GroupBy(x => x.Sale.TransactionCode).ToList();
            int colorIndex = 0;

            foreach (var group in grouped)
            {
                Color rowColor = RowColors[colorIndex % RowColors.Length];
                colorIndex++;

                foreach (var detail in group)
                {
                    int rowIdx = _gridSales.Rows.Add(
                        detail.Product?.Name ?? "",
                        detail.Product?.Barcode ?? "",
                        detail.Sale.TransactionCode,
                        detail.Sale.CreatedDate?.ToString("dd.MM.yyyy HH:mm"),
                        detail.TotalPrice
                    );

                    // Satırın arka plan rengini ayarla
                    for (int i = 0; i < _gridSales.Columns.Count; i++)
                    {
                        _gridSales.Rows[rowIdx].Cells[i].Style.BackColor = rowColor;
                    }
                }
            }

            // Toplam Ciro
            decimal totalRev = details.Sum(x => x.TotalPrice);
            _lblTotalRevenue.Text = $"Toplam: {totalRev:C2}";
        }

        private void FilterData()
        {
            string searchText = _txtSearch.Text.Trim().ToLower();
            if (searchText == "barkod veya ürün adı yazın..." || searchText == "")
            {
                foreach (DataGridViewRow row in _gridSales.Rows)
                    row.Visible = true;
                return;
            }
            foreach (DataGridViewRow row in _gridSales.Rows)
            {
                string barcode = row.Cells["Barcode"].Value?.ToString()?.ToLower() ?? "";
                string product = row.Cells["Product"].Value?.ToString()?.ToLower() ?? "";
                row.Visible = barcode.Contains(searchText) || product.Contains(searchText);
            }
        }
    }
}
