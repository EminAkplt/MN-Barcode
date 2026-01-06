using MN_Barcode.DataAccess;
using MN_Barcode.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MN_Barcode.Business
{
    public class SaleService
    {
        private BarcodeContext _context;

        public SaleService()
        {
            _context = new BarcodeContext();
        }

        // SATIŞI KAYDET VE STOKTAN DÜŞ (Transaction ile Güvenli İşlem)
        public void CompleteSale(Sale sale, List<SaleDetail> details)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. FİŞİ KAYDET
                    sale.CreatedDate = DateTime.Now;
                    _context.Sales.Add(sale);
                    _context.SaveChanges(); 

                    // 2. DETAYLARI EKLE VE STOKTAN DÜŞ
                    foreach (var item in details)
                    {
                        item.SaleId = sale.Id;
                        _context.SaleDetails.Add(item);

                        var product = _context.Products.Find(item.ProductId);
                        if (product != null)
                        {
                            product.StockQuantity -= item.Quantity;
                        }
                    }

                    _context.SaveChanges(); 
                    transaction.Commit(); 
                }
                catch (Exception ex)
                {
                    transaction.Rollback(); 
                    throw ex; 
                }
            }
        }

        // EN ÇOK SATILAN ÜRÜNLER (Tarih Filtreli)
        public List<TopSellingProduct> GetTopSellingProducts(int limit = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            DateTime start = startDate ?? DateTime.MinValue;
            DateTime end = endDate ?? DateTime.MaxValue;

            var rawData = _context.SaleDetails
                .Include(x => x.Sale)
                .ToList() // Client-side evaluation
                .Where(x => x.Sale.CreatedDate >= start && x.Sale.CreatedDate <= end && x.Quantity > 0);

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

            FillProductDetails(grouped);
            return grouped;
        }

        // EN ÇOK İADE EDİLEN ÜRÜNLER
        public List<TopSellingProduct> GetTopReturnedProducts(int limit = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            DateTime start = startDate ?? DateTime.MinValue;
            DateTime end = endDate ?? DateTime.MaxValue;

            var rawData = _context.SaleDetails
                .Include(x => x.Sale)
                .ToList()
                .Where(x => x.Sale.CreatedDate >= start && x.Sale.CreatedDate <= end && x.Quantity < 0); // Negatif miktar = İade

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

            FillProductDetails(grouped);
            return grouped;
        }

        private void FillProductDetails(List<TopSellingProduct> list)
        {
            foreach (var item in list)
            {
                var product = _context.Products.Find(item.ProductId);
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
            return _context.Sales
                .ToList()
                .Where(x => x.CreatedDate >= startDate && x.CreatedDate <= endDate && x.TotalAmount >= 0)
                .OrderByDescending(x => x.CreatedDate)
                .ToList();
        }

        // İADE GEÇMİŞİ (Sadece iadeler)
        public List<Sale> GetReturnsHistory(DateTime startDate, DateTime endDate)
        {
            return _context.Sales
                .ToList()
                .Where(x => x.CreatedDate >= startDate && x.CreatedDate <= endDate && x.TotalAmount < 0)
                .OrderByDescending(x => x.CreatedDate)
                .ToList();
        }

        // BUGÜNKÜ SATIŞ TOPLAMI
        public decimal GetTodaySalesTotal()
        {
            var today = DateTime.Today;
            var sales = _context.Sales.ToList().Where(x => x.CreatedDate.HasValue && x.CreatedDate.Value.Date == today && x.TotalAmount > 0);
            return sales.Sum(x => x.TotalAmount);
        }

        // BUGÜNKÜ SATIŞ SAYISI
        public int GetTodaySalesCount()
        {
            var today = DateTime.Today;
            return _context.Sales.ToList().Count(x => x.CreatedDate.HasValue && x.CreatedDate.Value.Date == today && x.TotalAmount > 0);
        }
    }
}