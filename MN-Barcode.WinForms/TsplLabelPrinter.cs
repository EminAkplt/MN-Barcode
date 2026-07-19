using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace MN_Barcode.WinForms
{
    /// <summary>
    /// Termal etiket yazıcılarına (Xprinter XP-DT325B, TSC, Argox vb.) doğrudan
    /// TSPL komutu gönderir.
    ///
    /// Neden GDI (PrintDocument) değil:
    /// Windows sürücüsü etiket yazıcısını A4/Letter sayfa gibi görebiliyor; bu durumda
    /// GDI çıktısı sayfa dışına taşıyor ve etiket boş çıkıyor. TSPL'de etiket ölçüsünü,
    /// boşluk sensörünü ve barkodu doğrudan cihaza bildiriyoruz — sürücü ayarları devre dışı.
    /// Ayrıca barkodu yazıcının kendi motoru çizdiği için barlar her zaman keskin ve okunur.
    /// </summary>
    public static class TsplLabelPrinter
    {
        /// <summary>203 dpi ≈ 8 nokta/mm. Yaygın termal etiket yazıcı çözünürlüğü.</summary>
        public const float DotsPerMm = 8f;

        /// <summary>Etiketler arası boşluk (gap) — standart rulolarda 2 mm.</summary>
        public const int DefaultGapMm = 2;

        // TSPL dahili bitmap fontları: (kod, nokta genişliği, nokta yüksekliği) @203dpi
        private static readonly (string Id, int W, int H)[] Fonts =
        {
            ("1", 8, 12),
            ("2", 12, 20),
            ("3", 16, 24),
            ("4", 24, 32),
            ("5", 32, 48)
        };

        // ── Yapılandırma önbelleği ───────────────────────────────────
        // SIZE/GAP komutları cihazda etiket boyu kalibrasyonunu tetikler ve baskıyı
        // belirgin şekilde geciktirir (ölçüm: 335 ms → 57 ms). Ölçü değişmediği sürece
        // başlığı tekrar göndermeye gerek yok.
        private static string _sonYapilandirma;
        private static string _sonYazici;
        private static DateTime _sonBaskiUtc = DateTime.MinValue;

        /// <summary>
        /// Bu süreden uzun aradan sonra yapılandırma yeniden gönderilir — yazıcı
        /// arada kapatılıp açılmış olabilir ve ayarlarını kaybetmiş olabilir.
        /// </summary>
        private static readonly TimeSpan YapilandirmaTazeleme = TimeSpan.FromSeconds(60);

        /// <summary>Cihaz ayarları (etiket ölçüsü, boşluk, hız, kod sayfası).</summary>
        private static string BuildConfig(int widthMm, int heightMm, int gapMm)
        {
            var sb = new StringBuilder();
            sb.Append("SIZE ").Append(widthMm).Append(" mm,").Append(heightMm).Append(" mm\r\n");
            sb.Append("GAP ").Append(gapMm).Append(" mm,0 mm\r\n");
            sb.Append("SPEED 4\r\n");
            sb.Append("DENSITY 10\r\n");
            sb.Append("DIRECTION 1\r\n");
            sb.Append("REFERENCE 0,0\r\n");
            sb.Append("CODEPAGE 857\r\n");     // Türkçe karakter desteği
            return sb.ToString();
        }

        /// <summary>
        /// Etiket içeriğini (yazı + barkod) üretir. Cihaz ayarlarını içermez.
        /// </summary>
        public static string BuildContent(string productName, string barcode, string priceText,
                                          int widthMm, int heightMm, int copies)
        {
            int w = (int)(widthMm * DotsPerMm);
            int h = (int)(heightMm * DotsPerMm);

            var sb = new StringBuilder();
            sb.Append("CLS\r\n");

            // ── Ürün adı (üst) ─────────────────────────────────────────
            int y = (int)(h * 0.06f);
            int nameH = AppendCentered(sb, productName, y, w, (int)(h * 0.16f));

            // ── Barkod (orta) ──────────────────────────────────────────
            // Okunur kod (human readable) barkodun hemen altına yazıcı tarafından basılır.
            int barY = y + nameH + (int)(h * 0.04f);
            int barH = (int)(h * 0.40f);
            if (!string.IsNullOrWhiteSpace(barcode))
            {
                int modules = EstimateCode128Modules(barcode);
                int narrow = Math.Max(1, (int)(w * 0.92f) / Math.Max(1, modules));
                int barW = modules * narrow;
                int barX = Math.Max(0, (w - barW) / 2);

                sb.Append("BARCODE ").Append(barX).Append(',').Append(barY)
                  .Append(",\"128\",").Append(barH)
                  .Append(",1,0,").Append(narrow).Append(',').Append(narrow)
                  .Append(",\"").Append(Escape(barcode)).Append("\"\r\n");
            }

            // ── Fiyat (alt) ────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(priceText))
            {
                int priceH = (int)(h * 0.20f);
                int priceY = h - priceH - (int)(h * 0.04f);
                AppendCentered(sb, priceText, priceY, w, priceH);
            }

            sb.Append("PRINT ").Append(Math.Max(1, copies)).Append(",1\r\n");
            return sb.ToString();
        }

        /// <summary>
        /// Etiketi yazdırır. Cihaz ayarları yalnızca gerektiğinde (ölçü değiştiyse,
        /// yazıcı değiştiyse veya uzun aradan sonra) gönderilir — bu, ardışık
        /// baskılarda gereksiz kalibrasyon gecikmesini ortadan kaldırır.
        /// Başarılıysa null, aksi halde hata metni döner.
        /// </summary>
        public static string Print(string printerName, string productName, string barcode, string priceText,
                                   int widthMm, int heightMm, int copies, int gapMm = DefaultGapMm)
        {
            string config = BuildConfig(widthMm, heightMm, gapMm);
            string content = BuildContent(productName, barcode, priceText, widthMm, heightMm, copies);

            bool yapilandirmaGerekli =
                !string.Equals(_sonYazici, printerName, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(_sonYapilandirma, config, StringComparison.Ordinal) ||
                (DateTime.UtcNow - _sonBaskiUtc) > YapilandirmaTazeleme;

            string error = Send(printerName, yapilandirmaGerekli ? config + content : content);
            if (error != null)
            {
                // Hata durumunda önbelleği geçersiz kıl: sonraki denemede ayarlar tekrar gitsin.
                _sonYapilandirma = null;
                _sonYazici = null;
                return error;
            }

            _sonYapilandirma = config;
            _sonYazici = printerName;
            _sonBaskiUtc = DateTime.UtcNow;
            return null;
        }

        /// <summary>
        /// Yapılandırma önbelleğini temizler — bir sonraki baskıda cihaz ayarları
        /// mutlaka yeniden gönderilir. Yazıcı kapatılıp açıldığında kullanılır.
        /// </summary>
        public static void ResetConfigCache()
        {
            _sonYapilandirma = null;
            _sonYazici = null;
            _sonBaskiUtc = DateTime.MinValue;
        }

        /// <summary>
        /// Metni etikete ortalayarak yazar; kutuya sığan en büyük fontu seçer.
        /// Geri dönüş: kullanılan satır yüksekliği (nokta).
        /// </summary>
        private static int AppendCentered(StringBuilder sb, string text, int y, int labelW, int maxH)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            text = text.Trim();

            // Sığan en büyük fontu seç (büyükten küçüğe).
            for (int i = Fonts.Length - 1; i >= 0; i--)
            {
                var f = Fonts[i];
                int textW = text.Length * f.W;
                if ((textW <= labelW * 0.96f && f.H <= maxH) || i == 0)
                {
                    // En küçük font bile sığmıyorsa metni kırp.
                    if (textW > labelW * 0.96f)
                    {
                        int fit = Math.Max(1, (int)(labelW * 0.96f) / f.W);
                        if (text.Length > fit) text = text.Substring(0, fit);
                        textW = text.Length * f.W;
                    }
                    int x = Math.Max(0, (labelW - textW) / 2);
                    sb.Append("TEXT ").Append(x).Append(',').Append(y)
                      .Append(",\"").Append(f.Id).Append("\",0,1,1,\"")
                      .Append(Escape(text)).Append("\"\r\n");
                    return f.H;
                }
            }
            return 0;
        }

        /// <summary>
        /// Code128 modül (dar bar birimi) sayısını üstten tahmin eder.
        /// Start + veri + kontrol = 11'er modül, Stop = 13 modül.
        /// Sayısal veride yazıcı Set C'ye geçip daha dar basabilir — bu tahmin güvenli taraftadır.
        /// </summary>
        private static int EstimateCode128Modules(string data)
        {
            return 11 * (data.Length + 2) + 13;
        }

        /// <summary>TSPL'de ters bölü kaçış karakteridir; tırnak ve ters bölü korunmalı.</summary>
        private static string Escape(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        /// <summary>
        /// Fiyatı etikete uygun biçime getirir. TSPL dahili fontlarında ₺ sembolü
        /// bulunmadığı için "TL" yazılır.
        /// </summary>
        public static string FormatPrice(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            string cleaned = raw.Replace("₺", "").Replace("TL", "").Trim().Replace(",", ".");
            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal p))
                return p.ToString("#,##0.00", CultureInfo.InvariantCulture) + " TL";
            return raw.Trim();
        }

        /// <summary>
        /// Metni CP857'ye (MS-DOS Türkçe) çevirir — TSPL'e gönderdiğimiz CODEPAGE 857 ile eşleşir.
        /// .NET 5+ üzerinde Encoding.GetEncoding(857) ek paket olmadan çalışmadığı için
        /// Türkçe karakterler elle eşlenir; tablo dışı karakterler ASCII karşılığına düşürülür.
        /// </summary>
        private static byte[] EncodeCp857(string text)
        {
            var bytes = new byte[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c < 128) { bytes[i] = (byte)c; continue; }

                switch (c)
                {
                    case 'Ç': bytes[i] = 0x80; break;
                    case 'ü': bytes[i] = 0x81; break;
                    case 'ç': bytes[i] = 0x87; break;
                    case 'ı': bytes[i] = 0x8D; break;
                    case 'ö': bytes[i] = 0x94; break;
                    case 'İ': bytes[i] = 0x98; break;
                    case 'Ö': bytes[i] = 0x99; break;
                    case 'Ü': bytes[i] = 0x9A; break;
                    case 'Ş': bytes[i] = 0x9E; break;
                    case 'ş': bytes[i] = 0x9F; break;
                    case 'Ğ': bytes[i] = 0xA6; break;
                    case 'ğ': bytes[i] = 0xA7; break;
                    default:  bytes[i] = (byte)AsciiFallback(c); break;
                }
            }
            return bytes;
        }

        /// <summary>CP857'de karşılığı olmayan karakterler için okunabilir ASCII karşılığı.</summary>
        private static char AsciiFallback(char c)
        {
            switch (c)
            {
                case '₺': return '.';
                case '’': case '‘': return '\'';
                case '“': case '”': return '"';
                case '–': case '—': return '-';
                default: return '?';
            }
        }

        // ─────────────────────────────────────────────────────────────
        // RAW yazdırma (spooler üzerinden, sürücü veriye dokunmadan)
        // ─────────────────────────────────────────────────────────────

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class DOCINFO
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string pDocName;
            [MarshalAs(UnmanagedType.LPWStr)] public string pOutputFile;
            [MarshalAs(UnmanagedType.LPWStr)] public string pDataType;
        }

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool OpenPrinter(string src, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level,
            [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFO di);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        /// <summary>
        /// Komutları yazıcıya ham (RAW) olarak gönderir.
        /// Başarılıysa null, aksi halde hata metni döner.
        /// </summary>
        public static string Send(string printerName, string commands)
        {
            if (string.IsNullOrWhiteSpace(printerName))
                return "Yazıcı seçilmedi.";

            byte[] bytes = EncodeCp857(commands);

            IntPtr hPrinter;
            if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                return $"Yazıcı açılamadı (kod {Marshal.GetLastWin32Error()}). Yazıcı adı: {printerName}";

            IntPtr buf = IntPtr.Zero;
            try
            {
                var di = new DOCINFO { pDocName = "MN-Barcode Etiket", pDataType = "RAW" };

                if (!StartDocPrinter(hPrinter, 1, di))
                    return $"Baskı başlatılamadı (kod {Marshal.GetLastWin32Error()}).";

                if (!StartPagePrinter(hPrinter))
                {
                    EndDocPrinter(hPrinter);
                    return $"Sayfa başlatılamadı (kod {Marshal.GetLastWin32Error()}).";
                }

                buf = Marshal.AllocCoTaskMem(bytes.Length);
                Marshal.Copy(bytes, 0, buf, bytes.Length);

                bool ok = WritePrinter(hPrinter, buf, bytes.Length, out int written);
                int err = Marshal.GetLastWin32Error();

                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);

                if (!ok) return $"Veri gönderilemedi (kod {err}).";
                if (written != bytes.Length) return $"Veri eksik gönderildi ({written}/{bytes.Length} bayt).";
                return null;
            }
            finally
            {
                if (buf != IntPtr.Zero) Marshal.FreeCoTaskMem(buf);
                ClosePrinter(hPrinter);
            }
        }
    }
}
