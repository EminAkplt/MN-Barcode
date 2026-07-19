using System;
using System.IO;
using System.Text;

namespace MN_Barcode.WinForms
{
    /// <summary>
    /// Basit dosya tabanlı hata günlüğü.
    ///
    /// Müşteri bilgisayarında bir sorun olduğunda "hata verdi" demek yeterli değil —
    /// teknik desteğin bakabileceği bir kayıt gerekir. Günlük, yönetici yetkisi
    /// gerektirmeyen kullanıcı klasörüne yazılır ve boyutu sınırlanır.
    /// </summary>
    public static class AppLogger
    {
        private static readonly object _kilit = new object();
        private const long MaxBoyutBayt = 2 * 1024 * 1024;   // 2 MB — sonra devredilir

        /// <summary>Günlük klasörü: %LOCALAPPDATA%\MN-Barcode</summary>
        public static string LogKlasoru
        {
            get
            {
                string kok = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(kok, "MN-Barcode");
            }
        }

        public static string LogDosyasi => Path.Combine(LogKlasoru, "hata.log");

        /// <summary>Hatayı günlüğe yazar. Günlüğe yazarken hata olursa sessizce vazgeçer.</summary>
        public static void Yaz(string baglam, Exception ex)
        {
            Yaz($"{baglam}\r\n{ex}");
        }

        public static void Yaz(string mesaj)
        {
            try
            {
                lock (_kilit)
                {
                    Directory.CreateDirectory(LogKlasoru);
                    Devret();

                    var sb = new StringBuilder();
                    sb.Append("─────────────────────────────────────────────\r\n");
                    sb.Append(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")).Append("\r\n");
                    sb.Append(mesaj).Append("\r\n");

                    File.AppendAllText(LogDosyasi, sb.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
                // Günlüğe yazamamak uygulamayı durdurmamalı.
            }
        }

        /// <summary>Günlük 2 MB'ı geçerse eskisini .1 uzantısıyla saklayıp yenisine başlar.</summary>
        private static void Devret()
        {
            try
            {
                var bilgi = new FileInfo(LogDosyasi);
                if (!bilgi.Exists || bilgi.Length < MaxBoyutBayt) return;

                string eski = LogDosyasi + ".1";
                if (File.Exists(eski)) File.Delete(eski);
                File.Move(LogDosyasi, eski);
            }
            catch
            {
                // Devretme başarısızsa günlüğe eklemeye devam et.
            }
        }
    }
}
