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

            // ====== TOP SECTION: Title + Summary ======
            Panel topSection = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.White, Padding = new Padding(24, 16, 24, 16) };
            this.Controls.Add(topSection);
            topSection.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(226, 232, 240), 1))
                    e.Graphics.DrawLine(pen, 0, topSection.Height - 1, topSection.Width, topSection.Height - 1);
            };

            Label title = new Label { Text = "Satış Geçmişi", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), AutoSize = true, Location = new Point(0, 0) };
            topSection.Controls.Add(title);

            _lblTotalRevenue = new Label { Text = "Toplam: ₺0.00", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.FromArgb(22, 163, 74), AutoSize = true, Dock = DockStyle.Right };
            topSection.Controls.Add(_lblTotalRevenue);

            // ====== FILTER SECTION ======
            Panel filterSection = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.White, Padding = new Padding(24, 16, 24, 16) };
            this.Controls.Add(filterSection);

            Label filterLabel = new Label { Text = "Filtreleme", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59), AutoSize = true, Location = new Point(0, 0) };
            filterSection.Controls.Add(filterLabel);

            FlowLayoutPanel filterControls = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 0)
            };
            filterSection.Controls.Add(filterControls);

            filterControls.Controls.Add(new Label { Text = "Tarih:", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Margin = new Padding(0, 8, 10, 0) });

            _dtStart = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 130,
                Height = 36,
                Font = new Font("Segoe UI", 10),
                Value = DateTime.Today.AddDays(-7),
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            _dtStart.ValueChanged += (s, e) => LoadData();
            filterControls.Controls.Add(_dtStart);

            filterControls.Controls.Add(new Label { Text = "–", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Margin = new Padding(10, 8, 10, 0) });

            _dtEnd = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 130,
                Height = 36,
                Font = new Font("Segoe UI", 10),
                Value = DateTime.Today,
                BackColor = Color.FromArgb(248, 250, 252),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            _dtEnd.ValueChanged += (s, e) => LoadData();
            filterControls.Controls.Add(_dtEnd);

            // Search kutusu
            _txtSearch = new TextBox
            {
                Width = 250,
                Height = 36,
                Font = new Font("Segoe UI", 10),
                Margin = new Padding(20, 0, 0, 0),
                ForeColor = Color.FromArgb(100, 116, 139)
            };
            _txtSearch.GotFocus += (s, e) => { if (_txtSearch.Text == "Barkod veya ürün adı yazın...") { _txtSearch.Text = ""; _txtSearch.ForeColor = Color.FromArgb(30, 41, 59); } };
            _txtSearch.LostFocus += (s, e) => { if (_txtSearch.Text == "") { _txtSearch.Text = "Barkod veya ürün adı yazın..."; _txtSearch.ForeColor = Color.FromArgb(100, 116, 139); } };
            _txtSearch.Text = "Barkod veya ürün adı yazın...";
            _txtSearch.TextChanged += (s, e) => FilterData();
            filterControls.Controls.Add(_txtSearch);

            // ====== CONTENT: Grid ======
            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(24, 16, 24, 24) };
            this.Controls.Add(content);

            _gridSales = CreateGrid();
            _gridSales.Columns.Add("Product", "ÜRÜN ADI"); _gridSales.Columns["Product"].FillWeight = 200;
            _gridSales.Columns.Add("Barcode", "BARKOD"); _gridSales.Columns["Barcode"].Width = 110;
            _gridSales.Columns.Add("SaleId", "FİŞ NO"); _gridSales.Columns["SaleId"].Width = 90;
            _gridSales.Columns.Add("Date", "TARİH"); _gridSales.Columns["Date"].Width = 150;
            _gridSales.Columns.Add("Price", "TUTAR"); _gridSales.Columns["Price"].Width = 110; _gridSales.Columns["Price"].DefaultCellStyle.Format = "C2";

            content.Controls.Add(_gridSales);
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
