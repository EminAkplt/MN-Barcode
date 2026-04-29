using System;
using System.Drawing;
using System.Windows.Forms;
using MN_Barcode.Business;
using System.Linq;

namespace MN_Barcode.WinForms
{
    public class SalesHistoryForm : Form
    {
        private SaleService _saleService;
        private DataGridView _gridSales;
        private DataGridView _gridTopSelling;
        private DateTimePicker _dtStart;
        private DateTimePicker _dtEnd;
        private Label _lblTotalRevenue;

        public SalesHistoryForm()
        {
            _saleService = new SaleService();
            this.DoubleBuffered = true;
            InitUI();
            this.Shown += (s, e) => LoadData();
        }

        private void InitUI()
        {
            this.BackColor = Color.FromArgb(247, 250, 252);
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;

            // HEADER
            Panel header = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(45, 55, 72) };
            this.Controls.Add(header);

            Label title = new Label { Text = "💰 SATIŞ GEÇMİŞİ", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.White, Location = new Point(30, 25), AutoSize = true };
            header.Controls.Add(title);

            // Tarih Seçimi
            _dtStart = new DateTimePicker { Location = new Point(300, 30), Format = DateTimePickerFormat.Short, Width = 120, Font = new Font("Segoe UI", 11) };
            _dtStart.Value = DateTime.Now.AddDays(-7); // Son 7 gün varsayılan
            header.Controls.Add(_dtStart);

            Label lblTo = new Label { Text = "➡", ForeColor = Color.White, Location = new Point(430, 30), AutoSize = true, Font = new Font("Segoe UI", 11) };
            header.Controls.Add(lblTo);

            _dtEnd = new DateTimePicker { Location = new Point(460, 30), Format = DateTimePickerFormat.Short, Width = 120, Font = new Font("Segoe UI", 11) };
            header.Controls.Add(_dtEnd);

            Button btnFilter = new Button { Text = "Filtrele", Location = new Point(600, 28), Size = new Size(100, 35), BackColor = Color.FromArgb(66, 153, 225), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnFilter.FlatAppearance.BorderSize = 0;
            btnFilter.Click += (s, e) => LoadData();
            header.Controls.Add(btnFilter);

            // Özet Bilgiler (Header içinde)
            _lblTotalRevenue = new Label { Text = "Toplam: ₺0.00", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.FromArgb(72, 187, 120), Location = new Point(header.Width - 300, 25), Anchor = AnchorStyles.Top | AnchorStyles.Right, AutoSize = true };
            header.Controls.Add(_lblTotalRevenue);

            // İÇERİK
            TableLayoutPanel content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(20) };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F)); // Sol taraf %60 (Liste)
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F)); // Sağ taraf %40 (Top 10)
            this.Controls.Add(content);
            content.BringToFront();

            // SOL: SATIŞ LİSTESİ
            Panel leftPanel = CreateCardPanel("🧾 SATIŞ HAREKETLERİ");
            content.Controls.Add(leftPanel, 0, 0);

            _gridSales = CreateGrid();
            _gridSales.Columns.Add("Id", "ID"); _gridSales.Columns["Id"].Visible = false;
            _gridSales.Columns.Add("Date", "TARİH"); _gridSales.Columns["Date"].Width = 150;
            _gridSales.Columns.Add("Code", "FİŞ NO"); _gridSales.Columns["Code"].Width = 120;
            _gridSales.Columns.Add("Type", "ÖDEME"); _gridSales.Columns["Type"].Width = 100;
            _gridSales.Columns.Add("Amount", "TUTAR"); _gridSales.Columns["Amount"].DefaultCellStyle.Format = "C2";
            
            Panel gridContainer1 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15, 0, 15, 15) };
            gridContainer1.Controls.Add(_gridSales);
            leftPanel.Controls.Add(gridContainer1);
            gridContainer1.BringToFront();

            // SAĞ: EN ÇOK SATILANLAR
            Panel rightPanel = CreateCardPanel("🏆 DÖNEMİN YILDIZLARI (Top 10)");
            content.Controls.Add(rightPanel, 1, 0);

            _gridTopSelling = CreateGrid();
            _gridTopSelling.Columns.Add("Name", "ÜRÜN ADI"); _gridTopSelling.Columns["Name"].FillWeight = 150;
            _gridTopSelling.Columns.Add("Qty", "ADET"); _gridTopSelling.Columns["Qty"].FillWeight = 50;
            _gridTopSelling.Columns.Add("Rev", "CİRO"); _gridTopSelling.Columns["Rev"].DefaultCellStyle.Format = "C2"; _gridTopSelling.Columns["Rev"].FillWeight = 80;

            Panel gridContainer2 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15, 0, 15, 15) };
            gridContainer2.Controls.Add(_gridTopSelling);
            rightPanel.Controls.Add(gridContainer2);
            gridContainer2.BringToFront();
        }

        private Panel CreateCardPanel(string title)
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(10) };
            Label lbl = new Label { Text = title, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(45, 55, 72), Dock = DockStyle.Top, Height = 45, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(15, 0, 0, 0) };
            panel.Controls.Add(lbl);
            return panel;
        }

        private DataGridView CreateGrid()
        {
            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, ReadOnly = true,
                RowTemplate = { Height = 40 }
            };
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            grid.ColumnHeadersHeight = 45;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(237, 242, 247);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(100, 116, 139);
            grid.EnableHeadersVisualStyles = false;
            return grid;
        }

        private void LoadData()
        {
            DateTime start = _dtStart.Value.Date;
            DateTime end = _dtEnd.Value.Date.AddDays(1).AddSeconds(-1);

            // Satışlar
            _gridSales.Rows.Clear();
            var sales = _saleService.GetSalesHistory(start, end);
            foreach (var s in sales)
            {
                _gridSales.Rows.Add(s.Id, s.CreatedDate?.ToString("dd.MM.yyyy HH:mm"), s.TransactionCode, s.PaymentType, s.TotalAmount);
            }

            // Toplam Ciro
            decimal totalRev = sales.Sum(x => x.TotalAmount);
            _lblTotalRevenue.Text = $"Toplam: {totalRev:C2}";

            // Top Selling
            _gridTopSelling.Rows.Clear();
            var top = _saleService.GetTopSellingProducts(10, start, end);
            foreach (var t in top)
            {
                _gridTopSelling.Rows.Add(t.ProductName, t.TotalQuantity, t.TotalRevenue);
            }
        }
    }
}
