using System;
using System.Drawing;
using System.Windows.Forms;
using MN_Barcode.Business;

namespace MN_Barcode.WinForms
{
    public class StockDashboardForm : Form
    {
        private ProductService _productService;
        private SaleService    _saleService;

        private DataGridView _gridTopSelling;
        private DataGridView _gridLowStock;
        private Label        _lblTodaySales;
        private Label        _lblTodayCount;

        public StockDashboardForm()
        {
            _productService = new ProductService();
            _saleService    = new SaleService();
            this.DoubleBuffered = true;
            InitUI();
            this.Shown += (s, e) => LoadData();
        }

        // ════════════════════════════════════════════════════════════
        private void InitUI()
        {
            this.BackColor       = Theme.Background;
            this.Dock            = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;

            // ── HEADER ──────────────────────────────────────────────
            Panel header = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 60,
                BackColor = Theme.Surface
            };
            header.Paint += (s, e) =>
            {
                using (var p = new Pen(Theme.Border, 1))
                    e.Graphics.DrawLine(p, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            this.Controls.Add(header);

            Label title = new Label
            {
                Text      = "Stok Dashboard",
                Font      = Theme.H1,
                ForeColor = Theme.TextPrimary,
                AutoSize  = true,
                Location  = new Point(Theme.PagePad, 14),
                BackColor = Color.Transparent
            };
            header.Controls.Add(title);

            // Bugünkü satış bilgileri
            _lblTodaySales = new Label
            {
                Text      = "Bugün: ₺0",
                Font      = Theme.H2,
                ForeColor = Theme.Success,
                AutoSize  = true,
                Location  = new Point(Theme.PagePad + 200, 18),
                BackColor = Color.Transparent
            };
            header.Controls.Add(_lblTodaySales);

            _lblTodayCount = new Label
            {
                Text      = "(0 satış)",
                Font      = Theme.Body,
                ForeColor = Theme.TextSecond,
                AutoSize  = true,
                Location  = new Point(Theme.PagePad + 400, 20),
                BackColor = Color.Transparent
            };
            header.Controls.Add(_lblTodayCount);

            // Yenile butonu (sağda)
            Button btnRefresh = Theme.MakeButton("  Yenile", Theme.SurfaceAlt, Theme.TextPrimary, 110, 36, 10);
            btnRefresh.Anchor   = AnchorStyles.Right | AnchorStyles.Top;
            btnRefresh.Location = new Point(header.Width - 130, 12);
            btnRefresh.FlatAppearance.BorderColor         = Theme.Border;
            btnRefresh.FlatAppearance.BorderSize          = 1;
            btnRefresh.FlatAppearance.MouseOverBackColor  = Theme.Surface;
            btnRefresh.Click += (s, e) => LoadData();
            header.Controls.Add(btnRefresh);
            header.Resize += (s, e) => btnRefresh.Location = new Point(header.Width - 130, 12);

            // ── İÇERİK ─────────────────────────────────────────────
            TableLayoutPanel content = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 1,
                Padding     = new Padding(Theme.PagePad, Theme.Gap, Theme.PagePad, Theme.PagePad)
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.Controls.Add(content);
            content.BringToFront();

            // Sol: En çok satılanlar
            Panel left = Theme.MakeCard("En Çok Satılan Ürünler", Theme.Accent);
            content.Controls.Add(left, 0, 0);

            _gridTopSelling = Theme.MakeDarkGrid();
            _gridTopSelling.Columns.Add("Rank",     "#");           _gridTopSelling.Columns["Rank"].Width      = 48;
            _gridTopSelling.Columns.Add("Name",     "ÜRÜN ADI");    _gridTopSelling.Columns["Name"].FillWeight = 150;
            _gridTopSelling.Columns.Add("Quantity", "SATILAN ADET");_gridTopSelling.Columns["Quantity"].FillWeight = 80;
            _gridTopSelling.Columns.Add("Revenue",  "TOPLAM CİRO"); _gridTopSelling.Columns["Revenue"].FillWeight  = 80;
            _gridTopSelling.Columns["Revenue"].DefaultCellStyle.ForeColor = Theme.Success;
            _gridTopSelling.Columns["Revenue"].DefaultCellStyle.Font      = new Font("Consolas", 10, FontStyle.Bold);

            Panel gw1 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 8) };
            gw1.Controls.Add(_gridTopSelling);
            left.Controls.Add(gw1);

            // Sağ: Stoğu azalanlar
            Panel right = Theme.MakeCard("Stoğu Azalan Ürünler", Theme.Warning);
            content.Controls.Add(right, 1, 0);

            _gridLowStock = Theme.MakeDarkGrid();
            _gridLowStock.Columns.Add("Name",     "ÜRÜN ADI");  _gridLowStock.Columns["Name"].FillWeight     = 150;
            _gridLowStock.Columns.Add("Category", "KATEGORİ"); _gridLowStock.Columns["Category"].FillWeight  = 80;
            _gridLowStock.Columns.Add("Stock",    "STOK");      _gridLowStock.Columns["Stock"].FillWeight     = 60;

            Panel gw2 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 8) };
            gw2.Controls.Add(_gridLowStock);
            right.Controls.Add(gw2);
        }

        // ════════════════════════════════════════════════════════════
        //  VERİ YÜKLEME
        // ════════════════════════════════════════════════════════════
        private void LoadData()
        {
            decimal todayTotal = _saleService.GetTodaySalesTotal();
            int     todayCount = _saleService.GetTodaySalesCount();
            _lblTodaySales.Text = $"Bugün: {todayTotal:₺#,##0.00}";
            _lblTodayCount.Text = $"({todayCount} satış)";

            _gridTopSelling.Rows.Clear();
            int rank = 1;
            foreach (var item in _saleService.GetTopSellingProducts(10))
                _gridTopSelling.Rows.Add(rank++, item.ProductName,
                    item.TotalQuantity.ToString("N0"),
                    item.TotalRevenue.ToString("₺#,##0.00"));

            _gridLowStock.Rows.Clear();
            foreach (var item in _productService.GetLowStockProducts(10, 20))
            {
                int idx = _gridLowStock.Rows.Add(item.Name, item.Category?.Name ?? "-",
                    item.StockQuantity.ToString("N0"));
                var cell = _gridLowStock.Rows[idx].Cells[2];
                cell.Style.ForeColor = item.StockQuantity <= 5 ? Theme.Danger : Theme.Warning;
                if (item.StockQuantity <= 5)
                    cell.Style.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            }
        }
    }
}
