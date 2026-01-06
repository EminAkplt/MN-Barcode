using System;
using System.Drawing;
using System.Windows.Forms;
using MN_Barcode.Business;
using System.Linq;

namespace MN_Barcode.WinForms
{
    public class StockDashboardForm : Form
    {
        private ProductService _productService;
        private SaleService _saleService;

        private DataGridView _gridTopSelling;
        private DataGridView _gridLowStock;
        private Label _lblTodaySales;
        private Label _lblTodayCount;

        public StockDashboardForm()
        {
            _productService = new ProductService();
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

            // ═══════════════════════════════════════════════════════════
            // HEADER
            // ═══════════════════════════════════════════════════════════
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(45, 55, 72)
            };
            this.Controls.Add(header);

            Label title = new Label
            {
                Text = "📊 STOK DASHBOARD",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(30, 20),
                AutoSize = true
            };
            header.Controls.Add(title);

            // Bugünkü Satış Bilgileri
            _lblTodaySales = new Label
            {
                Text = "Bugün: ₺0",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(72, 187, 120),
                Location = new Point(350, 22),
                AutoSize = true
            };
            header.Controls.Add(_lblTodaySales);

            _lblTodayCount = new Label
            {
                Text = "(0 satış)",
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.FromArgb(160, 174, 192),
                Location = new Point(550, 25),
                AutoSize = true
            };
            header.Controls.Add(_lblTodayCount);

            // Yenile Butonu
            Button btnRefresh = new Button
            {
                Text = "🔄 Yenile",
                Size = new Size(120, 40),
                Location = new Point(header.Width - 150, 15),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                BackColor = Color.FromArgb(66, 153, 225),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => LoadData();
            header.Controls.Add(btnRefresh);

            // ═══════════════════════════════════════════════════════════
            // İÇERİK - 2 SÜTUN
            // ═══════════════════════════════════════════════════════════
            TableLayoutPanel content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(20)
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.Controls.Add(content);
            content.BringToFront();

            // SOL PANEL - EN ÇOK SATILAN
            Panel leftPanel = CreateCardPanel("🏆 EN ÇOK SATILAN ÜRÜNLER");
            content.Controls.Add(leftPanel, 0, 0);

            _gridTopSelling = CreateGrid();
            _gridTopSelling.Columns.Add("Rank", "#");
            _gridTopSelling.Columns["Rank"].Width = 50;
            _gridTopSelling.Columns.Add("Name", "ÜRÜN ADI");
            _gridTopSelling.Columns["Name"].FillWeight = 150;
            _gridTopSelling.Columns.Add("Quantity", "SATILAN ADET");
            _gridTopSelling.Columns["Quantity"].FillWeight = 80;
            _gridTopSelling.Columns.Add("Revenue", "TOPLAM CİRO");
            _gridTopSelling.Columns["Revenue"].FillWeight = 80;

            Panel gridContainer1 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15, 0, 15, 15) };
            gridContainer1.Controls.Add(_gridTopSelling);
            leftPanel.Controls.Add(gridContainer1);
            gridContainer1.BringToFront();

            // SAĞ PANEL - STOĞU AZALAN
            Panel rightPanel = CreateCardPanel("⚠️ STOĞU AZALAN ÜRÜNLER");
            content.Controls.Add(rightPanel, 1, 0);

            _gridLowStock = CreateGrid();
            _gridLowStock.Columns.Add("Name", "ÜRÜN ADI");
            _gridLowStock.Columns["Name"].FillWeight = 150;
            _gridLowStock.Columns.Add("Category", "KATEGORİ");
            _gridLowStock.Columns["Category"].FillWeight = 80;
            _gridLowStock.Columns.Add("Stock", "STOK");
            _gridLowStock.Columns["Stock"].FillWeight = 60;

            Panel gridContainer2 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15, 0, 15, 15) };
            gridContainer2.Controls.Add(_gridLowStock);
            rightPanel.Controls.Add(gridContainer2);
            gridContainer2.BringToFront();
        }

        private Panel CreateCardPanel(string title)
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(10)
            };

            Label lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 55, 72),
                Dock = DockStyle.Top,
                Height = 50,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 10, 0, 0)
            };
            panel.Controls.Add(lbl);

            return panel;
        }

        private DataGridView CreateGrid()
        {
            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true
            };
            grid.RowTemplate.Height = 45;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 11);
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(45, 55, 72);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(237, 242, 247);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(45, 55, 72);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 250, 252);

            grid.ColumnHeadersHeight = 45;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(113, 128, 150);
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(237, 242, 247);
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.GridColor = Color.FromArgb(226, 232, 240);

            return grid;
        }

        private void LoadData()
        {
            // Bugünkü satışlar
            decimal todayTotal = _saleService.GetTodaySalesTotal();
            int todayCount = _saleService.GetTodaySalesCount();
            _lblTodaySales.Text = $"Bugün: {todayTotal:₺#,##0.00}";
            _lblTodayCount.Text = $"({todayCount} satış)";

            // En çok satılanlar
            _gridTopSelling.Rows.Clear();
            var topSelling = _saleService.GetTopSellingProducts(10);
            int rank = 1;
            foreach (var item in topSelling)
            {
                _gridTopSelling.Rows.Add(rank++, item.ProductName, item.TotalQuantity.ToString("N0"), item.TotalRevenue.ToString("₺#,##0.00"));
            }

            // Stoğu azalanlar
            _gridLowStock.Rows.Clear();
            var lowStock = _productService.GetLowStockProducts(10, 20);
            foreach (var item in lowStock)
            {
                int idx = _gridLowStock.Rows.Add(item.Name, item.Category?.Name ?? "-", item.StockQuantity.ToString("N0"));
                
                // Stok renklendirme
                if (item.StockQuantity <= 5)
                {
                    _gridLowStock.Rows[idx].Cells[2].Style.ForeColor = Color.FromArgb(229, 62, 62);
                    _gridLowStock.Rows[idx].Cells[2].Style.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                }
                else
                {
                    _gridLowStock.Rows[idx].Cells[2].Style.ForeColor = Color.FromArgb(237, 137, 54);
                }
            }
        }
    }
}
