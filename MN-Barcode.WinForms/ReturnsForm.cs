using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MN_Barcode.Business;
using MN_Barcode.Entities;

namespace MN_Barcode.WinForms
{
    public class ReturnsForm : Form
    {
        private SaleService _saleService;
        private DataGridView _gridReturns;
        private DateTimePicker _dtStart;
        private DateTimePicker _dtEnd;
        private Label _lblTotalReturnAmount;
        private TextBox _txtSearch;

        private static readonly Color[] RowColors = new[]
        {
            Color.FromArgb(255, 240, 240),  // Açık kırmızı
            Color.FromArgb(240, 255, 240),  // Açık yeşil
            Color.FromArgb(255, 250, 240)   // Açık krem
        };

        public ReturnsForm()
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
            Label title = new Label { Text = "İade İşlemleri", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(220, 38, 38), AutoSize = true };
            title.Location = new Point(24, 12);
            header.Controls.Add(title);

            _lblTotalReturnAmount = new Label { Text = "İade Toplamı: ₺0.00", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.FromArgb(220, 38, 38), AutoSize = true };
            _lblTotalReturnAmount.Location = new Point(header.Width - 250, 12);
            header.Controls.Add(_lblTotalReturnAmount);

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
                Value = DateTime.Today.AddDays(-30),
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
            _gridReturns = CreateGrid();
            _gridReturns.Columns.Add("Product", "ÜRÜN ADI"); _gridReturns.Columns["Product"].FillWeight = 200;
            _gridReturns.Columns.Add("Barcode", "BARKOD"); _gridReturns.Columns["Barcode"].Width = 110;
            _gridReturns.Columns.Add("SaleId", "FİŞ NO"); _gridReturns.Columns["SaleId"].Width = 90;
            _gridReturns.Columns.Add("Date", "TARİH"); _gridReturns.Columns["Date"].Width = 150;
            _gridReturns.Columns.Add("Amount", "İADE TUTARI"); _gridReturns.Columns["Amount"].Width = 110; _gridReturns.Columns["Amount"].DefaultCellStyle.Format = "C2";
            _gridReturns.Dock = DockStyle.Fill;
            _gridReturns.BackgroundColor = Color.FromArgb(248, 250, 252);
            this.Controls.Add(_gridReturns);
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

            _gridReturns.Rows.Clear();
            var details = _saleService.GetReturnsHistoryWithDetails(start, end);

            var grouped = details.GroupBy(x => x.Sale.TransactionCode).ToList();
            int colorIndex = 0;

            foreach (var group in grouped)
            {
                Color rowColor = RowColors[colorIndex % RowColors.Length];
                colorIndex++;

                foreach (var detail in group)
                {
                    int rowIdx = _gridReturns.Rows.Add(
                        detail.Product?.Name ?? "",
                        detail.Product?.Barcode ?? "",
                        detail.Sale.TransactionCode,
                        detail.Sale.CreatedDate?.ToString("dd.MM.yyyy HH:mm"),
                        Math.Abs(detail.TotalPrice) // Pozitif olarak göster
                    );

                    // Satırın arka plan rengini ayarla
                    for (int i = 0; i < _gridReturns.Columns.Count; i++)
                    {
                        _gridReturns.Rows[rowIdx].Cells[i].Style.BackColor = rowColor;
                    }
                }
            }

            // Toplam İade Tutarı
            decimal totalRet = details.Sum(x => Math.Abs(x.TotalPrice));
            _lblTotalReturnAmount.Text = $"İade Toplamı: {totalRet:C2}";
        }

        private void FilterData()
        {
            string searchText = _txtSearch.Text.Trim().ToLower();
            if (searchText == "barkod veya ürün adı yazın..." || searchText == "")
            {
                foreach (DataGridViewRow row in _gridReturns.Rows)
                    row.Visible = true;
                return;
            }
            foreach (DataGridViewRow row in _gridReturns.Rows)
            {
                string barcode = row.Cells["Barcode"].Value?.ToString()?.ToLower() ?? "";
                string product = row.Cells["Product"].Value?.ToString()?.ToLower() ?? "";
                row.Visible = barcode.Contains(searchText) || product.Contains(searchText);
            }
        }
    }
}
