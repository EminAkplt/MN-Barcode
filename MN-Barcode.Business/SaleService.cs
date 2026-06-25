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
        // SATIŞI KAYDET VE STOKTAN DÜŞ (Transaction ile Güvenli İşlem)
        // Fiş ve detaylar ya tamamen kaydedilir ya da (hata olursa) hiçbiri kaydedilmez.
        public void CompleteSale(Sale sale, List<SaleDetail> details)
        {
            using var context = new BarcodeContext();
            using var transaction = context.Database.BeginTransaction();
            try
            {
                // 1. FİŞİ KAYDET
                sale.CreatedDate = DateTime.Now;
                context.Sales.Add(sale);
                context.SaveChanges(); // sale.Id burada üretilir

                // 2. DETAYLARI EKLE VE STOKTAN DÜŞ
                foreach (var item in details)
                {
                    item.SaleId = sale.Id;
                    context.SaleDetails.Add(item);

                    // İlgili ürünün stoğunu güncelle (iade ise Quantity negatif olduğundan stok artar).
                    var product = context.Products.Find(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity -= item.Quantity;
                    }
                }

                context.SaveChanges();
                transaction.Commit(); // Her şey başarılı -> kalıcı yap
            }
            catch (Exception)
            {
                transaction.Rollback(); // Hata -> hiçbir şeyi kaydetme
                throw;
            }
        }

        // EN ÇOK SATILAN ÜRÜNLER (Tarih Filtreli)
        public List<TopSellingProduct> GetTopSellingProducts(int limit = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            using var context = new BarcodeContext();

            DateTime start = startDate ?? DateTime.MinValue;
            DateTime end = endDate ?? DateTime.MaxValue;

            // FİLTRELEMEYİ VERİTABANINA YAPTIRIYORUZ (Where, ToList'TEN ÖNCE).
            // Böylece tüm tabloyu RAM'e çekmek yerine sadece tarih aralığındaki
            // ve satış (Quantity > 0) olan satırlar gelir. Büyük veride hayati fark.
            var rawData = context.SaleDetails
                .Include(x => x.Sale)
                .Where(x => x.Sale.CreatedDate >= start && x.Sale.CreatedDate <= end && x.Quantity > 0)
                .ToList();

            // Gruplama (ürün bazında topla) bellekte yapılır; veri zaten küçülmüş durumda.
            var grouped = rawData
                .GroupBy(x => x.ProductId)
                .Select(g => new TopSellingProduct
                {
                    ProductId = g.Key,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.TotalPrice)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(limit)
                .ToList();

            FillProductDetails(context, grouped);
            return grouped;
        }

        // EN ÇOK İADE EDİLEN ÜRÜNLER
        public List<TopSellingProduct> GetTopReturnedProducts(int limit = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            using var context = new BarcodeContext();

            DateTime start = startDate ?? DateTime.MinValue;
            DateTime end = endDate ?? DateTime.MaxValue;

            // Filtre veritabanında çalışır; sadece iade satırları (Quantity < 0) gelir.
            var rawData = context.SaleDetails
                .Include(x => x.Sale)
                .Where(x => x.Sale.CreatedDate >= start && x.Sale.CreatedDate <= end && x.Quantity < 0) // Negatif miktar = İade
                .ToList();

            var grouped = rawData
                .GroupBy(x => x.ProductId)
                .Select(g => new TopSellingProduct
                {
                    ProductId = g.Key,
                    TotalQuantity = Math.Abs(g.Sum(x => x.Quantity)), // Pozitif göster
                    TotalRevenue = Math.Abs(g.Sum(x => x.TotalPrice))
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(limit)
                .ToList();

            FillProductDetails(context, grouped);
            return grouped;
        }

        // Gruplanmış sonuçlara ürün adı/barkod bilgisini doldurur.
        // (Aynı context'i kullanır ki ekstra bağlantı açılmasın.)
        private void FillProductDetails(BarcodeContext context, List<TopSellingProduct> list)
        {
            foreach (var item in list)
            {
                var product = context.Products.Find(item.ProductId);
                if (product != null)
                {
                    item.ProductName = product.Name;
                    item.Barcode = product.Barcode;
                }
            }
        }

        // SATIŞ GEÇMİŞİ (Sadece normal satışlar)
        public List<Sale> GetSalesHistory(DateTime startDate, DateTime endDate)
        {
            using var context = new BarcodeContext();
            // Filtre veritabanında: tüm tabloyu çekmeden sadece ilgili kayıtlar gelir.
            return context.Sales
                .Where(x => x.CreatedDate >= startDate && x.CreatedDate <= endDate && x.TotalAmount >= 0)
                .OrderByDescending(x => x.CreatedDate)
                .ToList();
        }

        // İADE GEÇMİŞİ (Sadece iadeler)
        public List<Sale> GetReturnsHistory(DateTime startDate, DateTime endDate)
        {
            using var context = new BarcodeContext();
            // TotalAmount < 0 olan fişler iadedir; filtre veritabanında uygulanır.
            return context.Sales
                .Where(x => x.CreatedDate >= startDate && x.CreatedDate <= endDate && x.TotalAmount < 0)
                .OrderByDescending(x => x.CreatedDate)
                .ToList();
        }

        // BUGÜNKÜ SATIŞ TOPLAMI
        public decimal GetTodaySalesTotal()
        {
            using var context = new BarcodeContext();

            // Bugünün başı (00:00) ile yarının başı arasını sorgula.
            // ".Date" yerine aralık karşılaştırması kullanmak veritabanı dostudur (index kullanır).
            DateTime bugun = DateTime.Today;
            DateTime yarin = bugun.AddDays(1);

            // Sum doğrudan veritabanında çalışır; satırlar belleğe çekilmez.
            return context.Sales
                .Where(x => x.CreatedDate >= bugun && x.CreatedDate < yarin && x.TotalAmount > 0)
                .Sum(x => (decimal?)x.TotalAmount) ?? 0m; // Kayıt yoksa null -> 0
        }

        // BUGÜNKÜ SATIŞ SAYISI
        public int GetTodaySalesCount()
        {
            using var context = new BarcodeContext();

            DateTime bugun = DateTime.Today;
            DateTime yarin = bugun.AddDays(1);

            // Count veritabanında çalışır.
            return context.Sales
                .Count(x => x.CreatedDate >= bugun && x.CreatedDate < yarin && x.TotalAmount > 0);
        }

        // AYLIK SATIŞ TOPLAMI (Bu ay)
        public decimal GetMonthlySalesTotal()
        {
            using var context = new BarcodeContext();

            // Ayın ilk günü (örn. 01.06.2026 00:00).
            DateTime ayinIlkGunu = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            return context.Sales
                .Where(x => x.CreatedDate >= ayinIlkGunu && x.TotalAmount > 0)
                .Sum(x => (decimal?)x.TotalAmount) ?? 0m;
        }

        // TARİH ARALIĞINA GÖRE GÜNLÜK SATIŞLAR (Grafik için)
        public List<DailySalesData> GetDailySales(DateTime startDate, DateTime endDate)
        {
            using var context = new BarcodeContext();

            // Aralığı gün bazına normalize et: başlangıç günü 00:00, bitiş gününün sonu (ertesi gün 00:00).
            DateTime baslangic = startDate.Date;
            DateTime bitisHaric = endDate.Date.AddDays(1); // bitiş gününü de kapsasın diye ertesi gün (hariç)

            // Önce veritabanında filtrele (sadece aralıktaki satış kayıtları),
            // sonra gün bazında gruplamayı bellekte yap.
            return context.Sales
                .Where(x => x.CreatedDate >= baslangic && x.CreatedDate < bitisHaric && x.TotalAmount > 0)
                .ToList()
                .GroupBy(x => x.CreatedDate.Value.Date) // Yukarıdaki filtre null'ları zaten eler
                .Select(g => new DailySalesData
                {
                    Date = g.Key,
                    Total = g.Sum(x => x.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .ToList();
        }
    }
}
