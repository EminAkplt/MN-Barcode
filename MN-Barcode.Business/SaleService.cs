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

                    // İade ise Quantity negatif → stok artar; satış ise azalır.
                    var product = context.Products.Find(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity -= item.Quantity;
                    }
                }

                context.SaveChanges();
                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
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

            // SaleType.Satis ve pozitif Quantity = normal satış satırı
            var rawData = context.SaleDetails
                .Include(x => x.Sale)
                .Where(x => x.Sale.CreatedDate >= start
                         && x.Sale.CreatedDate <= end
                         && x.Sale.SaleType == SaleType.Satis
                         && x.Quantity > 0)
                .ToList();

            var grouped = rawData
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

            // SaleType.Iade = iade fişi satırları
            var rawData = context.SaleDetails
                .Include(x => x.Sale)
                .Where(x => x.Sale.CreatedDate >= start
                         && x.Sale.CreatedDate <= end
                         && x.Sale.SaleType == SaleType.Iade)
                .ToList();

            var grouped = rawData
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

        // Gruplanmış sonuçlara ürün adı/barkod bilgisini doldurur.
        private void FillProductDetails(BarcodeContext context, List<TopSellingProduct> list)
        {
            foreach (var item in list)
            {
                var product = context.Products.Find(item.ProductId);
                if (product != null)
                {
                    item.ProductName = product.Name;
                    item.Barcode     = product.Barcode;
                }
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
        // BUGÜNKÜ SATIŞ TOPLAMI
        // ──────────────────────────────────────────────────────────────────
        public decimal GetTodaySalesTotal()
        {
            using var context = new BarcodeContext();
            DateTime bugun = DateTime.Today;
            DateTime yarin = bugun.AddDays(1);
            return context.Sales
                .Where(x => x.CreatedDate >= bugun
                         && x.CreatedDate < yarin
                         && x.SaleType == SaleType.Satis)
                .Sum(x => (decimal?)x.TotalAmount) ?? 0m;
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
            return context.Sales
                .Where(x => x.CreatedDate >= ayinIlkGunu
                         && x.SaleType == SaleType.Satis)
                .Sum(x => (decimal?)x.TotalAmount) ?? 0m;
        }

        // TARİH ARALIĞINA GÖRE GÜNLÜK SATIŞLAR (Grafik için)
        public List<DailySalesData> GetDailySales(DateTime startDate, DateTime endDate)
        {
            using var context = new BarcodeContext();
            DateTime baslangic  = startDate.Date;
            DateTime bitisHaric = endDate.Date.AddDays(1);

            return context.Sales
                .Where(x => x.CreatedDate >= baslangic
                         && x.CreatedDate < bitisHaric
                         && x.SaleType == SaleType.Satis)
                .ToList()
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
