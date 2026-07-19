using System;
using System.Globalization;

namespace MN_Barcode.Business
{
    /// <summary>
    /// Kullanıcının elle girdiği para/miktar metnini güvenli şekilde sayıya çevirir.
    ///
    /// NEDEN GEREKLİ:
    /// decimal.TryParse'ın kültüre bağlı hali kasada yanlış para tahsil ediyordu.
    /// Türkçe Windows'ta "." binlik ayracı sayıldığı için:
    ///     "1.5"   -> 15      (15 TL tahsil edilir, 1.5 TL yerine)
    ///     "12.50" -> 1250    (1250 TL tahsil edilir!)
    /// İngilizce Windows'ta ise "12,50" -> 1250 olur. İki yönlü bir para hatası.
    ///
    /// ÇÖZÜM:
    /// Ayraç kültürden bağımsız yorumlanır. Kural basit ve öngörülebilir:
    ///   - Bir metinde hem "." hem "," varsa, SONDAKİ ondalık ayracıdır,
    ///     diğeri binlik ayracıdır.      "1.234,56" -> 1234.56 / "1,234.56" -> 1234.56
    ///   - Tek tür ayraç birden fazla kez geçiyorsa binlik ayracıdır.
    ///                                    "1.234.567" -> 1234567
    ///   - Tek tür ayraç bir kez geçiyorsa ondalık ayracıdır.
    ///                                    "12,50" -> 12.50 / "12.50" -> 12.50
    /// </summary>
    public static class TutarParser
    {
        /// <summary>
        /// Metni ondalık sayıya çevirir. Başarısızsa false döner ve <paramref name="deger"/> 0 olur.
        /// </summary>
        public static bool TryParse(string girdi, out decimal deger)
        {
            deger = 0m;
            if (string.IsNullOrWhiteSpace(girdi)) return false;

            string s = Temizle(girdi);
            if (s.Length == 0) return false;

            int noktaSayisi  = Say(s, '.');
            int virgulSayisi = Say(s, ',');

            if (noktaSayisi > 0 && virgulSayisi > 0)
            {
                // Sondaki ayraç ondalıktır, diğeri binliktir.
                if (s.LastIndexOf(',') > s.LastIndexOf('.'))
                    s = s.Replace(".", "").Replace(',', '.');
                else
                    s = s.Replace(",", "");
            }
            else if (virgulSayisi > 1)
            {
                s = s.Replace(",", "");          // binlik ayracı
            }
            else if (virgulSayisi == 1)
            {
                s = s.Replace(',', '.');         // ondalık ayracı
            }
            else if (noktaSayisi > 1)
            {
                s = s.Replace(".", "");          // binlik ayracı
            }
            // Tek nokta zaten ondalık ayracı olarak geçerli.

            return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out deger);
        }

        /// <summary>Çözümlenemezse verilen varsayılanı döndürür.</summary>
        public static decimal ParseOrDefault(string girdi, decimal varsayilan = 0m)
        {
            return TryParse(girdi, out decimal d) ? d : varsayilan;
        }

        /// <summary>Miktar alanları için (stok, adet) — decimal yerine double döner.</summary>
        public static bool TryParseMiktar(string girdi, out double deger)
        {
            bool ok = TryParse(girdi, out decimal d);
            deger = ok ? (double)d : 0d;
            return ok;
        }

        /// <summary>Para birimi simgelerini, boşlukları ve görünmez karakterleri atar.</summary>
        private static string Temizle(string girdi)
        {
            var sb = new System.Text.StringBuilder(girdi.Length);
            foreach (char c in girdi)
            {
                if (char.IsDigit(c) || c == '.' || c == ',' || c == '-')
                    sb.Append(c);
                // ₺, TL, boşluk, sekme vb. atılır.
            }
            return sb.ToString();
        }

        private static int Say(string s, char hedef)
        {
            int n = 0;
            foreach (char c in s) if (c == hedef) n++;
            return n;
        }
    }
}
