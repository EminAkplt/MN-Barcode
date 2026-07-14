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
    /// ANA SAYFA (Dashboard) — Koyu tema, KPI kartları, satış grafiği, kritik stok.
    /// </summary>
    public class HomeDashboardForm : Form
    {
        private readonly ProductService _productService;
        private readonly SaleService    _saleService;

        private DateTimePicker _dtpStart;
        private DateTimePicker _dtpEnd;

        private Label _lblTodayCiro;
        private Label _lblMonthlyCiro;
        private Label _lblTodayCount;

        private Panel              _chartPanel;
        private List<DailySalesData> _chartData = new List<DailySalesData>();
        private DataGridView       _gridLowStock;

        public HomeDashboardForm()
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

            // ── Sayfa başlığı şeridi ────────────────────────────────
            Panel topBar = BuildTopBar();
            this.Controls.Add(topBar);

            // ── İçerik ─────────────────────────────────────────────
            Panel content = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding   = new Padding(Theme.PagePad, Theme.Gap, Theme.PagePad, Theme.PagePad)
            };
            this.Controls.Add(content);
            content.BringToFront();

            // Düzen: üstte KPI satırı (sabit), altta grafik + liste
            TableLayoutPanel grid = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 2,
                BackColor   = Color.Transparent
            };
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 128F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent,  100F));
            content.Controls.Add(grid);

            grid.Controls.Add(BuildKpiRow(), 0, 0);
            grid.Controls.Add(BuildBottomRow(), 0, 1);
        }

        // ── Üst başlık şeridi (sayfa adı + tarih seçici) ───────────
        private Panel BuildTopBar()
        {
            Panel bar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 60,
                BackColor = Theme.Surface,
                Padding   = new Padding(Theme.PagePad, 0, Theme.PagePad, 0)
            };
            bar.Paint += (s, e) =>
            {
                using (var p = new Pen(Theme.Border, 1))
                    e.Graphics.DrawLine(p, 0, bar.Height - 1, bar.Width, bar.Height - 1);
            };

            Label title = new Label
            {
                Text      = "Ana Sayfa",
                Font      = Theme.H1,
                ForeColor = Theme.TextPrimary,
                AutoSize  = true,
                Location  = new Point(Theme.PagePad, 14)
            };
            bar.Controls.Add(title);

            // Sağ: tarih kontrolleri
            FlowLayoutPanel ctrl = new FlowLayoutPanel
            {
                Dock          = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                AutoSize      = true,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0, 13, 0, 0)
            };
            bar.Controls.Add(ctrl);

            _dtpStart = Theme.MakeDatePicker(105);
            _dtpStart.Value        = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _dtpStart.Margin       = new Padding(4, 4, 2, 4);
            _dtpStart.ValueChanged += (s, e) => LoadChartAndStock();
            ctrl.Controls.Add(_dtpStart);

            ctrl.Controls.Add(new Label
            {
                Text      = "–",
                Font      = Theme.Body,
                ForeColor = Theme.TextSecond,
                AutoSize  = true,
                Margin    = new Padding(0, 8, 0, 0)
            });

            _dtpEnd = Theme.MakeDatePicker(105);
            _dtpEnd.Value        = DateTime.Today;
            _dtpEnd.Margin       = new Padding(2, 4, 4, 4);
            _dtpEnd.ValueChanged += (s, e) => LoadChartAndStock();
            ctrl.Controls.Add(_dtpEnd);

            ctrl.Controls.Add(BuildGhostBtn("Bu Hafta", () =>
            {
                var t   = DateTime.Today;
                int d   = (7 + (t.DayOfWeek - DayOfWeek.Monday)) % 7;
                _dtpStart.Value = t.AddDays(-d);
                _dtpEnd.Value   = t;
            }));
            ctrl.Controls.Add(BuildGhostBtn("Bu Ay", () =>
            {
                _dtpStart.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                _dtpEnd.Value   = DateTime.Today;
            }));

            return bar;
        }

        // ── 3 KPI kartı ─────────────────────────────────────────────
        private TableLayoutPanel BuildKpiRow()
        {
            var row = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 3,
                RowCount    = 1,
                BackColor   = Color.Transparent,
                Margin      = new Padding(0, 0, 0, Theme.Gap)
            };
            for (int i = 0; i < 3; i++)
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            row.Controls.Add(BuildKpiCard("BUGÜNKÜ CİRO",  Theme.Success,  "₺",   out _lblTodayCiro),    0, 0);
            row.Controls.Add(BuildKpiCard("AYLIK CİRO",    Theme.Info,     "₺",   out _lblMonthlyCiro),  1, 0);
            row.Controls.Add(BuildKpiCard("BUGÜNKÜ SATIŞ", Theme.Warning,  "adet",out _lblTodayCount),   2, 0);

            return row;
        }

        private Panel BuildKpiCard(string caption, Color accentColor, string unit, out Label valueLabel)
        {
            Panel card = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Theme.Surface,
                Margin    = new Padding(0, 0, Theme.Gap, 0)
            };
            card.Paint += (s, e) =>
            {
                using (var p = new Pen(Theme.Border, 1))
                    e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                // Üst aksan şeridi
                using (var b = new SolidBrush(accentColor))
                    e.Graphics.FillRectangle(b, 0, 0, card.Width, 3);
            };

            Label lblCap = new Label
            {
                Text      = caption,
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Theme.TextSecond,
                AutoSize  = true,
                BackColor = Color.Transparent,
                Location  = new Point(Theme.CardPad, 18)
            };
            card.Controls.Add(lblCap);

            valueLabel = new Label
            {
                Text      = "—",
                Font      = new Font("Consolas", 26, FontStyle.Bold),
                ForeColor = accentColor,
                AutoSize  = true,
                BackColor = Color.Transparent,
                Location  = new Point(Theme.CardPad, 38)
            };
            card.Controls.Add(valueLabel);

            Label lblUnit = new Label
            {
                Text      = unit,
                Font      = Theme.Caption,
                ForeColor = Theme.TextMuted,
                AutoSize  = true,
                BackColor = Color.Transparent,
                Location  = new Point(Theme.CardPad, 96)
            };
            card.Controls.Add(lblUnit);

            return card;
        }

        // ── Alt satır: grafik + kritik stok ─────────────────────────
        private TableLayoutPanel BuildBottomRow()
        {
            var row = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 1,
                BackColor   = Color.Transparent
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));

            // Grafik
            _chartPanel = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Theme.Surface,
                Margin    = new Padding(0, 0, Theme.Gap / 2, 0)
            };
            _chartPanel.Paint += ChartPanel_Paint;
            row.Controls.Add(_chartPanel, 0, 0);

            // Kritik stok
            row.Controls.Add(BuildLowStockCard(), 1, 0);

            return row;
        }

        private Panel BuildLowStockCard()
        {
            Panel card = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Theme.Surface,
                Margin    = new Padding(Theme.Gap / 2, 0, 0, 0)
            };
            card.Paint += (s, e) => Theme.PaintCard(card, e, Theme.Warning);

            Label head = new Label
            {
                Text      = "Kritik Stok",
                Font      = Theme.H3,
                ForeColor = Theme.Warning,
                Dock      = DockStyle.Top,
                Height    = 44,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(Theme.CardPad + 4, 0, 0, 0),
                BackColor = Color.Transparent
            };

            var sep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.Border };

            _gridLowStock = Theme.MakeDarkGrid();
            _gridLowStock.Columns.Add("Name",  "ÜRÜN");     _gridLowStock.Columns["Name"].FillWeight  = 150;
            _gridLowStock.Columns.Add("Stock", "STOK");     _gridLowStock.Columns["Stock"].FillWeight = 50;

            Panel wrap = new Panel
            {
                Dock    = DockStyle.Fill,
                Padding = new Padding(8, 4, 8, 8)
            };
            wrap.Controls.Add(_gridLowStock);

            card.Controls.Add(wrap);
            card.Controls.Add(sep);
            card.Controls.Add(head);
            return card;
        }

        // ════════════════════════════════════════════════════════════
        //  GRAFİK ÇİZİMİ
        // ════════════════════════════════════════════════════════════
        private void ChartPanel_Paint(object sender, PaintEventArgs e)
        {
            Panel panel = (Panel)sender;
            Graphics g  = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Theme.PaintCard(panel, e, Theme.Accent);

            using (var b = new SolidBrush(Theme.TextPrimary))
                g.DrawString("Satış Grafiği", Theme.H2, b, Theme.CardPad + 4, 14);

            int padL = 60, padR = 20, padT = 52, padB = 36;
            int chartW = panel.Width - padL - padR;
            int chartH = panel.Height - padT - padB;
            if (chartW <= 10 || chartH <= 10) return;

            if (_chartData == null || _chartData.Count == 0)
            {
                using (var b = new SolidBrush(Theme.TextMuted))
                {
                    string msg = "Bu tarih aralığında satış yok";
                    SizeF sz   = g.MeasureString(msg, Theme.Body);
                    g.DrawString(msg, Theme.Body, b, panel.Width / 2f - sz.Width / 2, panel.Height / 2f);
                }
                return;
            }

            decimal maxValue = _chartData.Max(x => x.Total);
            if (maxValue == 0) maxValue = 1;

            // Grid çizgileri + Y ekseni
            using (var gp = new Pen(Theme.Border, 1))
            using (var tb = new SolidBrush(Theme.TextMuted))
            {
                for (int i = 0; i <= 4; i++)
                {
                    int y   = padT + chartH * i / 4;
                    g.DrawLine(gp, padL, y, panel.Width - padR, y);
                    decimal val = maxValue - maxValue * i / 4;
                    g.DrawString(val >= 1000 ? $"{val / 1000:N0}K" : $"{val:N0}",
                        Theme.Caption, tb, 4, y - 8);
                }
            }

            int n = _chartData.Count;
            if (n == 1)
            {
                int cx = padL + chartW / 2, cy = padT + chartH / 2;
                using (var b = new SolidBrush(Theme.Accent)) g.FillEllipse(b, cx - 5, cy - 5, 10, 10);
                using (var b = new SolidBrush(Theme.TextPrimary))
                    g.DrawString($"{_chartData[0].Date:dd/MM}: {_chartData[0].Total:N0} ₺", Theme.Body, b, cx - 55, cy - 24);
                return;
            }

            Point[] pts = new Point[n];
            for (int i = 0; i < n; i++)
            {
                int x = padL + chartW * i / (n - 1);
                int y = padT + chartH - (int)((double)_chartData[i].Total / (double)maxValue * chartH);
                pts[i] = new Point(x, y);
            }

            // Dolgu
            Point[] fill = new Point[n + 2];
            Array.Copy(pts, fill, n);
            fill[n]     = new Point(pts[n - 1].X, padT + chartH);
            fill[n + 1] = new Point(pts[0].X, padT + chartH);
            using (var br = new LinearGradientBrush(
                new Rectangle(padL, padT, chartW, chartH),
                Color.FromArgb(45, Theme.Accent), Color.FromArgb(4, Theme.Accent),
                LinearGradientMode.Vertical))
                g.FillPolygon(br, fill);

            // Çizgi
            using (var lp = new Pen(Theme.Accent, 2) { LineJoin = LineJoin.Round })
                g.DrawLines(lp, pts);

            // X ekseni
            using (var tb = new SolidBrush(Theme.TextMuted))
            {
                int step = Math.Max(1, n / 7);
                for (int i = 0; i < n; i += step)
                {
                    string d = _chartData[i].Date.ToString("dd/MM");
                    SizeF sz = g.MeasureString(d, Theme.Caption);
                    g.DrawString(d, Theme.Caption, tb, pts[i].X - sz.Width / 2, padT + chartH + 8);
                }
            }
        }

        // ════════════════════════════════════════════════════════════
        //  YARDIMCI
        // ════════════════════════════════════════════════════════════
        private Button BuildGhostBtn(string text, Action onClick)
        {
            var btn = new Button
            {
                Text      = text,
                Width     = 72,
                Height    = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.SurfaceAlt,
                ForeColor = Theme.TextSecond,
                Font      = Theme.Caption,
                Cursor    = Cursors.Hand,
                Margin    = new Padding(4, 2, 0, 4)
            };
            btn.FlatAppearance.BorderColor         = Theme.Border;
            btn.FlatAppearance.BorderSize          = 1;
            btn.FlatAppearance.MouseOverBackColor  = Theme.Surface;
            btn.Click += (s, e) => onClick();
            return btn;
        }

        // ════════════════════════════════════════════════════════════
        //  VERİ YÜKLEME
        // ════════════════════════════════════════════════════════════
        private void LoadData()
        {
            try
            {
                _lblTodayCiro.Text    = _saleService.GetTodaySalesTotal().ToString("₺#,##0");
                _lblMonthlyCiro.Text  = _saleService.GetMonthlySalesTotal().ToString("₺#,##0");
                _lblTodayCount.Text   = _saleService.GetTodaySalesCount().ToString();
                LoadChartAndStock();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Dashboard yüklenirken hata: " + ex.Message);
            }
        }

        private void LoadChartAndStock()
        {
            try
            {
                _chartData = _saleService.GetDailySales(_dtpStart.Value, _dtpEnd.Value);
                _chartPanel?.Invalidate();

                if (_gridLowStock == null) return;
                _gridLowStock.Rows.Clear();
                foreach (var item in _productService.GetLowStockProducts(12, 20))
                {
                    int idx = _gridLowStock.Rows.Add(item.Name, item.StockQuantity.ToString("N0"));
                    var cell = _gridLowStock.Rows[idx].Cells[1];
                    cell.Style.ForeColor = item.StockQuantity <= 5 ? Theme.Danger : Theme.Warning;
                    cell.Style.Font      = new Font(Theme.FontName, 10, FontStyle.Bold);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Grafik/stok hatası: " + ex.Message);
            }
        }
    }
}
