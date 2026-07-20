using System;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using MN_Barcode.Business;
using MN_Barcode.DataAccess;
using MN_Barcode.Entities;

namespace MN_Barcode.WinForms
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Beklenmeyen hatalar uygulamayı çökertmemeli: yakala, günlüğe yaz,
            // kullanıcıya anlaşılır mesaj göster. Kasada çöken program = kayıp satış.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => HataBildir("Arayüz hatası", e.Exception, kritik: false);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                HataBildir("Beklenmeyen hata", e.ExceptionObject as Exception, kritik: true);

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!VeritabaniniHazirla()) return;

            Application.Run(new LoginForm());
        }

        /// <summary>
        /// Veritabanını açılışa hazırlar. Başarısızsa kullanıcıya sebebini söyler ve
        /// false döner — uygulama yarım yamalak açılmaz.
        /// </summary>
        private static bool VeritabaniniHazirla()
        {
            var sonuc = DatabaseBootstrapper.Initialize();

            if (!sonuc.IsSuccess)
            {
                AppLogger.Yaz($"Veritabanı başlatma hatası [{sonuc.Status}]\r\n{sonuc.TechnicalDetail}");

                // Teknik rapor doğrudan ekranda gösterilir. "Şu gizli klasördeki
                // log dosyasına bak" demek sahada işe yaramıyordu.
                string rapor =
                    sonuc.TechnicalDetail +
                    $"\r\n── ORTAM ──\r\n" +
                    $"Surum      : {Application.ProductVersion}\r\n" +
                    $"Windows    : {Environment.OSVersion}\r\n" +
                    $"64-bit OS  : {Environment.Is64BitOperatingSystem}\r\n" +
                    $"Kullanici  : {Environment.UserDomainName}\\{Environment.UserName}\r\n" +
                    $"Bilgisayar : {Environment.MachineName}\r\n" +
                    $"Uygulama   : {AppContext.BaseDirectory}\r\n" +
                    $"Hata kaydi : {AppLogger.LogDosyasi}\r\n";

                StartupErrorForm.Goster("MN-Barcode başlatılamadı", sonuc.UserMessage, rapor);
                return false;
            }

            // Şema hazır; başlangıç verilerini tamamla. Buradaki hata ölümcül değil —
            // günlüğe yazılır ama uygulama açılmaya devam eder.
            try
            {
                BaslangicVerileriniOlustur();
            }
            catch (Exception ex)
            {
                AppLogger.Yaz("Başlangıç verileri oluşturulamadı", ex);
            }

            return true;
        }

        private static void BaslangicVerileriniOlustur()
        {
            using var context = new BarcodeContext();

            if (!context.Users.Any())
            {
                context.Users.Add(new AppUser
                {
                    Username = "admin",
                    // Varsayılan şifre de hash'lenerek saklanır; veritabanına
                    // hiçbir zaman düz metin şifre yazılmaz.
                    Password = PasswordHasher.Hash("admin123"),
                    Role = UserRole.Admin
                });
                context.SaveChanges();
            }

            // Stok hiçbir zaman eksi olamaz: geçmişte eksiye düşmüş kayıtları 0'a çek.
            // ExecuteUpdate ile tek SQL cümlesi — kayıtlar belleğe alınmaz.
            // Her açılışta çalıştığı için maliyeti düşük tutulmalı; eskiden eşleşen
            // tüm ürünler belleğe çekiliyor ve tek tek güncelleniyordu.
            context.Products
                .Where(p => p.StockQuantity < 0)
                .ExecuteUpdate(s => s.SetProperty(p => p.StockQuantity, 0d));
        }

        /// <summary>
        /// Yakalanan hatayı günlüğe yazar ve kullanıcıya gösterir.
        /// Kritik hatalarda uygulama kapanır, aksi halde çalışmaya devam eder.
        /// </summary>
        private static void HataBildir(string baglam, Exception ex, bool kritik)
        {
            try
            {
                if (ex != null) AppLogger.Yaz(baglam, ex);
                else AppLogger.Yaz($"{baglam} (ayrıntı yok)");

                string mesaj = kritik
                    ? "Beklenmeyen bir hata oluştu ve program kapanacak.\n\n"
                    : "Bir hata oluştu. Programı kullanmaya devam edebilirsiniz.\n\n";

                MessageBox.Show(
                    mesaj +
                    $"Hata: {ex?.Message ?? "bilinmiyor"}\n\n" +
                    $"Teknik kayıt: {AppLogger.LogDosyasi}",
                    "MN-Barcode",
                    MessageBoxButtons.OK,
                    kritik ? MessageBoxIcon.Error : MessageBoxIcon.Warning);
            }
            catch
            {
                // Hata bildirirken hata olursa yapacak bir şey yok.
            }
        }
    }
}
