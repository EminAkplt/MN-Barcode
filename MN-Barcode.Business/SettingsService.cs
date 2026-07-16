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
            bool hasChanges = false;

            if (!context.Settings.Any(s => s.SettingKey == KEY_USER_PASSWORD))
            {
                context.Settings.Add(new SystemSettings { SettingKey = KEY_USER_PASSWORD, SettingValue = DEFAULT_PASSWORD });
                hasChanges = true;
            }

            if (!context.Settings.Any(s => s.SettingKey == KEY_ADMIN_PASSWORD))
            {
                context.Settings.Add(new SystemSettings { SettingKey = KEY_ADMIN_PASSWORD, SettingValue = DEFAULT_PASSWORD });
                hasChanges = true;
            }

            if (!context.Settings.Any(s => s.SettingKey == KEY_REPORTS_PASSWORD))
            {
                context.Settings.Add(new SystemSettings { SettingKey = KEY_REPORTS_PASSWORD, SettingValue = DEFAULT_PASSWORD });
                hasChanges = true;
            }

            if (hasChanges)
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

        public bool ValidateUserPassword(string password)
        {
            return GetSetting(KEY_USER_PASSWORD) == password;
        }

        public bool ValidateAdminPassword(string password)
        {
            return GetSetting(KEY_ADMIN_PASSWORD) == password;
        }

        public bool ValidateReportsPassword(string password)
        {
            return GetSetting(KEY_REPORTS_PASSWORD) == password;
        }

        public void UpdateUserPassword(string newPassword)
        {
            SetSetting(KEY_USER_PASSWORD, newPassword);
        }

        public void UpdateAdminPassword(string newPassword)
        {
            SetSetting(KEY_ADMIN_PASSWORD, newPassword);
        }

        public void UpdateReportsPassword(string newPassword)
        {
            SetSetting(KEY_REPORTS_PASSWORD, newPassword);
        }

        /// <summary>
        /// Tüm sistemi sıfırla (Users tablosu hariç): ürün, kategori, gider, satış
        /// verilerini siler ve şifreleri varsayılana döndürür.
        /// </summary>
        public void ResetSystem()
        {
            using var context = new BarcodeContext();

            // Veri tablolarını temizle.
            context.Products.RemoveRange(context.Products);
            context.Categories.RemoveRange(context.Categories);
            context.Expenses.RemoveRange(context.Expenses);
            context.SaleDetails.RemoveRange(context.SaleDetails);
            context.Sales.RemoveRange(context.Sales);

            // Şifre ayarlarını varsayılana döndür.
            var userPwd = context.Settings.FirstOrDefault(s => s.SettingKey == KEY_USER_PASSWORD);
            var adminPwd = context.Settings.FirstOrDefault(s => s.SettingKey == KEY_ADMIN_PASSWORD);
            var reportsPwd = context.Settings.FirstOrDefault(s => s.SettingKey == KEY_REPORTS_PASSWORD);

            if (userPwd != null) userPwd.SettingValue = DEFAULT_PASSWORD;
            if (adminPwd != null) adminPwd.SettingValue = DEFAULT_PASSWORD;
            if (reportsPwd != null) reportsPwd.SettingValue = DEFAULT_PASSWORD;

            context.SaveChanges();
        }
    }
}
