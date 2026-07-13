using MN_Barcode.DataAccess;
using MN_Barcode.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace MN_Barcode.Business
{
    /// <summary>
    /// Ürün iş mantığı servisi.
    /// Her metot kendi kısa ömürlü context'ini kullanır (bkz. CategoryService notu).
    /// </summary>
    public class ProductService
    {
        // 1. LİSTELEME (Arama destekli)
        public List<Product> GetProducts(string search = "")
        {
            using var context = new BarcodeContext();

            // Kategori bilgisini de birlikte çek (Include).
            var query = context.Products.Include(x => x.Category).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                // İsimde veya Barkodda ara (filtre veritabanında çalışır).
                query = query.Where(x => x.Name.Contains(search) || x.Barcode.Contains(search));
            }

            // En son eklenen en üstte olsun.
            return query.OrderByDescending(x => x.Id).ToList();
        }

        // 2. TEK ÜRÜN GETİR (ID ile - Düzenleme için lazım olacak)
        public Product GetById(int id)
        {
            using var context = new BarcodeContext();
            return context.Products.Find(id);
        }

        // 3. BARKOD İLE GETİR (Satış ekranı için)
        public Product GetByBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return null;
            using var context = new BarcodeContext();
            // AsNoTracking: Sadece okuma; takip maliyeti olmadan hızlı sonuç.
            return context.Products.AsNoTracking().FirstOrDefault(x => x.Barcode == barcode);
        }

        // 4. HIZLI ÜRÜN OLUŞTURMA (Satış ekranındaki o kod)
        //    Barkodu olan ürünü getirir; yoksa "Hızlı Satış" kategorisinde oluşturur.
        public Product GetOrCreateQuickProduct(string name, string barcode, decimal defaultPrice)
        {
            using var context = new BarcodeContext();

            var existing = context.Products.FirstOrDefault(p => p.Barcode == barcode);
            if (existing != null) return existing;

            // "Hızlı Satış" kategorisi yoksa oluştur.
            var cat = context.Categories.FirstOrDefault(c => c.Name == "Hızlı Satış");
            if (cat == null)
            {
                cat = new Category { Name = "Hızlı Satış" };
                context.Categories.Add(cat);
                context.SaveChanges();
            }

            var newProduct = new Product
            {
                Name = name,
                Barcode = barcode,
                BuyingPrice = 0,
                SellingPrice = defaultPrice,
                StockQuantity = 10000, // Hızlı ürünlerde stok takibi yapılmadığı için yüksek değer
                CategoryId = cat.Id
            };
            context.Products.Add(newProduct);
            context.SaveChanges();
            return newProduct;
        }

        // 5. EKLEME / GÜNCELLEME
        public void Save(Product product)
        {
            using var context = new BarcodeContext();

            if (product.Id == 0)
            {
                context.Products.Add(product);    // Yeni Kayıt (Id henüz yok)
            }
            else
            {
                context.Products.Update(product); // Mevcut kaydı güncelle
            }
            context.SaveChanges();
        }

        // 6. SİLME
        public void Delete(int id)
        {
            using var context = new BarcodeContext();
            var product = context.Products.Find(id);
            if (product != null)
            {
                context.Products.Remove(product);
                context.SaveChanges();
            }
        }

        // 7. STOĞU AZALAN ÜRÜNLER (Dashboard için)
        public List<Product> GetLowStockProducts(int limit = 10, double threshold = 20)
        {
            using var context = new BarcodeContext();
            // Eşik altındaki ürünleri, en az stoğu olan en üstte olacak şekilde getir.
            return context.Products
                .Include(x => x.Category)
                .Where(x => x.StockQuantity <= threshold)
                .OrderBy(x => x.StockQuantity)
                .Take(limit)
                .ToList();
        }
    }
}
