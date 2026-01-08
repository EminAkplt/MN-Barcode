using MN_Barcode.DataAccess;
using MN_Barcode.Entities;
using System.Linq;

namespace MN_Barcode.Business
{
    public class SettingsService
    {
        private BarcodeContext _context;

        // Default şifreler
        public const string DEFAULT_PASSWORD = "123";
        public const string KEY_USER_PASSWORD = "UserPassword";
        public const string KEY_ADMIN_PASSWORD = "AdminPassword";
        public const string KEY_REPORTS_PASSWORD = "ReportsPassword";

        public SettingsService()
        {
            _context = new BarcodeContext();
            EnsureDefaultSettings();
        }

        /// <summary>
        /// Default ayarları oluştur (ilk kurulum için)
        /// </summary>
        private void EnsureDefaultSettings()
        {
            bool hasChanges = false;

            if (!_context.Settings.Any(s => s.SettingKey == KEY_USER_PASSWORD))
            {
                _context.Settings.Add(new SystemSettings { SettingKey = KEY_USER_PASSWORD, SettingValue = DEFAULT_PASSWORD });
                hasChanges = true;
            }

            if (!_context.Settings.Any(s => s.SettingKey == KEY_ADMIN_PASSWORD))
            {
                _context.Settings.Add(new SystemSettings { SettingKey = KEY_ADMIN_PASSWORD, SettingValue = DEFAULT_PASSWORD });
                hasChanges = true;
            }

            if (!_context.Settings.Any(s => s.SettingKey == KEY_REPORTS_PASSWORD))
            {
                _context.Settings.Add(new SystemSettings { SettingKey = KEY_REPORTS_PASSWORD, SettingValue = DEFAULT_PASSWORD });
                hasChanges = true;
            }

            if (hasChanges)
            {
                _context.SaveChanges();
            }
        }

        public string GetSetting(string key)
        {
            var setting = _context.Settings.FirstOrDefault(s => s.SettingKey == key);
            return setting?.SettingValue ?? "";
        }

        public void SetSetting(string key, string value)
        {
            var setting = _context.Settings.FirstOrDefault(s => s.SettingKey == key);
            if (setting != null)
            {
                setting.SettingValue = value;
            }
            else
            {
                _context.Settings.Add(new SystemSettings { SettingKey = key, SettingValue = value });
            }
            _context.SaveChanges();
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
        /// Tüm sistemi sıfırla (Users tablosu hariç)
        /// </summary>
        public void ResetSystem()
        {
            // Products
            _context.Products.RemoveRange(_context.Products.ToList());
            
            // Categories
            _context.Categories.RemoveRange(_context.Categories.ToList());
            
            // Expenses
            _context.Expenses.RemoveRange(_context.Expenses.ToList());
            
            // Sales & SaleDetails
            _context.SaleDetails.RemoveRange(_context.SaleDetails.ToList());
            _context.Sales.RemoveRange(_context.Sales.ToList());
            
            // Settings - şifreleri resetle
            var userPwd = _context.Settings.FirstOrDefault(s => s.SettingKey == KEY_USER_PASSWORD);
            var adminPwd = _context.Settings.FirstOrDefault(s => s.SettingKey == KEY_ADMIN_PASSWORD);
            var reportsPwd = _context.Settings.FirstOrDefault(s => s.SettingKey == KEY_REPORTS_PASSWORD);
            
            if (userPwd != null) userPwd.SettingValue = DEFAULT_PASSWORD;
            if (adminPwd != null) adminPwd.SettingValue = DEFAULT_PASSWORD;
            if (reportsPwd != null) reportsPwd.SettingValue = DEFAULT_PASSWORD;
            
            _context.SaveChanges();
        }
    }
}
