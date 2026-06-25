using MN_Barcode.DataAccess;
using MN_Barcode.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MN_Barcode.Business
{
    /// <summary>
    /// Raporlama iş mantığı servisi (gün sonu özeti vb.).
    /// Her metot kendi kısa ömürlü context'ini kullanır (bkz. CategoryService notu).
    /// </summary>
    public class ReportingService
    {
        /// <summary>Gün sonu (X raporu) özet veri modeli.</summary>
        public class DailySummary
        {
            public DateTime Date { get; set; }
            public decimal TotalRevenue { get; set; }       // Toplam ciro
            public int TotalSalesCount { get; set; }        // Satış (fiş) adedi
            public decimal CashTotal { get; set; }          // Nakit toplam
            public decimal CardTotal { get; set; }          // Kart toplam
            public int TotalProductsSold { get; set; }      // Satılan ürün adedi
            public decimal TotalExpense { get; set; }       // Toplam Gider
            public decimal NetProfit { get; set; }          // Net Kâr (Ciro - Gider)
        }

        // GÜN SONU / X RAPORU ÖZET VERİSİ
        public DailySummary GetDailySummary(DateTime date)
        {
            using var context = new BarcodeContext();

            // İlgili günün başı (00:00) ve ertesi günün başı (hariç) — aralık karşılaştırması.
            DateTime gunBasi = date.Date;
            DateTime ertesiGun = gunBasi.AddDays(1);

            // 1. Satışlar — filtre veritabanında uygulanır, sadece o günün satışları gelir.
            var sales = context.Sales
                .Where(x => x.CreatedDate >= gunBasi && x.CreatedDate < ertesiGun && x.TotalAmount > 0)
                .ToList();

            // 2. Giderler — toplamı doğrudan veritabanında hesaplanır.
            var expenses = context.Expenses
                .Where(x => x.ExpenseDate >= gunBasi && x.ExpenseDate < ertesiGun)
                .Sum(x => (decimal?)x.Amount) ?? 0m; // Kayıt yoksa 0

            var totalRev = sales.Sum(x => x.TotalAmount);

            // Hiç hareket yoksa boş özet dön.
            if (!sales.Any() && expenses == 0) return new DailySummary { Date = date };

            // O güne ait satış fişlerinin id'leri üzerinden satılan toplam adedi hesapla.
            var detailIds = sales.Select(s => s.Id).ToList();
            var totalQty = context.SaleDetails
                .Where(x => detailIds.Contains(x.SaleId))
                .Sum(x => (double?)x.Quantity) ?? 0d; // Kayıt yoksa 0

            return new DailySummary
            {
                Date = date,
                TotalRevenue = totalRev,
                TotalSalesCount = sales.Count,
                CashTotal = sales.Where(x => x.PaymentType == "Nakit").Sum(x => x.TotalAmount),
                CardTotal = sales.Where(x => x.PaymentType == "Kredi Kartı").Sum(x => x.TotalAmount),
                TotalProductsSold = (int)totalQty,
                TotalExpense = expenses,
                NetProfit = totalRev - expenses // Basit Net Kâr (Alış fiyatı hesaba katılmadı, sadece ciro - gider)
            };
        }

        // DETAY RAPOR VERİSİ (Ürün Bazlı)
        public List<TopSellingProduct> GetDailyProductDetails(DateTime date)
        {
            using var context = new BarcodeContext();

            // O güne ait satış fişlerinin id'lerini veritabanında filtreleyerek al.
            DateTime gunBasi = date.Date;
            DateTime ertesiGun = gunBasi.AddDays(1);

            var sales = context.Sales
                .Where(x => x.CreatedDate >= gunBasi && x.CreatedDate < ertesiGun && x.TotalAmount > 0)
                .Select(x => x.Id)
                .ToList();

            // Bu fişlere ait detay satırlarını veritabanında filtrele, sonra bellekte grupla.
            var rawData = context.SaleDetails
                .Where(x => sales.Contains(x.SaleId))
                .ToList();

            var grouped = rawData
                .GroupBy(x => x.ProductId)
                .Select(g => new TopSellingProduct
                {
                    ProductId = g.Key,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.TotalPrice)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToList();

            // Ürün isimlerini doldur.
            foreach (var item in grouped)
            {
                var p = context.Products.Find(item.ProductId);
                if (p != null)
                {
                    item.ProductName = p.Name;
                    item.Barcode = p.Barcode;
                }
            }

            return grouped;
        }
    }
}
