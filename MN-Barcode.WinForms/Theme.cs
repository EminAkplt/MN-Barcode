using System.Drawing;
using System.Windows.Forms;

namespace MN_Barcode.WinForms
{
    /// <summary>
    /// ============================================================
    ///  MERKEZİ TASARIM AYARI (DESIGN SYSTEM)
    /// ============================================================
    /// Tüm ekranların renk, font ve boşluk değerleri BURADAN gelir.
    /// Tek bir yerde tutmanın amacı: tasarımı değiştirmek istediğinde
    /// her formu tek tek düzenlemek yerine sadece bu dosyayı değiştirirsin;
    /// uygulamanın tamamı tutarlı kalır.
    ///
    /// Tasarım dili: sade, az renk, bol boşluk, abartısız (flat).
    /// Renkler Tailwind "slate" paletinden esinlenilmiştir.
    /// </summary>
    public static class Theme
    {
        // ---------- RENKLER ----------
        public static readonly Color Background = Color.FromArgb(248, 250, 252); // Sayfa zemini (açık gri)
        public static readonly Color Surface    = Color.White;                  // Kart/panel zemini
        public static readonly Color Sidebar    = Color.FromArgb(30, 41, 59);   // Koyu menü/üst bar
        public static readonly Color Border      = Color.FromArgb(226, 232, 240);// İnce ayraç çizgisi

        public static readonly Color TextStrong = Color.FromArgb(15, 23, 42);   // En koyu metin (başlık, rakam)
        public static readonly Color TextDark    = Color.FromArgb(30, 41, 59);   // Normal koyu metin
        public static readonly Color TextMuted   = Color.FromArgb(100, 116, 139);// Soluk metin (etiket, ipucu)

        // Vurgu renkleri (yalın tutuldu — sadece anlam taşıyanlar)
        public static readonly Color Primary = Color.FromArgb(37, 99, 235);   // Mavi  (genel vurgu)
        public static readonly Color PrimaryDark = Color.FromArgb(29, 78, 188); // Koyu mavi (hover)
        public static readonly Color Success = Color.FromArgb(22, 163, 74);   // Yeşil (ciro, olumlu)
        public static readonly Color Warning = Color.FromArgb(217, 119, 6);   // Amber (uyarı, stok)
        public static readonly Color Danger  = Color.FromArgb(220, 38, 38);   // Kırmızı (kritik, hata)

        // --- "HIZLI SATIŞ" EKRANI DİLİ (POS tarzı: koyu panel + canlı rakam) ---
        public static readonly Color PanelDark = Color.FromArgb(30, 41, 59);  // Koyu kart zemini (TOPLAM paneli gibi)
        public static readonly Color Money     = Color.FromArgb(46, 204, 113);// Canlı yeşil (para/ciro rakamı)
        public static readonly Color MoneyBlue = Color.FromArgb(52, 152, 219);// Canlı mavi (ikincil rakam)
        public static readonly Color CaptionOnDark = Color.Silver;            // Koyu panel üzerindeki etiket

        // ---------- TİPOGRAFİ ----------
        public const string FontName = "Segoe UI";
        public static readonly Font H1      = new Font(FontName, 17, FontStyle.Bold);   // Sayfa başlığı
        public static readonly Font H2      = new Font(FontName, 12, FontStyle.Bold);   // Kart/bölüm başlığı
        public static readonly Font Stat    = new Font(FontName, 24, FontStyle.Bold);   // Büyük rakam (KPI)
        public static readonly Font Body    = new Font(FontName, 10, FontStyle.Regular);// Genel metin
        public static readonly Font Caption = new Font(FontName, 9,  FontStyle.Regular);// Küçük etiket
        public static readonly Font Number  = new Font("Consolas", 30, FontStyle.Bold); // Büyük canlı rakam (POS tarzı)

        // ---------- BOŞLUK / ÖLÇÜ ----------
        public const int PagePad = 24; // Sayfa kenar boşluğu
        public const int Gap     = 16; // Kartlar arası boşluk
        public const int CardPad = 20; // Kart içi boşluk

        // ============================================================
        //  YARDIMCI: Bir paneli "kart" gibi çiz (ince kenarlık + sol vurgu)
        //  Paint olayında çağrılır. accent verilirse sol kenara renkli şerit ekler.
        // ============================================================
        public static void PaintCard(Panel panel, PaintEventArgs e, Color? accent = null)
        {
            // İnce dış kenarlık
            using (var pen = new Pen(Border, 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            }

            // Sol vurgu şeridi (opsiyonel)
            if (accent.HasValue)
            {
                using (var brush = new SolidBrush(accent.Value))
                {
                    e.Graphics.FillRectangle(brush, 0, 0, 3, panel.Height);
                }
            }
        }
    }
}
