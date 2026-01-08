using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Columns;
using MN_Barcode.Business;

namespace MN_Barcode.WinForms
{
    public class StockDashboardFormDX : XtraForm
    {
        private ProductService _productService;
        private SaleService _saleService;

        private GridControl _gridTopSelling;
        private GridView _gridViewTopSelling;
        private GridControl _gridLowStock;
        private GridView _gridViewLowStock;
        private LabelControl _lblTodaySales;
        private LabelControl _lblTodayCount;

        public StockDashboardFormDX()
        {
            _productService = new ProductService();
            _saleService = new SaleService();
            this.DoubleBuffered = true;
            InitUI();

            this.Shown += (s, e) => LoadData();
        }

        private void InitUI()
        {
            this.Text = "Stok Dashboard";
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;
            this.LookAndFeel.SkinName = "Office 2019 Black";

            // ═══════════════════════════════════════════════════════════
            // HEADER - Basit Tasarım (SalesHistoryForm benzeri)
            // ═══════════════════════════════════════════════════════════
            PanelControl header = new PanelControl
            {
                Dock = DockStyle.Top,
                Height = 80,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
            };
            header.Appearance.BackColor = Color.FromArgb(45, 55, 72);
            this.Controls.Add(header);

            // Başlık
            LabelControl title = new LabelControl
            {
                Text = "📊 STOK DASHBOARD",
                Location = new Point(30, 25)
            };
            title.Appearance.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            title.Appearance.ForeColor = Color.White;
            header.Controls.Add(title);

            // Bugünkü Satış (Sağda)
            _lblTodaySales = new LabelControl
            {
                Text = "Bugün: ₺0",
                Location = new Point(350, 25),
                AutoSizeMode = LabelAutoSizeMode.Default
            };
            _lblTodaySales.Appearance.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            _lblTodaySales.Appearance.ForeColor = Color.FromArgb(72, 187, 120);
            header.Controls.Add(_lblTodaySales);

            // Satış Sayısı
            _lblTodayCount = new LabelControl
            {
                Text = "(0 satış)",
                Location = new Point(550, 28)
            };
            _lblTodayCount.Appearance.Font = new Font("Segoe UI", 12);
            _lblTodayCount.Appearance.ForeColor = Color.FromArgb(160, 174, 192);
            header.Controls.Add(_lblTodayCount);

            // Yenile Butonu
            SimpleButton btnRefresh = new SimpleButton
            {
                Text = "🔄 Yenile",
                Size = new Size(100, 35),
                Location = new Point(700, 22),
                Cursor = Cursors.Hand
            };
            btnRefresh.Appearance.BackColor = Color.FromArgb(66, 153, 225);
            btnRefresh.Appearance.ForeColor = Color.White;
            btnRefresh.Appearance.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnRefresh.Appearance.Options.UseBackColor = true;
            btnRefresh.Appearance.Options.UseForeColor = true;
            btnRefresh.Appearance.Options.UseFont = true;
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

            // ═══════════════════════════════════════════════════════════
            // SOL PANEL - EN ÇOK SATILAN
            // ═══════════════════════════════════════════════════════════
            PanelControl leftPanel = CreateCardPanel("🏆 EN ÇOK SATILAN ÜRÜNLER", Color.FromArgb(72, 187, 120));
            content.Controls.Add(leftPanel, 0, 0);

            // Grid Container
            PanelControl gridContainer1 = new PanelControl
            {
                Dock = DockStyle.Fill,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
                Padding = new Padding(15, 0, 15, 15)
            };
            leftPanel.Controls.Add(gridContainer1);
            gridContainer1.BringToFront();

            // Top Selling Grid
            _gridTopSelling = new GridControl { Dock = DockStyle.Fill };
            _gridViewTopSelling = new GridView(_gridTopSelling);
            _gridTopSelling.MainView = _gridViewTopSelling;

            _gridViewTopSelling.OptionsView.ShowGroupPanel = false;
            _gridViewTopSelling.OptionsView.ShowIndicator = false;
            _gridViewTopSelling.OptionsSelection.EnableAppearanceFocusedCell = false;
            _gridViewTopSelling.OptionsBehavior.Editable = false;
            _gridViewTopSelling.RowHeight = 45;

            _gridViewTopSelling.Columns.Add(new GridColumn { FieldName = "Rank", Caption = "#", VisibleIndex = 0, Width = 50 });
            _gridViewTopSelling.Columns.Add(new GridColumn { FieldName = "Name", Caption = "ÜRÜN ADI", VisibleIndex = 1, Width = 180 });
            _gridViewTopSelling.Columns.Add(new GridColumn { FieldName = "Quantity", Caption = "SATILAN ADET", VisibleIndex = 2, Width = 100 });
            _gridViewTopSelling.Columns.Add(new GridColumn { FieldName = "Revenue", Caption = "TOPLAM CİRO", VisibleIndex = 3, Width = 120 });

            // Sıralama kolonunu özel formatla
            _gridViewTopSelling.RowCellStyle += (s, e) =>
            {
                if (e.Column.FieldName == "Rank")
                {
                    var rank = Convert.ToInt32(e.CellValue);
                    if (rank == 1)
                    {
                        e.Appearance.BackColor = Color.FromArgb(255, 215, 0); // Altın
                        e.Appearance.Font = new Font("Segoe UI", 12, FontStyle.Bold);
                    }
                    else if (rank == 2)
                    {
                        e.Appearance.BackColor = Color.FromArgb(192, 192, 192); // Gümüş
                        e.Appearance.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                    }
                    else if (rank == 3)
                    {
                        e.Appearance.BackColor = Color.FromArgb(205, 127, 50); // Bronz
                        e.Appearance.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                    }
                }
                else if (e.Column.FieldName == "Revenue")
                {
                    e.Appearance.ForeColor = Color.FromArgb(56, 161, 105);
                    e.Appearance.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                }
            };

            gridContainer1.Controls.Add(_gridTopSelling);

            // ═══════════════════════════════════════════════════════════
            // SAĞ PANEL - STOĞU AZALAN
            // ═══════════════════════════════════════════════════════════
            PanelControl rightPanel = CreateCardPanel("⚠️ STOĞU AZALAN ÜRÜNLER", Color.FromArgb(237, 137, 54));
            content.Controls.Add(rightPanel, 1, 0);

            // Grid Container
            PanelControl gridContainer2 = new PanelControl
            {
                Dock = DockStyle.Fill,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
                Padding = new Padding(15, 0, 15, 15)
            };
            rightPanel.Controls.Add(gridContainer2);
            gridContainer2.BringToFront();

            // Low Stock Grid
            _gridLowStock = new GridControl { Dock = DockStyle.Fill };
            _gridViewLowStock = new GridView(_gridLowStock);
            _gridLowStock.MainView = _gridViewLowStock;

            _gridViewLowStock.OptionsView.ShowGroupPanel = false;
            _gridViewLowStock.OptionsView.ShowIndicator = false;
            _gridViewLowStock.OptionsSelection.EnableAppearanceFocusedCell = false;
            _gridViewLowStock.OptionsBehavior.Editable = false;
            _gridViewLowStock.RowHeight = 45;

            _gridViewLowStock.Columns.Add(new GridColumn { FieldName = "Name", Caption = "ÜRÜN ADI", VisibleIndex = 0, Width = 180 });
            _gridViewLowStock.Columns.Add(new GridColumn { FieldName = "Category", Caption = "KATEGORİ", VisibleIndex = 1, Width = 120 });
            _gridViewLowStock.Columns.Add(new GridColumn { FieldName = "Stock", Caption = "STOK", VisibleIndex = 2, Width = 80 });

            // Stok seviyesine göre renklendirme
            _gridViewLowStock.RowCellStyle += (s, e) =>
            {
                if (e.Column.FieldName == "Stock")
                {
                    var stockVal = e.CellValue?.ToString()?.Replace(".", "").Replace(",", ".") ?? "0";
                    if (int.TryParse(stockVal, out int stock))
                    {
                        if (stock <= 5)
                        {
                            e.Appearance.BackColor = Color.FromArgb(254, 226, 226);
                            e.Appearance.ForeColor = Color.FromArgb(229, 62, 62);
                            e.Appearance.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                        }
                        else if (stock <= 10)
                        {
                            e.Appearance.BackColor = Color.FromArgb(254, 243, 199);
                            e.Appearance.ForeColor = Color.FromArgb(237, 137, 54);
                            e.Appearance.Font = new Font("Segoe UI", 11, FontStyle.Bold);
                        }
                    }
                }
            };

            gridContainer2.Controls.Add(_gridLowStock);
        }

