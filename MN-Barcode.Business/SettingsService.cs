using Microsoft.EntityFrameworkCore;
using MN_Barcode.DataAccess;
using MN_Barcode.Entities;
using System.Linq;

namespace MN_Barcode.Business
{
    /// <summary>
    /// Sistem ayarları (şifreler vb.) iş mantığı servisi.
    /// Ayarlar anahtar-değer (key-value) olarak Settings tablosunda tutulur.
    /// Her metot kendi kısa ömürlü context'ini kullanır (bkz. CategoryService notu).
    /// </summary>
    public class SettingsService
    {
        public const string DEFAULT_PASSWORD = "123";
        public const string KEY_USER_PASSWORD = "UserPassword";
        public const string KEY_ADMIN_PASSWORD = "AdminPassword";
        public const string KEY_REPORTS_PASSWORD = "ReportsPassword";

        private static bool _defaultsEnsured = false;

        public SettingsService()
        {
            if (!_defaultsEnsured)
            {
                EnsureDefaultSettings();
                _defaultsEnsured = true;
            }
        }

        private void EnsureDefaultSettings()
        {
            using var context = new BarcodeContext();
            bool degisiklikVar = false;

            foreach (string anahtar in new[] { KEY_USER_PASSWORD, KEY_ADMIN_PASSWORD, KEY_REPORTS_PASSWORD })
            {
                if (context.Settings.Any(s => s.SettingKey == anahtar)) continue;

                // Varsayılan şifre de hash'lenerek saklanır — hiçbir şifre
                // veritabanına düz metin yazılmaz.
                context.Settings.Add(new SystemSettings
                {
                    SettingKey = anahtar,
                    SettingValue = PasswordHasher.Hash(DEFAULT_PASSWORD)
                });
                degisiklikVar = true;
            }

            if (degisiklikVar)
                context.SaveChanges();
        }

        /// <summary>Verilen anahtarın değerini getirir (yoksa boş metin).</summary>
        public string GetSetting(string key)
        {
            using var context = new BarcodeContext();
            var setting = context.Settings.FirstOrDefault(s => s.SettingKey == key);
            return setting?.SettingValue ?? "";
        }

        /// <summary>Anahtar varsa değerini günceller, yoksa yeni ayar ekler.</summary>
        public void SetSetting(string key, string value)
        {
            using var context = new BarcodeContext();
            var setting = context.Settings.FirstOrDefault(s => s.SettingKey == key);
            if (setting != null)
            {
                setting.SettingValue = value;
            }
            else
            {
                context.Settings.Add(new SystemSettings { SettingKey = key, SettingValue = value });
            }
            context.SaveChanges();
        }

        public bool ValidateUserPassword(string password)    => Dogrula(KEY_USER_PASSWORD, password);
        public bool ValidateAdminPassword(string password)   => Dogrula(KEY_ADMIN_PASSWORD, password);
        public bool ValidateReportsPassword(string password) => Dogrula(KEY_REPORTS_PASSWORD, password);

        public void UpdateUserPassword(string newPassword)    => SifreKaydet(KEY_USER_PASSWORD, newPassword);
        public void UpdateAdminPassword(string newPassword)   => SifreKaydet(KEY_ADMIN_PASSWORD, newPassword);
        public void UpdateReportsPassword(string newPassword) => SifreKaydet(KEY_REPORTS_PASSWORD, newPassword);

        /// <summary>
        /// Şifreyi PBKDF2 ile doğrular.
        ///
        /// Eskiden düz metin "==" karşılaştırması yapılıyordu ve şifreler veritabanında
        /// açıkta duruyordu. Mevcut kurulumlardaki düz metin değerler de kabul edilir;
        /// doğrulama başarılıysa kayıt sessizce hash'e yükseltilir. Böylece güncelleme
        /// sonrası kimse programa giremez duruma düşmez.
        /// </summary>
        private bool Dogrula(string anahtar, string password)
        {
            if (password == null) return false;

            string saklanan = GetSetting(anahtar);
            if (string.IsNullOrEmpty(saklanan)) return false;

            if (!PasswordHasher.Verify(password, saklanan)) return false;

            if (PasswordHasher.NeedsUpgrade(saklanan))
            {
                try { SetSetting(anahtar, PasswordHasher.Hash(password)); }
                catch { /* yükseltme başarısızsa giriş yine de geçerli */ }
            }

            return true;
        }

        private void SifreKaydet(string anahtar, string yeniSifre)
        {
            if (string.IsNullOrWhiteSpace(yeniSifre)) return;
            SetSetting(anahtar, PasswordHasher.Hash(yeniSifre));
        }

        /// <summary>
        /// Tüm sistemi sıfırla (Users tablosu hariç): ürün, kategori, gider, satış
        /// verilerini siler ve şifreleri varsayılana döndürür.
        /// </summary>
        public void ResetSystem()
        {
            using var context = new BarcodeContext();

            // Veri tablolarını sil. ExecuteDelete: satırları belleğe çekmeden
            // tek SQL ile siler. Eskiden tüm tablolar belleğe alınıp satır satır
            // DELETE üretiliyordu; büyük katalogda bu dakikalar sürebilirdi.
            // Sıra önemli: önce çocuk tablolar, sonra ebeveynler (FK ihlali olmasın).
            context.SaleDetails.ExecuteDelete();
            context.Sales.ExecuteDelete();
            context.Expenses.ExecuteDelete();
            context.Products.ExecuteDelete();
            context.Categories.ExecuteDelete();

            // Şifre ayarlarını varsayılana döndür (hash'lenmiş olarak).
            string varsayilanHash = PasswordHasher.Hash(DEFAULT_PASSWORD);

            foreach (string anahtar in new[] { KEY_USER_PASSWORD, KEY_ADMIN_PASSWORD, KEY_REPORTS_PASSWORD })
            {
                var kayit = context.Settings.FirstOrDefault(s => s.SettingKey == anahtar);
                if (kayit != null) kayit.SettingValue = varsayilanHash;
            }

            context.SaveChanges();
        }
    }
}
