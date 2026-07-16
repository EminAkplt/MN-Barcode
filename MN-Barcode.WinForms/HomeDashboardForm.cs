using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MN_Barcode.Business;
using MN_Barcode.Entities;

namespace MN_Barcode.WinForms
{
    /// <summary>
    /// ANA SAYFA (Dashboard).
    ///
    /// Tasarım yaklaşımı: SADE ve abartısız. Üç tane net özet kartı (KPI),
    /// tek bir satış grafiği ve kritik stok listesi. Emoji kalabalığı ve
    /// döviz kuru widget'ı (her açılışta internete gidip ekranı bekletiyordu)
    /// bilinçli olarak kaldırıldı. Tüm renk/font/boşluk değerleri Theme'den gelir.
    /// </summary>
    public class HomeDashboardForm : Form
    {
        private readonly ProductService _productService;
        private readonly SaleService _saleService;

        // Tarih aralığı (grafik için)
        private DateTimePicker _dtpStart;
        private DateTimePicker _dtpEnd;

        // KPI rakamları
        private Label _lblTodayCiro;
        private Label _lblMonthlyCiro;
        private Label _lblTodayCount;

        // Grafik
        private Panel _chartPanel;
        private List<DailySalesData> _chartData = new List<DailySalesData>();

        public HomeDashboardForm()
        {
            _productService = new ProductService();
            _saleService = new SaleService();
            this.DoubleBuffered = true;
            InitUI();
            // Veriyi ekran gösterildikten sonra yükle (arayüz takılmasın).
            this.Shown += (s, e) => LoadData();
        }

        // ============================================================
        //  ARAYÜZ KURULUMU
        // ============================================================
        private void InitUI()
        {
            this.BackColor = Theme.Background;
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;

            // İçerik (en alta eklenir ki header'ın altında kalsın → sonra BringToFront)
            Panel content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(Theme.PagePad)
            };
            this.Controls.Add(content);

            // Sayfa başlığı şeridi (beyaz, ince alt çizgili — sade sayfa başlığı)
            this.Controls.Add(BuildHeader());
            content.BringToFront();

