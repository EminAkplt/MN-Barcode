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
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleDetail> SaleDetails { get; set; }
        public DbSet<AppUser> Users { get; set; }
        public DbSet<SystemSettings> Settings { get; set; }

        private const string VarsayilanBaglanti =
            "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=MNBarcodeLocal;Integrated Security=true;";

        private static string? _cachedConnectionString;

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

        private static string? BaglantiCumlesiniOku()
        {
            if (_cachedConnectionString != null)
                return _cachedConnectionString;

            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .Build();

                _cachedConnectionString = config.GetConnectionString("DefaultConnection");
                return _cachedConnectionString;
            }
            catch
            {
                return null;
            }
        }
    }
}
