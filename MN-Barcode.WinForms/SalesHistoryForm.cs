using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MN_Barcode.Business;
using MN_Barcode.Entities;

namespace MN_Barcode.WinForms
{
    public class SalesHistoryForm : Form
    {
        private SaleService  _saleService;
        private DataGridView _gridSales;
        private DataGridView _gridTopSelling;
        private DateTimePicker _dtStart;
        private DateTimePicker _dtEnd;
        private Label        _lblTotalRevenue;

        public SalesHistoryForm()
        {
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
                Text      = "Satış Geçmişi",
                Font      = Theme.H1,
                ForeColor = Theme.TextPrimary,
                AutoSize  = true,
                Location  = new Point(Theme.PagePad, 14),
                BackColor = Color.Transparent
            };
            header.Controls.Add(title);

            // Tarih seçici + filtre (sağa akış)
            FlowLayoutPanel ctrl = new FlowLayoutPanel
            {
                Dock          = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                AutoSize      = true,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0, 14, 8, 0)
            };
            header.Controls.Add(ctrl);

            _dtStart = Theme.MakeDatePicker(110);
            _dtStart.Value  = DateTime.Now.AddDays(-7);
            _dtStart.Margin = new Padding(4, 2, 2, 4);
            ctrl.Controls.Add(_dtStart);

            ctrl.Controls.Add(new Label { Text = "→", Font = Theme.Body, ForeColor = Theme.TextSecond, AutoSize = true, Margin = new Padding(0, 6, 0, 0) });

            _dtEnd = Theme.MakeDatePicker(110);
            _dtEnd.Margin = new Padding(2, 2, 4, 4);
            ctrl.Controls.Add(_dtEnd);

            Button btnFilter = Theme.MakeButton("Filtrele", Theme.Accent, Color.White, 90, 32, 9.5f);
            btnFilter.Margin = new Padding(4, 2, 4, 4);
            btnFilter.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
            btnFilter.Click += (s, e) => LoadData();
            ctrl.Controls.Add(btnFilter);

            // Toplam ciro (sağ uç)
            _lblTotalRevenue = new Label
            {
                Text      = "Toplam: ₺0.00",
                Font      = Theme.H2,
                ForeColor = Theme.Success,
                AutoSize  = true,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
                Location  = new Point(header.Width - 300, 18),
                BackColor = Color.Transparent
            };
            header.Controls.Add(_lblTotalRevenue);
            header.Resize += (s, e) => _lblTotalRevenue.Location = new Point(header.Width - _lblTotalRevenue.Width - 320, 18);

            // ── İÇERİK ─────────────────────────────────────────────
            TableLayoutPanel content = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 1,
                Padding     = new Padding(Theme.PagePad, Theme.Gap, Theme.PagePad, Theme.PagePad)
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            this.Controls.Add(content);
            content.BringToFront();

            // Sol: Satış listesi
            Panel left = Theme.MakeCard("Satış Hareketleri", Theme.Accent);
            content.Controls.Add(left, 0, 0);

            _gridSales = Theme.MakeDarkGrid();
            _gridSales.Columns.Add("Id",     "ID");       _gridSales.Columns["Id"].Visible     = false;
            _gridSales.Columns.Add("Date",   "TARİH");    _gridSales.Columns["Date"].Width      = 150;
            _gridSales.Columns.Add("Code",   "FİŞ NO");   _gridSales.Columns["Code"].Width      = 120;
            _gridSales.Columns.Add("Type",   "ÖDEME");    _gridSales.Columns["Type"].Width      = 100;
            _gridSales.Columns.Add("Amount", "TUTAR");
            _gridSales.Columns["Amount"].DefaultCellStyle.ForeColor = Theme.Success;
            _gridSales.Columns["Amount"].DefaultCellStyle.Font      = new Font("Consolas", 10, FontStyle.Bold);

            Panel gw1 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 8) };
            gw1.Controls.Add(_gridSales);
            left.Controls.Add(gw1);

            // Sağ: Top 10
            Panel right = Theme.MakeCard("Dönemin Yıldızları (Top 10)", Theme.Warning);
            content.Controls.Add(right, 1, 0);

            _gridTopSelling = Theme.MakeDarkGrid();
            _gridTopSelling.Columns.Add("Name", "ÜRÜN ADI");  _gridTopSelling.Columns["Name"].FillWeight = 150;
            _gridTopSelling.Columns.Add("Qty",  "ADET");      _gridTopSelling.Columns["Qty"].FillWeight  = 50;
            _gridTopSelling.Columns.Add("Rev",  "CİRO");      _gridTopSelling.Columns["Rev"].FillWeight  = 80;
            _gridTopSelling.Columns["Rev"].DefaultCellStyle.ForeColor = Theme.Success;
            _gridTopSelling.Columns["Rev"].DefaultCellStyle.Font      = new Font("Consolas", 10, FontStyle.Bold);

            Panel gw2 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 8) };
            gw2.Controls.Add(_gridTopSelling);
            right.Controls.Add(gw2);
        }

        // ════════════════════════════════════════════════════════════
        //  VERİ YÜKLEME
        // ════════════════════════════════════════════════════════════
        private void LoadData()
        {
            DateTime start = _dtStart.Value.Date;
            DateTime end   = _dtEnd.Value.Date.AddDays(1).AddSeconds(-1);

            _gridSales.Rows.Clear();
            var sales = _saleService.GetSalesHistory(start, end);
            foreach (var s in sales)
            {
                string pt = s.PaymentType switch
                {
                    PaymentType.Nakit       => "Nakit",
                    PaymentType.KrediKarti  => "Kredi Kartı",
                    PaymentType.Iade        => "İade",
                    _                       => s.PaymentType.ToString()
                };
                _gridSales.Rows.Add(s.Id, s.CreatedDate?.ToString("dd.MM.yyyy HH:mm"),
                    s.TransactionCode, pt, s.TotalAmount.ToString("₺#,##0.00"));
            }

            decimal total = sales.Sum(x => x.TotalAmount);
            _lblTotalRevenue.Text = $"Toplam: {total:₺#,##0.00}";

            _gridTopSelling.Rows.Clear();
            foreach (var t in _saleService.GetTopSellingProducts(10, start, end))
                _gridTopSelling.Rows.Add(t.ProductName, t.TotalQuantity.ToString("N0"),
                    t.TotalRevenue.ToString("₺#,##0.00"));
        }
    }
}
