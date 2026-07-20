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

            // ──────────────────────────────────────────────────────────────────
            // PARA SÜTUNLARI — hassasiyet açıkça belirtilir
            //
            // Belirtilmezse EF uyarı veriyor ("values will be silently truncated").
            // Varsayılan decimal(18,2) ile aynı sonucu üretir; buradaki amaç
            // ileride biri modeli değiştirdiğinde para alanlarının sessizce
            // kırpılmaya başlamasını engellemek. Kasada kuruş kaybı kabul edilemez.
            // ──────────────────────────────────────────────────────────────────
            modelBuilder.Entity<Product>().Property(p => p.BuyingPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().Property(p => p.SellingPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Sale>().Property(s => s.TotalAmount).HasPrecision(18, 2);
            modelBuilder.Entity<SaleDetail>().Property(d => d.SellingPrice).HasPrecision(18, 2);
            modelBuilder.Entity<SaleDetail>().Property(d => d.TotalPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Expense>().Property(e => e.Amount).HasPrecision(18, 2);

            // ──────────────────────────────────────────────────────────────────
            // İNDEKSLER — kasadaki hızın ve veri doğruluğunun temeli
            // ──────────────────────────────────────────────────────────────────

            // Barkod: her okutmada sorgulanır. İndekssiz her okutma tam tablo
            // taramasıydı; 10-20 bin ürünlü bir katalogda mekanik diskte belirgin
            // gecikme demek. Ayrıca TEKİL: aynı barkod iki üründe olabiliyordu ve
            // FirstOrDefault rastgele birini döndürdüğü için YANLIŞ ÜRÜN satılıyordu.
            // Filtre: barkodsuz ürünler (NULL) tekillik kuralının dışında tutulur,
            // aksi halde ikinci barkodsuz ürün kaydedilemezdi.
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Barcode)
                .IsUnique()
                .HasFilter("[Barcode] IS NOT NULL");

            // Satış tarihi: Ana Sayfa, Satış Geçmişi, İadeler ve tüm raporlar bu
            // sütuna göre süzüyor. İndekssiz her ekran açılışı tam tablo taramasıydı.
            modelBuilder.Entity<Sale>()
                .HasIndex(s => s.CreatedDate);

            // Fiş numarası: geçmiş ve iade ekranları buna göre gruplar.
            modelBuilder.Entity<Sale>()
                .HasIndex(s => s.TransactionCode);

            // Gider tarihi: gider listesi ve günlük özet bu sütuna göre süzer.
            modelBuilder.Entity<Expense>()
                .HasIndex(e => e.ExpenseDate);
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
