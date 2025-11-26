using MN_Barcode.DataAccess;
using MN_Barcode.Entities;
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
            // Veritabanı işlemi başlatıyoruz. 
            // Eğer elektrik kesilirse veya hata olursa "yarım yamalak" kayıt yapmaz, hepsini iptal eder.
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. FİŞİ KAYDET
                    sale.CreatedDate = DateTime.Now;
                    _context.Sales.Add(sale);
                    _context.SaveChanges(); // Önce bunu kaydet ki 'SaleId' oluşsun

                    // 2. DETAYLARI EKLE VE STOKTAN DÜŞ
                    foreach (var item in details)
                    {
                        item.SaleId = sale.Id; // Oluşan Fiş ID'sini bağla
                        _context.SaleDetails.Add(item);

                        // STOK GÜNCELLEME
                        var product = _context.Products.Find(item.ProductId);
                        if (product != null)
                        {
                            // Stok eksiye düşse bile sat (Bakkal deftere yazar sonra düzeltir)
                            product.StockQuantity -= item.Quantity;
                        }
                    }

                    _context.SaveChanges(); // Detayları ve stok değişimini kaydet

                    transaction.Commit(); // Her şey başarılı, onayla ve bitir.
                }
                catch (Exception ex)
                {
                    transaction.Rollback(); // Hata oldu! Yapılan her şeyi geri al.
                    throw ex; // Hatayı ekrana fırlat ki görelim
                }
            }
        }
    }
}