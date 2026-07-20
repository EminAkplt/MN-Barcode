using System;
using System.Drawing;
using System.Windows.Forms;
using MN_Barcode.Business;
using System.Linq;

namespace MN_Barcode.WinForms
{
    /// <summary>
    /// STOK DASHBOARD — sade, klasik tema.
    /// Solda en çok satılan ürünler, sağda stoğu azalan ürünler.
    /// </summary>
    public class StockDashboardForm : Form, IEkranYenileme
    {
        /// <summary>Önbellekten tekrar açıldığında verileri tazeler (bkz. IEkranYenileme).</summary>
        public void EkraniYenile() => LoadData();

        private static readonly Color ColBg       = Color.FromArgb(245, 247, 250);
        private static readonly Color ColSurface  = Color.White;
        private static readonly Color ColBorder   = Color.FromArgb(214, 221, 230);
        private static readonly Color ColText      = Color.FromArgb(30, 41, 59);
        private static readonly Color ColStrong   = Color.FromArgb(15, 23, 42);
        private static readonly Color ColMuted    = Color.FromArgb(100, 116, 139);
        private static readonly Color ColHeaderBg = Color.FromArgb(241, 245, 249);
        private static readonly Color ColBlue      = Color.FromArgb(37, 99, 235);
        private static readonly Color ColBlueDark = Color.FromArgb(29, 78, 188);
        private static readonly Color ColBlueSoft = Color.FromArgb(232, 240, 254);
        private static readonly Color ColGreen     = Color.FromArgb(22, 163, 74);
        private static readonly Color ColAmber     = Color.FromArgb(217, 119, 6);
        private static readonly Color ColRed       = Color.FromArgb(220, 38, 38);

        private ProductService _productService;
        private SaleService _saleService;

        private DataGridView _gridTopSelling;
        private DataGridView _gridLowStock;
        private Label _lblTodaySales;

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
            this.BackColor = ColBg;
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;

            // ── İÇERİK (önce eklenir) ──
            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = ColBg, Padding = new Padding(16) };
            this.Controls.Add(content);