        private PanelControl CreateCardPanel(string title, Color accentColor)
        {
            PanelControl panel = new PanelControl
            {
                Dock = DockStyle.Fill,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
                Margin = new Padding(10)
            };
            panel.Appearance.BackColor = Color.White;

            // Üst renk şeridi
            PanelControl stripe = new PanelControl
            {
                Dock = DockStyle.Top,
                Height = 5,
                BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
            };
            stripe.Appearance.BackColor = accentColor;
            panel.Controls.Add(stripe);

            LabelControl lbl = new LabelControl
            {
                Text = title,
                Dock = DockStyle.Top,
                AutoSizeMode = LabelAutoSizeMode.None,
                Height = 50,
                Padding = new Padding(15, 15, 0, 0)
            };
            lbl.Appearance.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lbl.Appearance.ForeColor = Color.FromArgb(45, 55, 72);
            panel.Controls.Add(lbl);

            return panel;
        }

        private void LoadData()
        {
            try
            {
                // Bugünkü satışlar
                decimal todayTotal = _saleService.GetTodaySalesTotal();
                int todayCount = _saleService.GetTodaySalesCount();
                _lblTodaySales.Text = $"Bugün: {todayTotal:₺#,##0.00}";
                _lblTodayCount.Text = $"({todayCount} satış)";

                // En çok satılanlar
                var topSelling = _saleService.GetTopSellingProducts(10);
                int rank = 1;
                var topSellingData = topSelling.Select(item => new
                {
                    Rank = rank++,
                    Name = item.ProductName,
                    Quantity = item.TotalQuantity.ToString("N0"),
                    Revenue = item.TotalRevenue.ToString("₺#,##0.00")
                }).ToList();

                _gridTopSelling.DataSource = topSellingData;

                // Stoğu azalanlar
                var lowStock = _productService.GetLowStockProducts(10, 20);
                var lowStockData = lowStock.Select(item => new
                {
                    Name = item.Name,
                    Category = item.Category?.Name ?? "-",
                    Stock = item.StockQuantity
                }).ToList();

                _gridLowStock.DataSource = lowStockData;
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Veri yüklenirken hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
