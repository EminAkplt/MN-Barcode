using System;
using System.Drawing;
using System.Text;

namespace MN_Barcode.WinForms
{
    /// <summary>
    /// Harici kütüphane olmadan, saf GDI+ ile Code128 (Set B) barkod çizer.
    /// Code128 herhangi bir ASCII metni (rakam/harf) kodlar; okuyucu taranınca
    /// tam olarak kodlanan metni geri verir — böylece ürün barkoduyla birebir uyumlu olur.
    /// </summary>
    public static class Code128
    {
        // Standart Code128 desen tablosu (0..106). Her değer bar/boşluk genişlikleri (1-4).
        // 106 = Stop (7 haneli, sonlandırma barı dahil).
        private static readonly string[] P =
        {
            "212222","222122","222221","121223","121322","131222","122213","122312","132212","221213",
            "221312","231212","112232","122132","122231","113222","123122","123221","223211","221132",
            "221231","213212","223112","312131","311222","321122","321221","312212","322112","322211",
            "212123","212321","232121","111323","131123","131321","112313","132113","132311","211313",
            "231113","231311","112133","112331","132131","113123","113321","133121","313121","211331",
            "231131","213113","213311","213131","311123","311321","331121","312113","312311","332111",
            "314111","221411","431111","111224","111422","121124","121421","141122","141221","112214",
            "112412","122114","122411","142112","142211","241211","221114","413111","241112","134111",
            "111242","121142","121241","114212","124112","124211","411212","421112","421211","212141",
            "214121","412121","111143","111341","131141","114113","114311","411113","411311","113141",
            "114131","311141","411131","211412","211214","211232","2331112"
        };

        private const int StartB = 104;
        private const int Stop = 106;

        /// <summary>
        /// Verilen metnin Code128-B modül desenini (bar/boşluk genişlikleri) döndürür.
        /// İlk hane bardır, sonra sırayla boşluk/bar diye devam eder.
        /// </summary>
        public static string Encode(string data)
        {
            if (string.IsNullOrEmpty(data)) return "";

            var sb = new StringBuilder();
            sb.Append(P[StartB]);

            int checksum = StartB;
            int pos = 1;
            foreach (char ch in data)
            {
                int c = ch;
                if (c < 32 || c > 126) c = 32;   // Set B dışı karakterleri boşlukla değiştir
                int val = c - 32;
                sb.Append(P[val]);
                checksum += val * pos;
                pos++;
            }

            checksum %= 103;
            sb.Append(P[checksum]);
            sb.Append(P[Stop]);
            return sb.ToString();
        }

        /// <summary>
        /// Barkodu verilen dikdörtgene sığacak şekilde net (crisp) barlarla çizer.
        /// Modül genişliği dikdörtgen genişliğine göre hesaplanır — her ölçekte keskin.
        /// </summary>
        public static void Draw(Graphics g, string data, RectangleF rect, Color? barColor = null)
        {
            string modules = Encode(data);
            if (modules.Length == 0) return;

            int totalUnits = 0;
            foreach (char d in modules) totalUnits += d - '0';
            if (totalUnits <= 0) return;

            float unit = rect.Width / totalUnits;
            using var brush = new SolidBrush(barColor ?? Color.Black);

            float x = rect.X;
            bool bar = true; // ilk eleman bar
            foreach (char d in modules)
            {
                int w = d - '0';
                float width = w * unit;
                if (bar)
                    g.FillRectangle(brush, x, rect.Y, width, rect.Height);
                x += width;
                bar = !bar;
            }
        }

        /// <summary>Barkodu bir Bitmap olarak üretir (önizleme/kaydetme için).</summary>
        public static Bitmap CreateBitmap(string data, int width, int height)
        {
            var bmp = new Bitmap(Math.Max(1, width), Math.Max(1, height));
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            Draw(g, data, new RectangleF(0, 0, width, height));
            return bmp;
        }
    }
}
