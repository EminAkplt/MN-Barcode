using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MN_Barcode.Entities;
using System;
using System.IO;


namespace MN_Barcode.DataAccess
{
    /// <summary>
    /// Veritabanı bağlam (context) sınıfı.
    /// Tüm tablolara (DbSet) buradan erişilir ve bağlantı ayarları burada yapılır.
    /// </summary>
    public class BarcodeContext : DbContext
    {
        // --- TABLOLAR (Her DbSet veritabanında bir tabloya karşılık gelir) ---
        public DbSet<Product> Products { get; set; }          // Ürünler
        public DbSet<Category> Categories { get; set; }       // Kategoriler
        public DbSet<Expense> Expenses { get; set; }          // Giderler
        public DbSet<Sale> Sales { get; set; }                // Satış fişleri
        public DbSet<SaleDetail> SaleDetails { get; set; }    // Fiş satır detayları
        public DbSet<AppUser> Users { get; set; }             // Kullanıcılar
        public DbSet<SystemSettings> Settings { get; set; }   // Sistem ayarları

        // Bağlantı bulunamazsa kullanılacak güvenli varsayılan (yerel SQL Express).
        private const string VarsayilanBaglanti =
            "Data Source=.\\SQLEXPRESS; Initial Catalog=MNBarcodeLocal; Integrated Security=True; TrustServerCertificate=True;";

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Bağlantı cümlesini önce appsettings.json'dan okumayı dene.
            // Böylece farklı bilgisayar/sunucuda kodu değiştirmeden sadece
            // appsettings.json düzenlenerek veritabanı adresi değiştirilebilir.
            string baglanti = BaglantiCumlesiniOku() ?? VarsayilanBaglanti;

            optionsBuilder.UseSqlServer(baglanti);
        }

        /// <summary>
        /// Uygulamanın çalıştığı klasördeki appsettings.json dosyasından
        /// "ConnectionStrings:DefaultConnection" değerini okur.
        /// Dosya yoksa veya değer boşsa null döner (varsayılana düşülür).
        /// </summary>
        private static string? BaglantiCumlesiniOku()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    // Çalışan exe'nin bulunduğu klasörü temel al (çalışma dizininden bağımsız).
                    .SetBasePath(AppContext.BaseDirectory)
                    // optional: true -> dosya yoksa hata verme, sadece geç.
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .Build();

                return config.GetConnectionString("DefaultConnection");
            }
            catch
            {
                // Herhangi bir okuma hatasında uygulamayı kırma, varsayılana düş.
                return null;
            }
        }
    }
}
