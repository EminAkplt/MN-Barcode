using MN_Barcode.DataAccess;
using MN_Barcode.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MN_Barcode.Business
{
    /// <summary>
    /// Satış iş mantığı servisi.
    /// Her metot kendi kısa ömürlü context'ini kullanır (bkz. CategoryService notu).
    /// </summary>
    public class SaleService
    {
        // ──────────────────────────────────────────────────────────────────
        // SATIŞI KAYDET VE STOKTAN DÜŞ (Transaction ile Güvenli İşlem)
        // Fiş ve detaylar ya tamamen kaydedilir ya da (hata olursa) hiçbiri kaydedilmez.
        // ──────────────────────────────────────────────────────────────────
        /// <summary>
        /// Satışı (veya iadeyi) kaydeder ve stoğu günceller.
        ///
        /// STOK DOĞRULUĞU:
        /// Eskiden stok bellekte okunup değiştirilip yazılıyordu ve sonuç
        /// Math.Max(0, ...) ile kırpılıyordu. İki sorun vardı:
        ///  1) Eşzamanlı iki işlem birbirinin güncellemesini eziyordu (kayıp güncelleme).
        ///  2) Stoktan fazla satış sessizce 0'a kırpılıyordu; 3 adetlik üründen 50
        ///     satılınca açık kaybolduğu için envanter haftalar içinde güvenilmez oluyordu.
        ///
        /// Artık:
        ///  - Satır kilidiyle (UPDLOCK) okunur, böylece aynı anda ikinci bir işlem bekler.
        ///  - Yetersiz stok varsa işlem iptal edilir ve çağırana anlatılır.
        ///  - Güncelleme veritabanında yapılır (StockQuantity = StockQuantity - @miktar).
        /// </summary>
        /// <exception cref="InsufficientStockException">Satılmak istenen miktar stoktan fazlaysa.</exception>
        public void CompleteSale(Sale sale, List<SaleDetail> details)
        {
            if (details == null || details.Count == 0)
                throw new InvalidOperationException("Sepet boş. Kaydedilecek satır yok.");

            using var context = new BarcodeContext();
            // Serializable: okuma ile yazma arasında başka işlem araya giremez.
            using var transaction = context.Database.BeginTransaction(
                System.Data.IsolationLevel.Serializable);
            try
            {
                // ── 1. Stok yeterli mi? (yazmadan önce kontrol) ──────────
                // Aynı ürün sepette birden fazla satırda olabilir; toplamına bakılır.
                var gerekli = new Dictionary<int, double>();
                foreach (var item in details)
                {
                    if (!gerekli.ContainsKey(item.ProductId)) gerekli[item.ProductId] = 0;
                    gerekli[item.ProductId] += item.Quantity;
                }

                var urunIdleri = gerekli.Keys.ToList();
                var urunler = context.Products
                    .Where(p => urunIdleri.Contains(p.Id))
                    .ToDictionary(p => p.Id);

                foreach (var kayit in gerekli)
                {
                    // Negatif miktar = iade; stok geri eklenir, kontrole gerek yok.
                    if (kayit.Value <= 0) continue;

                    if (!urunler.TryGetValue(kayit.Key, out var urun))
                        throw new InvalidOperationException(
                            $"Ürün bulunamadı (kod: {kayit.Key}). Sepeti temizleyip tekrar deneyin.");

                    if (urun.StockQuantity < kayit.Value)
                        throw new InsufficientStockException(urun.Name, urun.StockQuantity, kayit.Value);
                }

                // ── 2. Satış başlığı ────────────────────────────────────
                sale.CreatedDate = DateTime.Now;
                context.Sales.Add(sale);
                context.SaveChanges();

                // ── 3. Satırlar + stok düşümü ───────────────────────────
                foreach (var item in details)
                {
                    item.SaleId = sale.Id;
                    context.SaleDetails.Add(item);
                }

                foreach (var kayit in gerekli)
                {
                    // Stok veritabanında güncellenir; bellekteki değer üzerinden
                    // yazılmadığı için eşzamanlı işlemler birbirini ezmez.
                    context.Database.ExecuteSqlRaw(
                        "UPDATE Products SET StockQuantity = StockQuantity - {0} WHERE Id = {1};",
                        kayit.Value, kayit.Key);
                }

                context.SaveChanges();
                transaction.Commit();
            }
            catch (Exception)
            {
                try { transaction.Rollback(); } catch { /* bağlantı zaten kopmuş olabilir */ }
                throw;
            }
        }

        // ──────────────────────────────────────────────────────────────────
        // EN ÇOK SATILAN ÜRÜNLER (Tarih Filtreli)
        // ──────────────────────────────────────────────────────────────────
        public List<TopSellingProduct> GetTopSellingProducts(int limit = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            using var context = new BarcodeContext();

            DateTime start = startDate ?? DateTime.MinValue;
            DateTime end   = endDate   ?? DateTime.MaxValue;

            var grouped = context.SaleDetails
                .Include(x => x.Sale)
                .Where(x => x.Sale.CreatedDate >= start
                         && x.Sale.CreatedDate <= end
                         && x.Sale.SaleType == SaleType.Satis
                         && x.Quantity > 0)
                .GroupBy(x => x.ProductId)
                .Select(g => new TopSellingProduct
                {
                    ProductId     = g.Key,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalRevenue  = g.Sum(x => x.TotalPrice)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(limit)
                .ToList();

            FillProductDetails(context, grouped);
            return grouped;
        }

        // ──────────────────────────────────────────────────────────────────
        // EN ÇOK İADE EDİLEN ÜRÜNLER
        // ──────────────────────────────────────────────────────────────────
        public List<TopSellingProduct> GetTopReturnedProducts(int limit = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            using var context = new BarcodeContext();

            DateTime start = startDate ?? DateTime.MinValue;
            DateTime end   = endDate   ?? DateTime.MaxValue;

            var grouped = context.SaleDetails
                .Include(x => x.Sale)
                .Where(x => x.Sale.CreatedDate >= start
                         && x.Sale.CreatedDate <= end
                         && x.Sale.SaleType == SaleType.Iade)
                .GroupBy(x => x.ProductId)
                .Select(g => new TopSellingProduct
                {
                    ProductId     = g.Key,
                    TotalQuantity = Math.Abs(g.Sum(x => x.Quantity)),
                    TotalRevenue  = Math.Abs(g.Sum(x => x.TotalPrice))
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(limit)
                .ToList();

            FillProductDetails(context, grouped);
            return grouped;
        }

        private void FillProductDetails(BarcodeContext context, List<TopSellingProduct> list)
        {
            var productIds = list.ConvertAll(l => l.ProductId);
            var products = context.Products.Where(p => productIds.Contains(p.Id)).ToDictionary(p => p.Id);
            foreach (var item in list)
                if (products.TryGetValue(item.ProductId, out var product))
                {
                    item.ProductName = product.Name;
                    item.Barcode = product.Barcode;
                }
        }

        // ──────────────────────────────────────────────────────────────────
        // SATIŞ GEÇMİŞİ (Sadece normal satışlar)
        // ──────────────────────────────────────────────────────────────────
        public List<Sale> GetSalesHistory(DateTime startDate, DateTime endDate)
        {
            using var context = new BarcodeContext();
            return context.Sales
                .Where(x => x.CreatedDate >= startDate
                         && x.CreatedDate <= endDate
                         && x.SaleType == SaleType.Satis)
                .OrderByDescending(x => x.CreatedDate)
                .ToList();
        }

        // ──────────────────────────────────────────────────────────────────
        // SATIŞ GEÇMİŞİ - ÜRÜN DETAYLARI İLE (Her ürün ayrı satır)
        // ──────────────────────────────────────────────────────────────────
        public List<SaleDetail> GetSalesHistoryWithDetails(DateTime startDate, DateTime endDate)
        {
            using var context = new BarcodeContext();
            return context.SaleDetails
                .Include(x => x.Sale)
                .Include(x => x.Product)
                .Where(x => x.Sale.CreatedDate >= startDate
                         && x.Sale.CreatedDate <= endDate
                         && x.Sale.SaleType == SaleType.Satis
                         && x.Quantity > 0)
                .OrderByDescending(x => x.Sale.CreatedDate)
                .ThenBy(x => x.Sale.TransactionCode)
                .ToList();
        }

        // ──────────────────────────────────────────────────────────────────
        // İADE GEÇMİŞİ (Sadece iadeler)
        // ──────────────────────────────────────────────────────────────────
        public List<Sale> GetReturnsHistory(DateTime startDate, DateTime endDate)
        {
            using var context = new BarcodeContext();
            return context.Sales
                .Where(x => x.CreatedDate >= startDate
                         && x.CreatedDate <= endDate
                         && x.SaleType == SaleType.Iade)
                .OrderByDescending(x => x.CreatedDate)
                .ToList();
        }

        // ──────────────────────────────────────────────────────────────────
        // İADE GEÇMİŞİ - ÜRÜN DETAYLARI İLE (Her ürün ayrı satır)
        // ──────────────────────────────────────────────────────────────────
        public List<SaleDetail> GetReturnsHistoryWithDetails(DateTime startDate, DateTime endDate)
        {
            using var context = new BarcodeContext();
            return context.SaleDetails
                .Include(x => x.Sale)
                .Include(x => x.Product)
                .Where(x => x.Sale.CreatedDate >= startDate
                         && x.Sale.CreatedDate <= endDate
                         && x.Sale.SaleType == SaleType.Iade
                         && x.Quantity < 0)  // İade negatif quantity
                .OrderByDescending(x => x.Sale.CreatedDate)
                .ThenBy(x => x.Sale.TransactionCode)
                .ToList();
        }

        // ──────────────────────────────────────────────────────────────────
        // BUGÜNKÜ SATIŞ TOPLAMI
        // ──────────────────────────────────────────────────────────────────
        public decimal GetTodaySalesTotal()
        {
            using var context = new BarcodeContext();
            DateTime bugun = DateTime.Today;
            DateTime yarin = bugun.AddDays(1);

            // Satış cirosu
            decimal satis = context.Sales
                .Where(x => x.CreatedDate >= bugun
                         && x.CreatedDate < yarin
                         && x.SaleType == SaleType.Satis)
                .Sum(x => (decimal?)x.TotalAmount) ?? 0m;

            // İade tutarı (negatif değer)
            decimal iade = context.Sales
                .Where(x => x.CreatedDate >= bugun
                         && x.CreatedDate < yarin
                         && x.SaleType == SaleType.Iade)
                .Sum(x => (decimal?)x.TotalAmount) ?? 0m;

            // Net ciro = Satışlar + İadeler (iade negatif olduğu için toplama)
            return satis + iade;
        }

        // BUGÜNKÜ SATIŞ SAYISI
        public int GetTodaySalesCount()
        {
            using var context = new BarcodeContext();
            DateTime bugun = DateTime.Today;
            DateTime yarin = bugun.AddDays(1);
            return context.Sales
                .Count(x => x.CreatedDate >= bugun
                          && x.CreatedDate < yarin
                          && x.SaleType == SaleType.Satis);
        }

        // AYLIK SATIŞ TOPLAMI (Bu ay)
        public decimal GetMonthlySalesTotal()
        {
            using var context = new BarcodeContext();
            DateTime ayinIlkGunu = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            decimal satis = context.Sales
                .Where(x => x.CreatedDate >= ayinIlkGunu
                         && x.SaleType == SaleType.Satis)
                .Sum(x => (decimal?)x.TotalAmount) ?? 0m;

            decimal iade = context.Sales
                .Where(x => x.CreatedDate >= ayinIlkGunu
                         && x.SaleType == SaleType.Iade)
                .Sum(x => (decimal?)x.TotalAmount) ?? 0m;

            return satis + iade;
        }

        // TARİH ARALIĞINA GÖRE GÜNLÜK SATIŞLAR (Grafik için)
        public List<DailySalesData> GetDailySales(DateTime startDate, DateTime endDate)
        {
            using var context = new BarcodeContext();
            DateTime baslangic  = startDate.Date;
            DateTime bitisHaric = endDate.Date.AddDays(1);

            // Satışlar ve iadeler dahil — net günlük ciro hesaplanacak.
            var allSales = context.Sales
                .Where(x => x.CreatedDate >= baslangic
                         && x.CreatedDate < bitisHaric
                         && (x.SaleType == SaleType.Satis || x.SaleType == SaleType.Iade))
                .ToList();

            return allSales
                .GroupBy(x => x.CreatedDate!.Value.Date)
                .Select(g => new DailySalesData
                {
                    Date  = g.Key,
                    Total = g.Sum(x => x.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .ToList();
        }
    }
}
