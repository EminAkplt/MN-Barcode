using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MN_Barcode.WinForms
{
    /// <summary>
    /// ============================================================
    ///  MERKEZİ TASARIM SİSTEMİ (DARK THEME)
    /// ============================================================
    /// Tüm renkler, fontlar ve yardımcı metodlar buradan gelir.
    /// Palette: Tailwind slate-950 / slate-800 + indigo aksan.
    /// </summary>
    public static class Theme
    {
        // ── ARKA PLAN / YÜZEYler ────────────────────────────────────
        public static readonly Color Background  = Color.FromArgb(15,  23,  42);  // slate-950
        public static readonly Color Surface     = Color.FromArgb(30,  41,  59);  // slate-800
        public static readonly Color SurfaceAlt  = Color.FromArgb(51,  65,  85);  // slate-700 (hover/alt yüzey)
        public static readonly Color Sidebar     = Color.FromArgb(2,   6,   23);  // ~siyah
        public static readonly Color SidebarHov  = Color.FromArgb(30,  41,  59);  // hover
        public static readonly Color Header      = Color.FromArgb(2,   6,   23);

        // ── SINIR ───────────────────────────────────────────────────
        public static readonly Color Border      = Color.FromArgb(51,  65,  85);  // slate-700
        public static readonly Color BorderLight = Color.FromArgb(71,  85, 105);  // slate-600

        // ── METİN ───────────────────────────────────────────────────
        public static readonly Color TextPrimary = Color.FromArgb(241, 245, 249); // slate-100
        public static readonly Color TextSecond  = Color.FromArgb(148, 163, 184); // slate-400
        public static readonly Color TextMuted   = Color.FromArgb(71,  85, 105);  // slate-600

        // ── AKSAN ───────────────────────────────────────────────────
        public static readonly Color Accent      = Color.FromArgb(99,  102, 241); // indigo-500
        public static readonly Color AccentHover = Color.FromArgb(79,  70,  229); // indigo-600
        public static readonly Color AccentLight = Color.FromArgb(165, 180, 252); // indigo-300

        // ── ANLAMLI RENKLER ─────────────────────────────────────────
        public static readonly Color Success     = Color.FromArgb(16,  185, 129); // emerald-500
        public static readonly Color Warning     = Color.FromArgb(245, 158, 11);  // amber-500
        public static readonly Color Danger      = Color.FromArgb(239, 68,  68);  // red-500
        public static readonly Color Info        = Color.FromArgb(56,  189, 248); // sky-400

        // ── GERİ UYUMLULUK (SalesForm dokunulmadı) ─────────────────
        public static readonly Color TextStrong    = TextPrimary;
        public static readonly Color TextDark      = TextPrimary;
        public static readonly Color Primary       = Accent;
        public static readonly Color Money         = Success;
        public static readonly Color MoneyBlue     = Info;
        public static readonly Color PanelDark     = Surface;
        public static readonly Color CaptionOnDark = TextSecond;

        // ── TİPOGRAFİ ───────────────────────────────────────────────
        public const string FontName = "Segoe UI";
        public static readonly Font H1      = new Font(FontName, 18, FontStyle.Bold);
        public static readonly Font H2      = new Font(FontName, 13, FontStyle.Bold);
        public static readonly Font H3      = new Font(FontName, 11, FontStyle.Bold);
        public static readonly Font Stat    = new Font(FontName, 26, FontStyle.Bold);
        public static readonly Font Body    = new Font(FontName, 10, FontStyle.Regular);
        public static readonly Font BodySm  = new Font(FontName,  9, FontStyle.Regular);
        public static readonly Font Caption = new Font(FontName,  9, FontStyle.Regular);
        public static readonly Font Number  = new Font("Consolas", 28, FontStyle.Bold);

        // ── BOYUT / BOŞLUK ──────────────────────────────────────────
        public const int PagePad  = 24;
        public const int Gap      = 12;
        public const int CardPad  = 20;
        public const int SidebarW = 260;
        public const int HeaderH  = 60;

        // ════════════════════════════════════════════════════════════
        //  YARDIMCI: Kart kenarlığı çiz
        // ════════════════════════════════════════════════════════════
        public static void PaintCard(Panel panel, PaintEventArgs e, Color? accent = null)
        {
            using (var pen = new Pen(Border, 1))
                e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);

            if (accent.HasValue)
                using (var b = new SolidBrush(accent.Value))
                    e.Graphics.FillRectangle(b, 0, 0, 3, panel.Height);
        }

        // ════════════════════════════════════════════════════════════
        //  YARDIMCI: Modern düz buton uygula
        // ════════════════════════════════════════════════════════════
        public static Button MakeButton(string text, Color bg, Color fg,
                                        int w = 160, int h = 42, float fontSize = 10)
        {
            var btn = new Button
            {
                Text      = text,
                Size      = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = fg,
                Font      = new Font(FontName, fontSize, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                TabStop   = false
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // ════════════════════════════════════════════════════════════
        //  YARDIMCI: DataGridView'i koyu temaya al
        // ════════════════════════════════════════════════════════════
        public static DataGridView MakeDarkGrid()
        {
            var grid = new DataGridView
            {
                Dock                        = DockStyle.Fill,
                BackgroundColor             = Surface,
                ForeColor                   = TextPrimary,
                BorderStyle                 = BorderStyle.None,
                RowHeadersVisible           = false,
                AllowUserToAddRows          = false,
                AllowUserToResizeRows       = false,
                AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode               = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect                 = false,
                ReadOnly                    = true,
                GridColor                   = Border,
                CellBorderStyle             = DataGridViewCellBorderStyle.SingleHorizontal,
                EnableHeadersVisualStyles   = false,
                ColumnHeadersBorderStyle    = DataGridViewHeaderBorderStyle.None,
                ScrollBars                  = ScrollBars.Both
            };
            grid.RowTemplate.Height = 42;

            grid.DefaultCellStyle.Font             = Body;
            grid.DefaultCellStyle.ForeColor        = TextPrimary;
            grid.DefaultCellStyle.BackColor        = Surface;
            grid.DefaultCellStyle.SelectionBackColor = SurfaceAlt;
            grid.DefaultCellStyle.SelectionForeColor = TextPrimary;
            grid.DefaultCellStyle.Padding          = new Padding(6, 0, 6, 0);

            grid.ColumnHeadersHeight = 44;
            grid.ColumnHeadersDefaultCellStyle.Font      = Caption;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextSecond;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            grid.ColumnHeadersDefaultCellStyle.Padding   = new Padding(6, 0, 6, 0);

            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(20, 30, 48);

            return grid;
        }

        // ════════════════════════════════════════════════════════════
        //  YARDIMCI: Koyu kart paneli oluştur (başlıklı)
        // ════════════════════════════════════════════════════════════
        public static Panel MakeCard(string title, Color? accentColor = null)
        {
            var card = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Surface,
                Margin    = new Padding(Gap / 2)
            };

            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(Border, 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                if (accentColor.HasValue)
                    using (var b = new SolidBrush(accentColor.Value))
                        e.Graphics.FillRectangle(b, 0, 0, 3, card.Height);
            };

            var lbl = new Label
            {
                Text      = title,
                Dock      = DockStyle.Top,
                Height    = 48,
                Font      = H3,
                ForeColor = accentColor ?? AccentLight,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(CardPad - (accentColor.HasValue ? 2 : 0), 0, 0, 0)
            };
            card.Controls.Add(lbl);

            // Başlık altı ince çizgi
            var sep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Border };
            card.Controls.Add(sep);

            return card;
        }

        // ════════════════════════════════════════════════════════════
        //  YARDIMCI: Koyu TextBox
        // ════════════════════════════════════════════════════════════
        public static TextBox MakeDarkInput(int width = 200)
        {
            return new TextBox
            {
                Width       = width,
                Height      = 34,
                Font        = Body,
                BackColor   = Color.FromArgb(15, 23, 42),
                ForeColor   = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        // ════════════════════════════════════════════════════════════
        //  YARDIMCI: DateTimePicker'ı tema ile stilize et
        // ════════════════════════════════════════════════════════════
        public static DateTimePicker MakeDatePicker(int width = 120)
        {
            return new DateTimePicker
            {
                Width  = width,
                Format = DateTimePickerFormat.Short,
                Font   = Body,
                CalendarMonthBackground = Surface,
                CalendarForeColor       = TextPrimary,
                CalendarTitleBackColor  = Color.FromArgb(15, 23, 42),
                CalendarTitleForeColor  = TextPrimary
            };
        }
    }
}
