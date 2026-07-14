using System;
using System.Drawing;
using System.Windows.Forms;
using MN_Barcode.Business;
using System.Linq;

namespace MN_Barcode.WinForms
{
    public class ReturnsForm : Form
    {
        private SaleService _saleService;
        private DataGridView _gridReturns;
        private DataGridView _gridTopReturns;
        private DateTimePicker _dtStart;
        private DateTimePicker _dtEnd;
        private Label _lblTotalReturnAmount;

        public ReturnsForm()
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

            // HEADER (Kırmızımsı tema - İade olduğu için)
            Panel header = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(153, 27, 27) }; // Koyu kırmızı
            this.Controls.Add(header);

            Label title = new Label { Text = "↩️ İADE İŞLEMLERİ", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.White, Location = new Point(30, 25), AutoSize = true };
            header.Controls.Add(title);

            // Tarih Seçimi
            _dtStart = new DateTimePicker { Location = new Point(300, 30), Format = DateTimePickerFormat.Short, Width = 120, Font = new Font("Segoe UI", 11) };
            _dtStart.Value = DateTime.Now.AddDays(-30);
            header.Controls.Add(_dtStart);

            Label lblTo = new Label { Text = "➡", ForeColor = Color.White, Location = new Point(430, 30), AutoSize = true, Font = new Font("Segoe UI", 11) };
            header.Controls.Add(lblTo);

            _dtEnd = new DateTimePicker { Location = new Point(460, 30), Format = DateTimePickerFormat.Short, Width = 120, Font = new Font("Segoe UI", 11) };
            header.Controls.Add(_dtEnd);

            Button btnFilter = new Button { Text = "Filtrele", Location = new Point(600, 28), Size = new Size(100, 35), BackColor = Color.White, ForeColor = Color.FromArgb(153, 27, 27), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnFilter.FlatAppearance.BorderSize = 0;
            btnFilter.Click += (s, e) => LoadData();
            header.Controls.Add(btnFilter);

            // Özet Bilgiler
            _lblTotalReturnAmount = new Label { Text = "İade Toplamı: ₺0.00", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.White, Location = new Point(header.Width - 350, 25), Anchor = AnchorStyles.Top | AnchorStyles.Right, AutoSize = true };
            header.Controls.Add(_lblTotalReturnAmount);

            // İÇERİK
            TableLayoutPanel content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(20) };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            this.Controls.Add(content);
            content.BringToFront();

            // SOL: İADE LİSTESİ
            Panel leftPanel = CreateCardPanel("🧾 İADE HAREKETLERİ");
            content.Controls.Add(leftPanel, 0, 0);

            _gridReturns = CreateGrid();
            _gridReturns.Columns.Add("Id", "ID"); _gridReturns.Columns["Id"].Visible = false;
            _gridReturns.Columns.Add("Date", "TARİH"); _gridReturns.Columns["Date"].Width = 150;
            _gridReturns.Columns.Add("Code", "FİŞ NO"); _gridReturns.Columns["Code"].Width = 120;
            _gridReturns.Columns.Add("Amount", "İADE TUTARI"); _gridReturns.Columns["Amount"].DefaultCellStyle.Format = "C2"; _gridReturns.Columns["Amount"].DefaultCellStyle.ForeColor = Color.Red;
            
            Panel gridContainer1 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15, 0, 15, 15) };
            gridContainer1.Controls.Add(_gridReturns);
            leftPanel.Controls.Add(gridContainer1);
            gridContainer1.BringToFront();

            // SAĞ: EN ÇOK İADE EDİLENLER
            Panel rightPanel = CreateCardPanel("⚠️ EN ÇOK İADE EDİLENLER");
            content.Controls.Add(rightPanel, 1, 0);

            _gridTopReturns = CreateGrid();
            _gridTopReturns.Columns.Add("Name", "ÜRÜN ADI"); _gridTopReturns.Columns["Name"].FillWeight = 150;
            _gridTopReturns.Columns.Add("Qty", "ADET"); _gridTopReturns.Columns["Qty"].FillWeight = 50;
            _gridTopReturns.Columns.Add("Rev", "İADE TUTARI"); _gridTopReturns.Columns["Rev"].DefaultCellStyle.Format = "C2"; _gridTopReturns.Columns["Rev"].FillWeight = 80;

            Panel gridContainer2 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15, 0, 15, 15) };
            gridContainer2.Controls.Add(_gridTopReturns);
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

            // İade Listesi
            _gridReturns.Rows.Clear();
            var returns = _saleService.GetReturnsHistory(start, end);
            foreach (var r in returns)
            {
                _gridReturns.Rows.Add(r.Id, r.CreatedDate?.ToString("dd.MM.yyyy HH:mm"), r.TransactionCode, Math.Abs(r.TotalAmount)); // Pozitif göster
            }

            // Toplam İade Tutarı
            decimal totalRet = returns.Sum(x => Math.Abs(x.TotalAmount));
            _lblTotalReturnAmount.Text = $"İade Toplamı: {totalRet:C2}";

            // Top Returned Items
            _gridTopReturns.Rows.Clear();
            var top = _saleService.GetTopReturnedProducts(10, start, end);
            foreach (var t in top)
            {
                _gridTopReturns.Rows.Add(t.ProductName, t.TotalQuantity, t.TotalRevenue);
            }
        }
    }
}
