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
            Color.FromArgb(255, 240, 240),  // Çok açık kırmızı
            Color.FromArgb(240, 255, 240),  // Çok açık yeşil
            Color.FromArgb(255, 250, 240)   // Çok açık krem
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
            this.BackColor = Theme.Background;
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;

            // HEADER
            Panel header = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Theme.Surface, Padding = new Padding(Theme.PagePad, 0, Theme.PagePad, 0) };
            this.Controls.Add(header);
            header.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.Border, 1))
                    e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };

            Label title = new Label { Text = "İade İşlemleri", Font = Theme.H1, ForeColor = Theme.Danger, AutoSize = true, Location = new Point(Theme.PagePad, 18) };
            header.Controls.Add(title);

            // Sağ taraftaki kontroller
            FlowLayoutPanel controls = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, BackColor = Color.Transparent, Padding = new Padding(0, 14, 0, 0) };
            header.Controls.Add(controls);

            _dtStart = BuildStyledDateTimePicker(DateTime.Today.AddDays(-30), () => LoadData());
            controls.Controls.Add(_dtStart);

            controls.Controls.Add(new Label { Text = "–", Font = Theme.Body, ForeColor = Theme.TextMuted, AutoSize = true, Margin = new Padding(6, 10, 6, 0) });

            _dtEnd = BuildStyledDateTimePicker(DateTime.Today, () => LoadData());
            controls.Controls.Add(_dtEnd);

            // Arama kutusu
            _txtSearch = new TextBox { Width = 120, Height = 32, Font = Theme.Body, Margin = new Padding(8, 0, 0, 0), Text = "Barkod..." };
            _txtSearch.GotFocus += (s, e) => { if (_txtSearch.Text == "Barkod...") _txtSearch.Text = ""; };
            _txtSearch.LostFocus += (s, e) => { if (_txtSearch.Text == "") _txtSearch.Text = "Barkod..."; };
            _txtSearch.TextChanged += (s, e) => FilterData();
            controls.Controls.Add(_txtSearch);

            // İÇERİK
            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background, Padding = new Padding(Theme.PagePad) };
            this.Controls.Add(content);

            // Panel başlığı + özet
            Panel headerPanel = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.Transparent };
            content.Controls.Add(headerPanel);

            Label panelTitle = new Label { Text = "İade Edilen Ürünler (Her satır = 1 ürün, aynı renk = aynı fiş)", Font = Theme.H2, ForeColor = Theme.TextDark, AutoSize = true, Location = new Point(0, 0) };
            headerPanel.Controls.Add(panelTitle);

            _lblTotalReturnAmount = new Label { Text = "İade Toplamı: ₺0.00", Font = Theme.H2, ForeColor = Theme.Danger, AutoSize = true, Dock = DockStyle.Right };
            headerPanel.Controls.Add(_lblTotalReturnAmount);

            // Grid
            Panel gridPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Padding = new Padding(0, Theme.Gap, 0, 0) };
            content.Controls.Add(gridPanel);

            _gridReturns = CreateGrid();
            _gridReturns.Columns.Add("SaleId", "FİŞ NO"); _gridReturns.Columns["SaleId"].Width = 80;
            _gridReturns.Columns.Add("Date", "TARİH"); _gridReturns.Columns["Date"].Width = 140;
            _gridReturns.Columns.Add("Barcode", "BARCODE"); _gridReturns.Columns["Barcode"].Width = 100;
            _gridReturns.Columns.Add("Product", "ÜRÜN ADI"); _gridReturns.Columns["Product"].FillWeight = 200;
            _gridReturns.Columns.Add("Qty", "ADET"); _gridReturns.Columns["Qty"].Width = 60;
            _gridReturns.Columns.Add("Amount", "İADE TUTARI"); _gridReturns.Columns["Amount"].Width = 100; _gridReturns.Columns["Amount"].DefaultCellStyle.Format = "C2";

            Panel gridContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(1) };
            gridContainer.Controls.Add(_gridReturns);
            gridPanel.Controls.Add(gridContainer);
        }

        private DateTimePicker BuildStyledDateTimePicker(DateTime initialValue, Action onChanged)
        {
            DateTimePicker dtp = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 105,
                Font = Theme.Body,
                Value = initialValue,
                BackColor = Theme.Background,
                ForeColor = Theme.TextDark
            };
            dtp.ValueChanged += (s, e) => onChanged();
            return dtp;
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
                        detail.Sale.TransactionCode,
                        detail.Sale.CreatedDate?.ToString("dd.MM.yyyy HH:mm"),
                        detail.Product?.Barcode ?? "",
                        detail.Product?.Name ?? "",
                        Math.Abs(detail.Quantity),  // Pozitif olarak göster
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
            string searchText = _txtSearch.Text.ToLower();
            foreach (DataGridViewRow row in _gridReturns.Rows)
            {
                string barcode = row.Cells["Barcode"].Value?.ToString()?.ToLower() ?? "";
                string product = row.Cells["Product"].Value?.ToString()?.ToLower() ?? "";
                row.Visible = barcode.Contains(searchText) || product.Contains(searchText);
            }
        }
    }
}
