using System;
using System.Linq;
using System.Windows.Forms;
using MN_Barcode.DataAccess;
using MN_Barcode.Entities;

namespace MN_Barcode.WinForms
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            InitializeDatabase();
            Application.Run(new LoginForm());
        }

        private static void InitializeDatabase()
        {
            try
            {
                using var context = new BarcodeContext();
                context.Database.EnsureCreated();
                if (!context.Users.Any())
                {
                    context.Users.Add(new AppUser { Username = "admin", Password = "admin123", Role = UserRole.Admin });
                    context.SaveChanges();
                }
            }
            catch { }
        }
    }
}
