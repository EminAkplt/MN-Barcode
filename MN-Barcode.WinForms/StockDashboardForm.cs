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
            this.BackColor = Theme.Background;
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;

            Panel header = UiHelpers.StyleHeaderPanel(new Panel(), "📊 STOK DASHBOARD");
            this.Controls.Add(header);

            _lblTodaySales = new Label { Text = "Bugün: ₺0", Font = Theme.H2, ForeColor = Theme.Money, Location = new Point(350, 22), AutoSize = true };
            header.Controls.Add(_lblTodaySales);

            _lblTodayCount = new Label { Text = "(0 satış)", Font = Theme.Body, ForeColor = Theme.TextMuted, Location = new Point(550, 25), AutoSize = true };
            header.Controls.Add(_lblTodayCount);

            Button btnRefresh = new Button
            {
                Text = "🔄 Yenile",
                Size = new Size(120, 40),
                Location = new Point(header.Width - 150, 15),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(Theme.FontName, 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => LoadData();
            header.Controls.Add(btnRefresh);

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

            Panel leftPanel = UiHelpers.CreateCardPanel("🏆 EN ÇOK SATILAN ÜRÜNLER", Theme.Success);
            content.Controls.Add(leftPanel, 0, 0);

            _gridTopSelling = UiHelpers.CreateGrid();
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

            Panel rightPanel = UiHelpers.CreateCardPanel("⚠️ STOĞU AZALAN ÜRÜNLER", Theme.Warning);
            content.Controls.Add(rightPanel, 1, 0);

            _gridLowStock = UiHelpers.CreateGrid();
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
                
                if (item.StockQuantity <= 5)
                {
                    _gridLowStock.Rows[idx].Cells[2].Style.ForeColor = Theme.Danger;
                    _gridLowStock.Rows[idx].Cells[2].Style.Font = new Font(Theme.FontName, 11, FontStyle.Bold);
                }
                else
                {
                    _gridLowStock.Rows[idx].Cells[2].Style.ForeColor = Theme.Warning;
                }
            }
        }
    }
}
