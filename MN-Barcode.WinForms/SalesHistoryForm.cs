using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MN_Barcode.Business;
using MN_Barcode.Entities;

namespace MN_Barcode.WinForms
{
    /// <summary>
    /// SATIŞ GEÇMİŞİ — sade, klasik tasarım.
    /// Üstte başlık + toplam, altında tarih filtresi + arama, en altta liste.
    /// Her ürün ayrı satır; aynı fişe ait ürünler aynı renkle gruplanır.
    /// </summary>
    public class SalesHistoryForm : Form
    {
        private const string SearchHint = "Barkod veya ürün adı ara...";

        // Klasik/açık palet
        private static readonly Color ColBg      = Color.FromArgb(245, 247, 250);
        private static readonly Color ColSurface = Color.White;
        private static readonly Color ColBorder  = Color.FromArgb(214, 221, 230);
        private static readonly Color ColText     = Color.FromArgb(30, 41, 59);
        private static readonly Color ColStrong  = Color.FromArgb(15, 23, 42);
        private static readonly Color ColMuted   = Color.FromArgb(100, 116, 139);
        private static readonly Color ColGreen   = Color.FromArgb(22, 163, 74);
        private static readonly Color ColHeaderBg = Color.FromArgb(241, 245, 249);

        private static readonly Color[] RowColors =
        {
            Color.FromArgb(240, 248, 255),  // açık mavi
            Color.FromArgb(240, 255, 244),  // açık yeşil
            Color.FromArgb(255, 251, 235)   // açık krem
        };

        private readonly SaleService _saleService;
        private DataGridView _grid;
        private DateTimePicker _dtStart;
        private DateTimePicker _dtEnd;
        private Label _lblTotal;
        private TextBox _txtSearch;

        public SalesHistoryForm()
        {
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

            // --- GRID (önce eklenir ki kalan alanı doldursun) ---
            _grid = CreateGrid();
            _grid.Columns.Add("Product", "ÜRÜN ADI");
            _grid.Columns["Product"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Columns["Product"].FillWeight = 100;
            _grid.Columns.Add("Barcode", "BARKOD");
            _grid.Columns["Barcode"].Width = 130;
            _grid.Columns.Add("SaleId", "FİŞ NO");
            _grid.Columns["SaleId"].Width = 110;
            _grid.Columns.Add("Date", "TARİH");
            _grid.Columns["Date"].Width = 150;
            _grid.Columns.Add("Price", "TUTAR");
            _grid.Columns["Price"].Width = 120;
            _grid.Columns["Price"].DefaultCellStyle.Format = "C2";
            _grid.Columns["Price"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _grid.Columns["Price"].DefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            Panel gridHost = new Panel { Dock = DockStyle.Fill, BackColor = ColBg, Padding = new Padding(16, 12, 16, 16) };
            gridHost.Controls.Add(_grid);
            this.Controls.Add(gridHost);

            // --- BAŞLIK (form'a en son eklenir → en üstte durur) ---
            this.Controls.Add(BuildHeader());
        }

        // Başlık + toplam + filtre satırını içeren tek kart.
        private Panel BuildHeader()
        {
            Panel header = new Panel { Dock = DockStyle.Top, Height = 128, BackColor = ColSurface };
            header.Paint += (s, e) =>
            {
                using var pen = new Pen(ColBorder, 1);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };

            // Filtre satırı (Dock.Fill → başlığın altındaki tüm alan)
            header.Controls.Add(BuildFilterRow());
            // Başlık satırı (Dock.Top → en üste)
            header.Controls.Add(BuildTitleRow());

            return header;
        }

        // Başlık (sol) + Toplam (sağ) — TableLayoutPanel ile kendiliğinden hizalı.
        private TableLayoutPanel BuildTitleRow()
        {
            TableLayoutPanel row = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 58,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = ColSurface,
                Padding = new Padding(24, 14, 24, 0)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            Label title = new Label
            {
                Text = "Satış Geçmişi",
                Font = new Font("Segoe UI", 17, FontStyle.Bold),
                ForeColor = ColStrong,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };
            row.Controls.Add(title, 0, 0);

            _lblTotal = new Label
            {
                Text = "Toplam: ₺0,00",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = ColGreen,
                AutoSize = true,
                Anchor = AnchorStyles.Right
            };
            row.Controls.Add(_lblTotal, 1, 0);

            return row;
        }

        // Tarih aralığı + arama kutusu.
        private FlowLayoutPanel BuildFilterRow()
        {
            FlowLayoutPanel flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                BackColor = ColSurface,
                Padding = new Padding(24, 8, 24, 0)
            };

            flow.Controls.Add(MakeLabel("Tarih:", new Padding(0, 9, 10, 0)));

            _dtStart = MakeDatePicker(DateTime.Today.AddDays(-7));
            _dtStart.ValueChanged += (s, e) => LoadData();
            flow.Controls.Add(_dtStart);

            flow.Controls.Add(MakeLabel("–", new Padding(8, 8, 8, 0)));

            _dtEnd = MakeDatePicker(DateTime.Today);
            _dtEnd.ValueChanged += (s, e) => LoadData();
            flow.Controls.Add(_dtEnd);

            // Arama kutusu — sade, klasik, biraz büyük.
            _txtSearch = new TextBox
            {
                Width = 320,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 12),
                ForeColor = ColMuted,
                Text = SearchHint,
                Margin = new Padding(24, 5, 0, 0)
            };
            _txtSearch.GotFocus += (s, e) =>
            {
                if (_txtSearch.Text == SearchHint)
                {
                    _txtSearch.Text = "";
                    _txtSearch.ForeColor = ColText;
                }
            };
            _txtSearch.LostFocus += (s, e) =>
            {
                if (_txtSearch.Text.Length == 0)
                {
                    _txtSearch.Text = SearchHint;
                    _txtSearch.ForeColor = ColMuted;
                }
            };
            _txtSearch.TextChanged += (s, e) => FilterData();
            flow.Controls.Add(_txtSearch);

            return flow;
        }