            // İçerik düzeni: üstte KPI satırı (sabit yükseklik), altta grafik + liste (kalan alan)
            TableLayoutPanel grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F)); // KPI kartları
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Grafik + liste
            content.Controls.Add(grid);

            grid.Controls.Add(BuildKpiRow(), 0, 0);
            grid.Controls.Add(BuildBottomRow(), 0, 1);
        }

        // --- Sayfa başlığı + tarih aralığı kontrolleri ---
        private Panel BuildHeader()
        {
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Theme.Surface,
                Padding = new Padding(Theme.PagePad, 0, Theme.PagePad, 0)
            };
            // Alt ayraç çizgisi
            header.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.Border, 1))
                    e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };

            Label title = new Label
            {
                Text = "Ana Sayfa",
                Font = Theme.H1,
                ForeColor = Theme.TextStrong,
                AutoSize = true,
                Location = new Point(Theme.PagePad, 12)
            };
            header.Controls.Add(title);

            // Sağ taraftaki filtre paneli (daha yapılandırılmış)
            Panel filterPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 420,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 8)
            };
            header.Controls.Add(filterPanel);

            // Filtre başlığı
            Label filterLabel = new Label
            {
                Text = "Tarih Aralığı:",
                Font = Theme.Caption,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            filterPanel.Controls.Add(filterLabel);

            // Tarih seçim konteyneri
            FlowLayoutPanel dateControls = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Height = 44,
                Padding = new Padding(0, 4, 0, 0)
            };
            filterPanel.Controls.Add(dateControls);

            _dtpStart = BuildStyledDateTimePicker(
                new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                () => LoadChartAndStock());
            dateControls.Controls.Add(_dtpStart);

            Label separator = new Label
            {
                Text = "–",
                Font = Theme.Body,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Margin = new Padding(6, 10, 6, 0),
                Width = 12
            };
            dateControls.Controls.Add(separator);

            _dtpEnd = BuildStyledDateTimePicker(DateTime.Today, () => LoadChartAndStock());
            dateControls.Controls.Add(_dtpEnd);

            // Hızlı seçim butonları
            dateControls.Controls.Add(BuildDateShortcutButton("Bu Hafta", () =>
            {
                var today = DateTime.Today;
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                _dtpStart.Value = today.AddDays(-diff);
                _dtpEnd.Value = today;
            }));
            dateControls.Controls.Add(BuildDateShortcutButton("Bu Ay", () =>
            {
                _dtpStart.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                _dtpEnd.Value = DateTime.Today;
            }));

            return header;
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
                ForeColor = Theme.TextDark,
                CalendarMonthBackground = Theme.Background,
                CalendarForeColor = Theme.TextDark,
                CalendarTitleBackColor = Theme.Primary,
                CalendarTitleForeColor = Color.White,
                CalendarTrailingForeColor = Theme.TextMuted
            };
            dtp.ValueChanged += (s, e) => onChanged();
            return dtp;
        }

        private Button BuildDateShortcutButton(string text, Action onClick)
        {
            Button btn = new Button
            {
                Text = text,
                AutoSize = false,
                Width = 70,
                Height = 34,
                BackColor = Theme.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(Theme.FontName, 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(6, 0, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Theme.PrimaryDark;
            btn.Click += (s, e) => onClick();
            return btn;
        }

        // --- 3 adet özet (KPI) kartı ---
        private TableLayoutPanel BuildKpiRow()
        {
            TableLayoutPanel row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            for (int i = 0; i < 3; i++)
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 3));

            row.Controls.Add(BuildStatCard("BUGÜNKÜ CİRO", Theme.Money, out _lblTodayCiro), 0, 0);
            row.Controls.Add(BuildStatCard("AYLIK CİRO", Theme.MoneyBlue, out _lblMonthlyCiro), 1, 0);
            row.Controls.Add(BuildStatCard("BUGÜNKÜ SATIŞ", Color.White, out _lblTodayCount), 2, 0);

            return row;
        }

        // --- Alt satır: tam genişlikli satış grafiği ---
        private TableLayoutPanel BuildBottomRow()
        {
            TableLayoutPanel row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, Theme.Gap, 0, 0)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Grafik kartı (tam genişlik)
            _chartPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                MinimumSize = new Size(0, 300)
            };
            _chartPanel.Paint += ChartPanel_Paint;
            row.Controls.Add(_chartPanel, 0, 0);

            return row;
        }

        // ============================================================
        //  KÜÇÜK BİLEŞENLER
        // ============================================================

        // KPI kartı — "Hızlı Satış" ekranındaki TOPLAM paneli tarzında:
        // koyu zemin, üstte soluk etiket, altında büyük canlı Consolas rakam.
        private Panel BuildStatCard(string caption, Color numberColor, out Label valueLabel)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.PanelDark, // Koyu panel (POS tarzı)
                Margin = new Padding(Theme.Gap / 2, 0, Theme.Gap / 2, 0)
            };

            Label lblCaption = new Label
            {
                Text = caption,
                Font = Theme.Caption,
                ForeColor = Theme.CaptionOnDark, // Koyu üstünde gümüş etiket
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(Theme.CardPad, 18)
            };
            card.Controls.Add(lblCaption);

            valueLabel = new Label
            {
                Text = "—",
                Font = Theme.Number,             // Consolas büyük rakam
                ForeColor = numberColor,         // Canlı renk (yeşil/mavi/beyaz)
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(Theme.CardPad - 2, 44)
            };
            card.Controls.Add(valueLabel);

            return card;
        }

        // ============================================================
        //  SATIŞ GRAFİĞİ (sade çizgi grafik)
        // ============================================================
        private void ChartPanel_Paint(object sender, PaintEventArgs e)
        {
            Panel panel = (Panel)sender;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Kart kenarlığı + sol mavi vurgu
            Theme.PaintCard(panel, e, Theme.Primary);

            // Başlık
            using (var brush = new SolidBrush(Theme.TextDark))
                g.DrawString("Satış Grafiği", Theme.H2, brush, Theme.CardPad, 16);

            int padL = 64, padR = 24, padT = 56, padB = 44;
            int chartW = panel.Width - padL - padR;
            int chartH = panel.Height - padT - padB;
            if (chartW <= 10 || chartH <= 10) return;

            // Veri yoksa nazik bir mesaj göster
            if (_chartData == null || _chartData.Count == 0)
            {
                using (var brush = new SolidBrush(Theme.TextMuted))
                {
                    string msg = "Bu tarih aralığında satış yok";
                    SizeF sz = g.MeasureString(msg, Theme.Body);
                    g.DrawString(msg, Theme.Body, brush, panel.Width / 2 - sz.Width / 2, panel.Height / 2);
                }
                return;
            }

            decimal maxValue = _chartData.Max(x => x.Total);
            if (maxValue == 0) maxValue = 1;

            // Yatay grid çizgileri + Y ekseni değerleri (soluk)
            using (var gridPen = new Pen(Theme.Border, 1))
            using (var brush = new SolidBrush(Theme.TextMuted))
            {
                for (int i = 0; i <= 4; i++)
                {
                    int y = padT + (chartH * i / 4);
                    g.DrawLine(gridPen, padL, y, panel.Width - padR, y);

                    decimal val = maxValue - (maxValue * i / 4);
                    string valStr = val >= 1000 ? $"{val / 1000:N0}K" : $"{val:N0}";
                    g.DrawString(valStr, Theme.Caption, brush, 8, y - 8);
                }
            }

            int n = _chartData.Count;
            if (n == 1)
            {
                // Tek nokta: ortada bir işaret + değer
                int x = padL + chartW / 2;
                int y = padT + chartH / 2;
                using (var b = new SolidBrush(Theme.Primary))
                    g.FillEllipse(b, x - 5, y - 5, 10, 10);
                using (var b = new SolidBrush(Theme.TextDark))
                    g.DrawString($"{_chartData[0].Date:dd/MM}: {_chartData[0].Total:N0} ₺", Theme.Body, b, x - 60, y - 28);
                return;
            }

            // Noktaları hesapla
            Point[] points = new Point[n];
            for (int i = 0; i < n; i++)
            {
                int x = padL + (chartW * i / (n - 1));
                int y = padT + chartH - (int)((double)_chartData[i].Total / (double)maxValue * chartH);
                points[i] = new Point(x, y);
            }

            // Çizgi altına çok hafif dolgu (abartısız)
            Point[] fill = new Point[n + 2];
            Array.Copy(points, fill, n);
            fill[n] = new Point(points[n - 1].X, padT + chartH);
            fill[n + 1] = new Point(points[0].X, padT + chartH);
            using (var brush = new LinearGradientBrush(
                new Rectangle(padL, padT, chartW, chartH),
                Color.FromArgb(28, Theme.Primary), Color.FromArgb(4, Theme.Primary),
                LinearGradientMode.Vertical))
            {
                g.FillPolygon(brush, fill);
            }

            // Çizgi (ince)
            using (var linePen = new Pen(Theme.Primary, 2) { LineJoin = LineJoin.Round })
                g.DrawLines(linePen, points);

            // X ekseni tarihleri (seyrek)
            using (var brush = new SolidBrush(Theme.TextMuted))
            {
                int step = Math.Max(1, n / 7);
                for (int i = 0; i < n; i += step)
                {
                    string d = _chartData[i].Date.ToString("dd/MM");
                    SizeF sz = g.MeasureString(d, Theme.Caption);
                    g.DrawString(d, Theme.Caption, brush, points[i].X - sz.Width / 2, padT + chartH + 8);
                }
            }
        }

        // ============================================================
        //  VERİ YÜKLEME
        // ============================================================
        private void LoadData()
        {
            try
            {
                _lblTodayCiro.Text = _saleService.GetTodaySalesTotal().ToString("₺#,##0");
                _lblMonthlyCiro.Text = _saleService.GetMonthlySalesTotal().ToString("₺#,##0");
                _lblTodayCount.Text = _saleService.GetTodaySalesCount().ToString();

                LoadChartAndStock();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Dashboard yüklenirken hata: " + ex.Message);
            }
        }

        // Tarih aralığına bağlı grafik — tarih değişince yeniden yüklenir.
        private void LoadChartAndStock()
        {
            try
            {
                _chartData = _saleService.GetDailySales(_dtpStart.Value, _dtpEnd.Value);
                _chartPanel?.Invalidate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Grafik yüklenirken hata: " + ex.Message);
            }
        }
    }
}
