using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Forms;
using MN_Barcode.Business;
using MN_Barcode.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace MN_Barcode.WinForms
{
    public class HomeDashboardForm : Form
    {
        private ProductService _productService;
        private SaleService _saleService;

        // Header components
        private DateTimePicker _dtpStart;
        private DateTimePicker _dtpEnd;

        // Ciro kartları
        private Label _lblTodayCiro;
        private Label _lblMonthlyCiro;

        // Data grids
        private DataGridView _gridLowStock;
        
        // Grafik paneli
        private Panel _chartPanel;
        private List<DailySalesData> _chartData;
        
        // Döviz kurları
        private Panel _currencyPanel;
        private Label _lblUSD;
        private Label _lblEUR;
        private Label _lblGBP;

        // Renk Paleti - Modern Light Theme
        private readonly Color BgColor = Color.FromArgb(248, 250, 252);
        private readonly Color CardBg = Color.White;
        private readonly Color HeaderBg = Color.FromArgb(30, 41, 59);
        private readonly Color PrimaryBlue = Color.FromArgb(59, 130, 246);
        private readonly Color SuccessGreen = Color.FromArgb(16, 185, 129);
        private readonly Color WarningOrange = Color.FromArgb(245, 158, 11);
        private readonly Color PurpleAccent = Color.FromArgb(139, 92, 246);
        private readonly Color TextDark = Color.FromArgb(30, 41, 59);
        private readonly Color TextMuted = Color.FromArgb(100, 116, 139);
        private readonly Color BorderColor = Color.FromArgb(226, 232, 240);

        public HomeDashboardForm()
        {
            _productService = new ProductService();
            _saleService = new SaleService();
            _chartData = new List<DailySalesData>();
            this.DoubleBuffered = true;
            InitUI();
            this.Shown += async (s, e) => await LoadDataAsync();
        }

        private void InitUI()
        {
            this.BackColor = BgColor;
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;

            // ═══════════════════════════════════════════════════════════
            // HEADER
            // ═══════════════════════════════════════════════════════════
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = HeaderBg,
                Padding = new Padding(20, 0, 20, 0)
            };
            this.Controls.Add(header);

            // Header için TableLayoutPanel kullan (responsive)
            TableLayoutPanel headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Başlık
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Ortada boşluk
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Kontroller
            header.Controls.Add(headerLayout);

            // Başlık
            Label title = new Label
            {
                Text = "📊 ANA SAYFA",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 20, 0, 0)
            };
            headerLayout.Controls.Add(title, 0, 0);

            // Sağ taraf kontroller
            FlowLayoutPanel controls = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 15, 0, 0)
            };
            headerLayout.Controls.Add(controls, 2, 0);

            _dtpStart = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Size = new Size(110, 28),
                Font = new Font("Segoe UI", 10),
                Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                Margin = new Padding(5)
            };
            _dtpStart.ValueChanged += async (s, e) => await LoadDataAsync();
            controls.Controls.Add(_dtpStart);

            Label lblTo = new Label
            {
                Text = "—",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Margin = new Padding(5, 8, 5, 0)
            };
            controls.Controls.Add(lblTo);

            _dtpEnd = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Size = new Size(110, 28),
                Font = new Font("Segoe UI", 10),
                Value = DateTime.Today,
                Margin = new Padding(5)
            };
            _dtpEnd.ValueChanged += async (s, e) => await LoadDataAsync();
            controls.Controls.Add(_dtpEnd);

            // Hızlı tarih butonları
            controls.Controls.Add(CreateQuickDateButton("Bu Hafta", () =>
            {
                var today = DateTime.Today;
                var diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                _dtpStart.Value = today.AddDays(-diff);
                _dtpEnd.Value = today;
            }));

            controls.Controls.Add(CreateQuickDateButton("Bu Ay", () =>
            {
                _dtpStart.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                _dtpEnd.Value = DateTime.Today;
            }));

            // Yenile butonu
            Button btnRefresh = new Button
            {
                Text = "🔄",
                Size = new Size(40, 36),
                BackColor = PrimaryBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12),
                Cursor = Cursors.Hand,
                Margin = new Padding(10, 5, 0, 5)
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += async (s, e) => await LoadDataAsync();
            controls.Controls.Add(btnRefresh);

            // ═══════════════════════════════════════════════════════════
            // ANA İÇERİK PANELİ
            // ═══════════════════════════════════════════════════════════
            Panel mainContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = BgColor
            };
            this.Controls.Add(mainContent);
            mainContent.BringToFront();

            // Ana tablo düzeni - 3 satır
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = Color.Transparent
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F)); // Grafik
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F)); // Ciro kartları
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55F)); // Alt widgetlar
            mainContent.Controls.Add(mainLayout);

            // ═══════════════════════════════════════════════════════════
            // SATIŞ GRAFİĞİ (XY Line Chart)
            // ═══════════════════════════════════════════════════════════
            _chartPanel = CreateCardPanel();
            _chartPanel.Dock = DockStyle.Fill;
            _chartPanel.Margin = new Padding(5);
            _chartPanel.Paint += ChartPanel_Paint;
            mainLayout.Controls.Add(_chartPanel, 0, 0);

            // ═══════════════════════════════════════════════════════════
            // CİRO KARTLARI
            // ═══════════════════════════════════════════════════════════
            TableLayoutPanel ciroRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            ciroRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            ciroRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mainLayout.Controls.Add(ciroRow, 0, 1);

            // Bugünkü Ciro Kartı
            Panel todayCard = CreateCiroCard("💰 BUGÜNKÜ CİRO", SuccessGreen, out _lblTodayCiro);
            ciroRow.Controls.Add(todayCard, 0, 0);

            // Aylık Ciro Kartı
            Panel monthlyCard = CreateCiroCard("📅 AYLIK CİRO", PrimaryBlue, out _lblMonthlyCiro);
            ciroRow.Controls.Add(monthlyCard, 1, 0);

            // ═══════════════════════════════════════════════════════════
            // ALT WİDGETLAR
            // ═══════════════════════════════════════════════════════════
            TableLayoutPanel bottomRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            mainLayout.Controls.Add(bottomRow, 0, 2);

            // Tükenmekte olan ürünler
            Panel lowStockPanel = CreateCardPanel();
            lowStockPanel.Dock = DockStyle.Fill;
            lowStockPanel.Margin = new Padding(5);
            bottomRow.Controls.Add(lowStockPanel, 0, 0);

            Label lblLowStock = new Label
            {
                Text = "⚠️ TÜKENMEKTE OLAN ÜRÜNLER",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = WarningOrange,
                Dock = DockStyle.Top,
                Height = 45,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0)
            };
            lowStockPanel.Controls.Add(lblLowStock);

            _gridLowStock = CreateModernGrid();
            _gridLowStock.Columns.Add("Name", "ÜRÜN ADI");
            _gridLowStock.Columns["Name"].FillWeight = 150;
            _gridLowStock.Columns.Add("Category", "KATEGORİ");
            _gridLowStock.Columns["Category"].FillWeight = 80;
            _gridLowStock.Columns.Add("Stock", "STOK");
            _gridLowStock.Columns["Stock"].FillWeight = 50;

            Panel gridContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 0, 10, 10) };
            gridContainer.Controls.Add(_gridLowStock);
            lowStockPanel.Controls.Add(gridContainer);
            gridContainer.BringToFront();

            // Döviz Kurları
            _currencyPanel = CreateCardPanel();
            _currencyPanel.Dock = DockStyle.Fill;
            _currencyPanel.Margin = new Padding(5);
            bottomRow.Controls.Add(_currencyPanel, 1, 0);

            Label lblCurrency = new Label
            {
                Text = "💱 DÖVİZ KURLARI",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = PurpleAccent,
                Dock = DockStyle.Top,
                Height = 45,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0)
            };
            _currencyPanel.Controls.Add(lblCurrency);

            Panel currencyContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 10, 20, 20)
            };
            _currencyPanel.Controls.Add(currencyContent);
            currencyContent.BringToFront();

            _lblUSD = CreateCurrencyLabel("🇺🇸 USD/TRY", "Yükleniyor...", 0);
            currencyContent.Controls.Add(_lblUSD);

            _lblEUR = CreateCurrencyLabel("🇪🇺 EUR/TRY", "Yükleniyor...", 55);
            currencyContent.Controls.Add(_lblEUR);

            _lblGBP = CreateCurrencyLabel("🇬🇧 GBP/TRY", "Yükleniyor...", 110);
            currencyContent.Controls.Add(_lblGBP);
        }

        private Button CreateQuickDateButton(string text, Action onClick)
        {
            Button btn = new Button
            {
                Text = text,
                Size = new Size(75, 36),
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand,
                Margin = new Padding(5)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => onClick();
            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(71, 85, 105);
            btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(51, 65, 85);
            return btn;
        }

        private Panel CreateCardPanel()
        {
            Panel panel = new Panel
            {
                BackColor = CardBg
            };
            panel.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(BorderColor, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                }
            };
            return panel;
        }

        private Panel CreateCiroCard(string title, Color accentColor, out Label valueLabel)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                BackColor = CardBg
            };

            card.Paint += (s, e) =>
            {
                using (SolidBrush brush = new SolidBrush(accentColor))
                {
                    e.Graphics.FillRectangle(brush, 0, 0, 4, card.Height);
                }
                using (Pen pen = new Pen(BorderColor, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                }
            };

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11),
                ForeColor = TextMuted,
                Location = new Point(20, 15),
                AutoSize = true
            };
            card.Controls.Add(lblTitle);

            valueLabel = new Label
            {
                Text = "₺0",
                Font = new Font("Segoe UI", 26, FontStyle.Bold),
                ForeColor = TextDark,
                Location = new Point(20, 42),
                AutoSize = true
            };
            card.Controls.Add(valueLabel);

            return card;
        }

        private Label CreateCurrencyLabel(string currency, string value, int top)
        {
            Label lbl = new Label
            {
                Font = new Font("Segoe UI", 13),
                ForeColor = TextDark,
                Location = new Point(0, top),
                Size = new Size(280, 45),
                TextAlign = ContentAlignment.MiddleLeft
            };
            lbl.Text = $"{currency}: {value}";
            return lbl;
        }

        private DataGridView CreateModernGrid()
        {
            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = CardBg,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                GridColor = BorderColor
            };
            grid.RowTemplate.Height = 40;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            grid.DefaultCellStyle.ForeColor = TextDark;
            grid.DefaultCellStyle.BackColor = CardBg;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(241, 245, 249);
            grid.DefaultCellStyle.SelectionForeColor = TextDark;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

            grid.ColumnHeadersHeight = 42;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextMuted;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.EnableHeadersVisualStyles = false;

            return grid;
        }

        private void ChartPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Panel panel = sender as Panel;
            int paddingLeft = 80;
            int paddingRight = 30;
            int paddingTop = 50;
            int paddingBottom = 50;
            int chartHeight = panel.Height - paddingTop - paddingBottom;
            int chartWidth = panel.Width - paddingLeft - paddingRight;

            // Başlık
            using (Font font = new Font("Segoe UI", 13, FontStyle.Bold))
            using (SolidBrush brush = new SolidBrush(TextDark))
            {
                g.DrawString("📈 SATIŞ GRAFİĞİ", font, brush, paddingLeft, 15);
            }

            if (_chartData == null || _chartData.Count == 0)
            {
                using (Font font = new Font("Segoe UI", 11))
                using (SolidBrush brush = new SolidBrush(TextMuted))
                {
                    g.DrawString("Bu tarih aralığında satış bulunamadı", font, brush, 
                        panel.Width / 2 - 110, panel.Height / 2);
                }
                return;
            }

            decimal maxValue = _chartData.Max(x => x.Total);
            if (maxValue == 0) maxValue = 1;

            // Y ekseni grid çizgileri ve değerler
            using (Pen gridPen = new Pen(BorderColor, 1))
            using (Font font = new Font("Segoe UI", 9))
            using (SolidBrush brush = new SolidBrush(TextMuted))
            {
                for (int i = 0; i <= 5; i++)
                {
                    int y = paddingTop + (chartHeight * i / 5);
                    g.DrawLine(gridPen, paddingLeft, y, panel.Width - paddingRight, y);
                    
                    decimal val = maxValue - (maxValue * i / 5);
                    string valStr = val >= 1000 ? $"{val / 1000:N0}K" : $"{val:N0}";
                    g.DrawString(valStr, font, brush, 10, y - 8);
                }
            }

            // X çizgisi
            using (Pen pen = new Pen(TextMuted, 1))
            {
                g.DrawLine(pen, paddingLeft, paddingTop + chartHeight, panel.Width - paddingRight, paddingTop + chartHeight);
            }

            // Çizgi grafiği çiz
            int pointCount = _chartData.Count;
            if (pointCount > 1)
            {
                Point[] points = new Point[pointCount];
                
                for (int i = 0; i < pointCount; i++)
                {
                    var data = _chartData[i];
                    int x = paddingLeft + (chartWidth * i / (pointCount - 1));
                    int y = paddingTop + chartHeight - (int)((double)data.Total / (double)maxValue * chartHeight);
                    points[i] = new Point(x, y);
                }

                // Gradient dolgu alanı
                if (points.Length > 1)
                {
                    Point[] fillPoints = new Point[points.Length + 2];
                    Array.Copy(points, fillPoints, points.Length);
                    fillPoints[points.Length] = new Point(points[points.Length - 1].X, paddingTop + chartHeight);
                    fillPoints[points.Length + 1] = new Point(points[0].X, paddingTop + chartHeight);

                    using (LinearGradientBrush brush = new LinearGradientBrush(
                        new Rectangle(paddingLeft, paddingTop, chartWidth, chartHeight),
                        Color.FromArgb(60, PrimaryBlue),
                        Color.FromArgb(10, PrimaryBlue),
                        LinearGradientMode.Vertical))
                    {
                        g.FillPolygon(brush, fillPoints);
                    }
                }

                // Çizgi
                using (Pen linePen = new Pen(PrimaryBlue, 3))
                {
                    linePen.LineJoin = LineJoin.Round;
                    g.DrawLines(linePen, points);
                }

                // Noktalar
                for (int i = 0; i < points.Length; i++)
                {
                    g.FillEllipse(Brushes.White, points[i].X - 5, points[i].Y - 5, 10, 10);
                    using (Pen dotPen = new Pen(PrimaryBlue, 2))
                    {
                        g.DrawEllipse(dotPen, points[i].X - 5, points[i].Y - 5, 10, 10);
                    }
                }

                // X ekseni tarihleri
                using (Font font = new Font("Segoe UI", 8))
                using (SolidBrush brush = new SolidBrush(TextMuted))
                {
                    int step = Math.Max(1, pointCount / 8);
                    for (int i = 0; i < pointCount; i += step)
                    {
                        string dateStr = _chartData[i].Date.ToString("dd/MM");
                        SizeF size = g.MeasureString(dateStr, font);
                        g.DrawString(dateStr, font, brush, points[i].X - size.Width / 2, paddingTop + chartHeight + 8);
                    }
                }
            }
            else if (pointCount == 1)
            {
                var data = _chartData[0];
                int x = paddingLeft + chartWidth / 2;
                int y = paddingTop + chartHeight / 2;
                
                g.FillEllipse(new SolidBrush(PrimaryBlue), x - 8, y - 8, 16, 16);
                
                using (Font font = new Font("Segoe UI", 10, FontStyle.Bold))
                using (SolidBrush brush = new SolidBrush(TextDark))
                {
                    string text = $"{data.Date:dd/MM}: {data.Total:₺#,##0}";
                    SizeF size = g.MeasureString(text, font);
                    g.DrawString(text, font, brush, x - size.Width / 2, y - 30);
                }
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                decimal todayTotal = _saleService.GetTodaySalesTotal();
                decimal monthlyTotal = _saleService.GetMonthlySalesTotal();

                _lblTodayCiro.Text = todayTotal.ToString("₺#,##0.00");
                _lblMonthlyCiro.Text = monthlyTotal.ToString("₺#,##0.00");

                _chartData = _saleService.GetDailySales(_dtpStart.Value, _dtpEnd.Value);
                _chartPanel.Invalidate();

                _gridLowStock.Rows.Clear();
                var lowStock = _productService.GetLowStockProducts(10, 20);
                foreach (var item in lowStock)
                {
                    int idx = _gridLowStock.Rows.Add(item.Name, item.Category?.Name ?? "-", item.StockQuantity.ToString("N0"));

                    if (item.StockQuantity <= 5)
                    {
                        _gridLowStock.Rows[idx].Cells[2].Style.ForeColor = Color.FromArgb(220, 38, 38);
                        _gridLowStock.Rows[idx].Cells[2].Style.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                    }
                    else
                    {
                        _gridLowStock.Rows[idx].Cells[2].Style.ForeColor = WarningOrange;
                    }
                }

                await LoadCurrencyRatesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dashboard yüklenirken hata: {ex.Message}");
            }
        }

        private async Task LoadCurrencyRatesAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    string response = await client.GetStringAsync("https://open.er-api.com/v6/latest/TRY");
                    
                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        var rates = doc.RootElement.GetProperty("rates");
                        
                        double usdRate = 1 / rates.GetProperty("USD").GetDouble();
                        double eurRate = 1 / rates.GetProperty("EUR").GetDouble();
                        double gbpRate = 1 / rates.GetProperty("GBP").GetDouble();

                        _lblUSD.Text = $"🇺🇸 USD/TRY: {usdRate:N2} ₺";
                        _lblEUR.Text = $"🇪🇺 EUR/TRY: {eurRate:N2} ₺";
                        _lblGBP.Text = $"🇬🇧 GBP/TRY: {gbpRate:N2} ₺";
                    }
                }
            }
            catch (Exception)
            {
                _lblUSD.Text = "🇺🇸 USD/TRY: Bağlantı hatası";
                _lblEUR.Text = "🇪🇺 EUR/TRY: Bağlantı hatası";
                _lblGBP.Text = "🇬🇧 GBP/TRY: Bağlantı hatası";
            }
        }
    }
}