        private Label MakeLabel(string text, Padding margin) => new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 11),
            ForeColor = ColMuted,
            AutoSize = true,
            Margin = margin
        };

        private DateTimePicker MakeDatePicker(DateTime value) => new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Width = 135,
            Font = new Font("Segoe UI", 11),
            Value = value,
            Margin = new Padding(0, 5, 0, 0)
        };

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
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(233, 238, 244),
                RowTemplate = { Height = 40 }
            };
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            grid.DefaultCellStyle.ForeColor = ColText;
            grid.DefaultCellStyle.BackColor = ColSurface;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            grid.DefaultCellStyle.SelectionForeColor = ColStrong;
            grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);

            grid.ColumnHeadersHeight = 42;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = ColMuted;
            grid.ColumnHeadersDefaultCellStyle.BackColor = ColHeaderBg;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            return grid;
        }

        private void LoadData()
        {
            DateTime start = _dtStart.Value.Date;
            DateTime end = _dtEnd.Value.Date.AddDays(1).AddSeconds(-1);

            _grid.Rows.Clear();
            var details = _saleService.GetSalesHistoryWithDetails(start, end);

            int colorIndex = 0;
            foreach (var group in details.GroupBy(x => x.Sale.TransactionCode))
            {
                Color rowColor = RowColors[colorIndex % RowColors.Length];
                colorIndex++;

                foreach (var d in group)
                {
                    int idx = _grid.Rows.Add(
                        d.Product?.Name ?? "",
                        d.Product?.Barcode ?? "",
                        d.Sale.TransactionCode,
                        d.Sale.CreatedDate?.ToString("dd.MM.yyyy HH:mm"),
                        d.TotalPrice
                    );
                    _grid.Rows[idx].DefaultCellStyle.BackColor = rowColor;
                }
            }

            decimal total = details.Sum(x => x.TotalPrice);
            _lblTotal.Text = $"Toplam: {total:C2}";

            if (_txtSearch.Text != SearchHint && _txtSearch.Text.Length > 0)
                FilterData();
        }

        private void FilterData()
        {
            string q = _txtSearch.Text.Trim().ToLower();
            bool showAll = q.Length == 0 || q == SearchHint.ToLower();

            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (showAll) { row.Visible = true; continue; }
                string barcode = row.Cells["Barcode"].Value?.ToString()?.ToLower() ?? "";
                string product = row.Cells["Product"].Value?.ToString()?.ToLower() ?? "";
                row.Visible = barcode.Contains(q) || product.Contains(q);
            }
        }
    }
}
