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
            string baglanti = BaglantiCumlesiniOku() ?? VarsayilanBaglanti;
            optionsBuilder.UseSqlServer(baglanti);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ──────────────────────────────────────────────────────────────────
            // ENUM → STRING dönüşümleri
            // Veritabanında sütun tipi değişmez (varchar kalır), enum adı string
            // olarak saklanır. Böylece enum'a geçiş mevcut veriyi bozmaz.
            // ──────────────────────────────────────────────────────────────────

            // Sale.PaymentType: "Nakit" / "KrediKarti" / "Iade"
            modelBuilder.Entity<Sale>()
                .Property(s => s.PaymentType)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Sale.SaleType: "Satis" / "Iade"
            modelBuilder.Entity<Sale>()
                .Property(s => s.SaleType)
                .HasConversion<string>()
                .HasMaxLength(10);

            // AppUser.Role: "Admin" / "Kasiyer" / "Viewer"
            modelBuilder.Entity<AppUser>()
                .Property(u => u.Role)
                .HasConversion<string>()
                .HasMaxLength(20);

            // ──────────────────────────────────────────────────────────────────
            // Mevcut Sale kayıtlarında PaymentType kolonu string değer içerebilir
            // ("Nakit", "Kredi Kartı" gibi). HasConversion bunları otomatik map eder.
            // ──────────────────────────────────────────────────────────────────
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
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .Build();

                return config.GetConnectionString("DefaultConnection");
            }
            catch
            {
                return null;
            }
        }
    }
}
