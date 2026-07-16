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
                sale.CreatedDate = DateTime.Now;
                context.Sales.Add(sale);
                context.SaveChanges();

                var productIds = details.ConvertAll(d => d.ProductId);
                var products = context.Products.Where(p => productIds.Contains(p.Id)).ToDictionary(p => p.Id);

                foreach (var item in details)
                {
                    item.SaleId = sale.Id;
                    context.SaleDetails.Add(item);
                    if (products.TryGetValue(item.ProductId, out var product))
                    {
                        // Stok hiçbir zaman eksiye düşmesin: satışta stoktan düş,
                        // iadede (Quantity negatif) geri ekle; alt sınır 0.
                        product.StockQuantity = System.Math.Max(0, product.StockQuantity - item.Quantity);
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
