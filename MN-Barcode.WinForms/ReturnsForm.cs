using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MN_Barcode.Business;

namespace MN_Barcode.WinForms
{
    public class ReturnsForm : Form
    {
        private SaleService  _saleService;
        private DataGridView _gridReturns;
        private DataGridView _gridTopReturns;
        private DateTimePicker _dtStart;
        private DateTimePicker _dtEnd;
        private Label        _lblTotalReturnAmount;

        public ReturnsForm()
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
                using (var p = new Pen(Theme.Danger, 3))
                    e.Graphics.DrawLine(p, 0, header.Height - 3, header.Width, header.Height - 3);
                using (var p = new Pen(Theme.Border, 1))
                    e.Graphics.DrawLine(p, 0, 0, header.Width, 0);
            };
            this.Controls.Add(header);

            Label title = new Label
            {
                Text      = "İade İşlemleri",
                Font      = Theme.H1,
                ForeColor = Theme.TextPrimary,
                AutoSize  = true,
                Location  = new Point(Theme.PagePad, 14),
                BackColor = Color.Transparent
            };
            header.Controls.Add(title);

            // Filtre grubu (sağa akış)
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
            _dtStart.Value  = DateTime.Now.AddDays(-30);
            _dtStart.Margin = new Padding(4, 2, 2, 4);
            ctrl.Controls.Add(_dtStart);

            ctrl.Controls.Add(new Label { Text = "→", Font = Theme.Body, ForeColor = Theme.TextSecond, AutoSize = true, Margin = new Padding(0, 6, 0, 0) });

            _dtEnd = Theme.MakeDatePicker(110);
            _dtEnd.Margin = new Padding(2, 2, 4, 4);
            ctrl.Controls.Add(_dtEnd);

            Button btnFilter = Theme.MakeButton("Filtrele", Theme.Danger, Color.White, 90, 32, 9.5f);
            btnFilter.Margin = new Padding(4, 2, 4, 4);
            btnFilter.FlatAppearance.MouseOverBackColor = Color.FromArgb(185, 28, 28);
            btnFilter.Click += (s, e) => LoadData();
            ctrl.Controls.Add(btnFilter);

            // İade toplamı
            _lblTotalReturnAmount = new Label
            {
                Text      = "İade Toplamı: ₺0.00",
                Font      = Theme.H2,
                ForeColor = Theme.Danger,
                AutoSize  = true,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
                Location  = new Point(header.Width - 320, 18),
                BackColor = Color.Transparent
            };
            header.Controls.Add(_lblTotalReturnAmount);
            header.Resize += (s, e) => _lblTotalReturnAmount.Location =
                new Point(header.Width - _lblTotalReturnAmount.Width - 340, 18);

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

            // Sol: İade listesi
            Panel left = Theme.MakeCard("İade Hareketleri", Theme.Danger);
            content.Controls.Add(left, 0, 0);

            _gridReturns = Theme.MakeDarkGrid();
            _gridReturns.Columns.Add("Id",     "ID");          _gridReturns.Columns["Id"].Visible     = false;
            _gridReturns.Columns.Add("Date",   "TARİH");       _gridReturns.Columns["Date"].Width      = 150;
            _gridReturns.Columns.Add("Code",   "FİŞ NO");      _gridReturns.Columns["Code"].Width      = 120;
            _gridReturns.Columns.Add("Amount", "İADE TUTARI");
            _gridReturns.Columns["Amount"].DefaultCellStyle.ForeColor = Theme.Danger;
            _gridReturns.Columns["Amount"].DefaultCellStyle.Font      = new Font("Consolas", 10, FontStyle.Bold);

            Panel gw1 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 8) };
            gw1.Controls.Add(_gridReturns);
            left.Controls.Add(gw1);

            // Sağ: En çok iade edilenler
            Panel right = Theme.MakeCard("En Çok İade Edilenler", Theme.Warning);
            content.Controls.Add(right, 1, 0);

            _gridTopReturns = Theme.MakeDarkGrid();
            _gridTopReturns.Columns.Add("Name", "ÜRÜN ADI");  _gridTopReturns.Columns["Name"].FillWeight = 150;
            _gridTopReturns.Columns.Add("Qty",  "ADET");      _gridTopReturns.Columns["Qty"].FillWeight  = 50;
            _gridTopReturns.Columns.Add("Rev",  "İADE TUTARI"); _gridTopReturns.Columns["Rev"].FillWeight = 80;
            _gridTopReturns.Columns["Rev"].DefaultCellStyle.ForeColor = Theme.Danger;
            _gridTopReturns.Columns["Rev"].DefaultCellStyle.Font      = new Font("Consolas", 10, FontStyle.Bold);

            Panel gw2 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 8) };
            gw2.Controls.Add(_gridTopReturns);
            right.Controls.Add(gw2);
        }

        // ════════════════════════════════════════════════════════════
        //  VERİ YÜKLEME
        // ════════════════════════════════════════════════════════════
        private void LoadData()
        {
            DateTime start = _dtStart.Value.Date;
            DateTime end   = _dtEnd.Value.Date.AddDays(1).AddSeconds(-1);

            _gridReturns.Rows.Clear();
            var returns = _saleService.GetReturnsHistory(start, end);
            foreach (var r in returns)
                _gridReturns.Rows.Add(r.Id, r.CreatedDate?.ToString("dd.MM.yyyy HH:mm"),
                    r.TransactionCode, Math.Abs(r.TotalAmount).ToString("₺#,##0.00"));

            decimal total = returns.Sum(x => Math.Abs(x.TotalAmount));
            _lblTotalReturnAmount.Text = $"İade Toplamı: {total:₺#,##0.00}";

            _gridTopReturns.Rows.Clear();
            foreach (var t in _saleService.GetTopReturnedProducts(10, start, end))
                _gridTopReturns.Rows.Add(t.ProductName, t.TotalQuantity.ToString("N0"),
                    t.TotalRevenue.ToString("₺#,##0.00"));
        }
    }
}
