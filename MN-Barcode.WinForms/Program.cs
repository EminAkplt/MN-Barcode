using MN_Barcode.DataAccess; // Context'e eriþim
using MN_Barcode.Entities;   // AppUser oluþturmak için
using MN_Barcode.WinForms;
using System;
using System.Linq; // Veritabaný sorgusu için þart
using System.Windows.Forms;

namespace MN_Barcode.UI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // --- 1. OTOMATÝK ADMÝN OLUÞTURMA KODU (SEEDER) ---
            // Bu blok, program açýlmadan önce veritabanýný kontrol eder.
            try
            {
                using (var context = new BarcodeContext())
                {
                    // Eðer veritabaný yoksa oluþtur (Garanti olsun)
                    context.Database.EnsureCreated();

                    // Kullanýcýlar tablosu boþ mu? Veya 'admin' yok mu?
                    if (!context.Users.Any(u => u.Username == "admin"))
                    {
                        // Yoksa hemen bir tane oluþtur
                        var adminUser = new AppUser
                        {
                            Username = "admin",
                            Password = "123", // Þimdilik basit þifre
                            FullName = "Süper Admin",
                            Role = "Admin",
                            CreatedDate = DateTime.Now
                        };

                        context.Users.Add(adminUser);
                        context.SaveChanges(); // Kaydet
                    }
                }
            }
            catch (Exception ex)
            {
                // Veritabaný baðlantý hatasý olursa program patlamasýn, mesaj versin.
                MessageBox.Show("Veritabaný baðlantýsý kurulamadý!\n" + ex.Message, "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // Programý açmadan kapat
            }
            // ----------------------------------------------------

            // 2. LOGÝN FORMUNU AÇ
            Application.Run(new LoginForm());
        }
    }
}