            TableLayoutPanel cols = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = ColBg
            };
            cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            content.Controls.Add(cols);

            // Sol kart: En çok satılan
            _gridTopSelling = CreateGrid();
            _gridTopSelling.Columns.Add("Rank", "#"); _gridTopSelling.Columns["Rank"].FillWeight = 30;
            _gridTopSelling.Columns["Rank"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _gridTopSelling.Columns.Add("Name", "ÜRÜN ADI"); _gridTopSelling.Columns["Name"].FillWeight = 150;
            _gridTopSelling.Columns.Add("Quantity", "ADET"); _gridTopSelling.Columns["Quantity"].FillWeight = 70;
            _gridTopSelling.Columns["Quantity"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _gridTopSelling.Columns.Add("Revenue", "CİRO"); _gridTopSelling.Columns["Revenue"].FillWeight = 90;
            _gridTopSelling.Columns["Revenue"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _gridTopSelling.Columns["Revenue"].DefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            _gridTopSelling.Columns["Revenue"].DefaultCellStyle.ForeColor = ColGreen;
            cols.Controls.Add(BuildCard("En Çok Satılan Ürünler", ColGreen, _gridTopSelling, new Padding(0, 0, 8, 0)), 0, 0);

            // Sağ kart: Stoğu azalan
            _gridLowStock = CreateGrid();
            _gridLowStock.Columns.Add("Name", "ÜRÜN ADI"); _gridLowStock.Columns["Name"].FillWeight = 150;
            _gridLowStock.Columns.Add("Category", "KATEGORİ"); _gridLowStock.Columns["Category"].FillWeight = 90;
            _gridLowStock.Columns.Add("Stock", "STOK"); _gridLowStock.Columns["Stock"].FillWeight = 55;
            _gridLowStock.Columns["Stock"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _gridLowStock.Columns["Stock"].DefaultCellStyle.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            cols.Controls.Add(BuildCard("Stoğu Azalan Ürünler", ColAmber, _gridLowStock, new Padding(8, 0, 0, 0)), 1, 0);

            // ── HEADER (en son eklenir → en üstte) ──
            this.Controls.Add(BuildHeader());
        }

        private Panel BuildHeader()
        {
            Panel header = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = ColSurface };
            header.Paint += (s, e) =>
            {
                using var pen = new Pen(ColBorder, 1);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };

            TableLayoutPanel row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = ColSurface,
                Padding = new Padding(24, 0, 24, 0)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.Controls.Add(row);

            Label title = new Label
            {
                Text = "Stok Dashboard",
                Font = new Font("Segoe UI", 17, FontStyle.Bold),
                ForeColor = ColStrong,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };
            row.Controls.Add(title, 0, 0);

            _lblTodaySales = new Label
            {
                Text = "Bugün: ₺0,00",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = ColGreen,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(20, 0, 0, 0)
            };
            row.Controls.Add(_lblTodaySales, 1, 0);

            Button btnRefresh = new Button
            {
                Text = "Yenile",
                Size = new Size(110, 38),
                BackColor = ColBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.FlatAppearance.MouseOverBackColor = ColBlueDark;
            btnRefresh.Click += (s, e) => LoadData();
            row.Controls.Add(btnRefresh, 2, 0);

            return header;
        }

        // Başlık şeritli + üst renk vurgulu beyaz kart, içinde grid.
        private Panel BuildCard(string title, Color accent, DataGridView grid, Padding margin)
        {
            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = ColSurface, Margin = margin };
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(ColBorder, 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                using (var brush = new SolidBrush(accent))
                    e.Graphics.FillRectangle(brush, 0, 0, card.Width, 3);
            };

            // Grid barındırıcı (önce eklenir → kalan alan)
            Panel host = new Panel { Dock = DockStyle.Fill, BackColor = ColSurface, Padding = new Padding(12, 0, 12, 12) };
            host.Controls.Add(grid);
            card.Controls.Add(host);

            Label lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = ColStrong,
                Dock = DockStyle.Top,
                Height = 50,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 3, 0, 0)
            };
            card.Controls.Add(lbl);

            return card;
        }

        private DataGridView CreateGrid()
        {
            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = ColSurface,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                AllowUserToOrderColumns = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(233, 238, 244)
            };
            grid.RowTemplate.Height = 42;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            grid.DefaultCellStyle.ForeColor = ColText;
            grid.DefaultCellStyle.BackColor = ColSurface;
            grid.DefaultCellStyle.SelectionBackColor = ColBlueSoft;
            grid.DefaultCellStyle.SelectionForeColor = ColStrong;
            grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 253);

            grid.ColumnHeadersHeight = 40;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = ColMuted;
            grid.ColumnHeadersDefaultCellStyle.BackColor = ColHeaderBg;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            return grid;
        }

        private void LoadData()
        {
            // Veritabanı erişimi hata verirse (bağlantı koptu, disk doldu vb.)
            // eskiden istisna yakalanmadan yukarı çıkıp uygulamayı çökertiyordu.
            try
            {
                VerileriYukle();
            }
            catch (Exception ex)
            {
                AppLogger.Yaz("Stok ekranı yüklenemedi", ex);
                MessageBox.Show("Stok bilgileri yüklenemedi.\nAyrıntı hata kaydına yazıldı.",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void VerileriYukle()
        {
            // Bugünkü satışlar
            decimal todayTotal = _saleService.GetTodaySalesTotal();
            int todayCount = _saleService.GetTodaySalesCount();
            _lblTodaySales.Text = $"Bugün: {todayTotal:₺#,##0.00}  ({todayCount} satış)";

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
                    _gridLowStock.Rows[idx].Cells[2].Style.ForeColor = ColRed;
                else
                    _gridLowStock.Rows[idx].Cells[2].Style.ForeColor = ColAmber;
            }
        }
    }
}
