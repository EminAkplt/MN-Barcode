using MN_Barcode.DataAccess;
using MN_Barcode.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MN_Barcode.Business
{
    public class ReportingService
    {
        private BarcodeContext _context;

        public ReportingService()
        {
            _context = new BarcodeContext();
        }

        public class DailySummary
        {
            public DateTime Date { get; set; }
            public decimal TotalRevenue { get; set; }
            public int TotalSalesCount { get; set; }
            public decimal CashTotal { get; set; }
            public decimal CardTotal { get; set; }
            public int TotalProductsSold { get; set; }
            public decimal TotalExpense { get; set; } // Toplam Gider
            public decimal NetProfit { get; set; } // Net Kâr (Ciro - Gider)
        }

        // GÜN SONU / X RAPORU ÖZET VERİSİ
        public DailySummary GetDailySummary(DateTime date)
        {
            // 1. Satışlar
            var sales = _context.Sales
                .ToList() // Client-side
                .Where(x => x.CreatedDate.HasValue && x.CreatedDate.Value.Date == date.Date && x.TotalAmount > 0)
                .ToList();

            // 2. Giderler
            var expenses = _context.Expenses
                .ToList()
                .Where(x => x.ExpenseDate.Date == date.Date)
                .Sum(x => x.Amount);

            var totalRev = sales.Sum(x => x.TotalAmount);

            if (!sales.Any() && expenses == 0) return new DailySummary { Date = date };

            var detailIds = sales.Select(s => s.Id).ToList();
            var totalQty = _context.SaleDetails.ToList()
                .Where(x => detailIds.Contains(x.SaleId))
                .Sum(x => x.Quantity);

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
            var sales = _context.Sales
                .ToList()
                .Where(x => x.CreatedDate.HasValue && x.CreatedDate.Value.Date == date.Date && x.TotalAmount > 0)
                .Select(x => x.Id)
                .ToList();

            var rawData = _context.SaleDetails
                .ToList()
                .Where(x => sales.Contains(x.SaleId));

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

            // Ürün isimlerini doldur
            foreach (var item in grouped)
            {
                var p = _context.Products.Find(item.ProductId);
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
