using MN_Barcode.DataAccess;
using MN_Barcode.Entities;
using Microsoft.EntityFrameworkCore;
using System;
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
        public List<Product> GetProducts(string search = "", string categoryName = "")
        {
            using var context = new BarcodeContext();

            var query = context.Products.Include(x => x.Category).AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(x => x.Name.Contains(search) || x.Barcode.Contains(search));

            if (!string.IsNullOrEmpty(categoryName) && categoryName != "Tümü")
                query = query.Where(x => x.Category != null && x.Category.Name == categoryName);

            return query.OrderByDescending(x => x.Id).ToList();
        }

        // 2. TEK ÜRÜN GETİR (ID ile - Düzenleme için lazım olacak)
        public Product GetById(int id)
        {
            using var context = new BarcodeContext();
            return context.Products.Find(id);
        }

        /// <summary>
        /// Belirtilen önekle başlayan barkodları tek sorguda getirir.
        ///
        /// Barkod üretiminde benzersizlik kontrolü için kullanılır: eskiden her aday
        /// için ayrı bir veritabanı sorgusu (ve ayrı bağlantı) açılıyordu; soğuk
        /// LocalDB'de bu, arayüzü onlarca saniye kilitleyebiliyordu.
        /// </summary>
        public HashSet<string> GetBarcodesWithPrefix(string prefix)
        {
            using var context = new BarcodeContext();
            var liste = context.Products
                .AsNoTracking()
                .Where(p => p.Barcode != null && p.Barcode.StartsWith(prefix))
                .Select(p => p.Barcode)
                .ToList();

            return new HashSet<string>(liste, StringComparer.Ordinal);
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
        /// <summary>
        /// Ürünü siler.
        ///
        /// DİKKAT: SaleDetails → Products ilişkisi veritabanında Cascade tanımlı.
        /// Yani bu ürünü silmek, ürünün geçmiş SATIŞ SATIRLARINI da siler.
        /// Satış başlıklarındaki toplam tutarlar yerinde kalacağı için defterler
        /// tutmaz hale gelir ve geri dönüşü olmaz.
        ///
        /// Bu yüzden satış geçmişi olan ürünün silinmesine izin verilmez.
        /// Kullanılmayan ürün için "pasife alma" tercih edilmelidir.
        /// </summary>
        /// <exception cref="InvalidOperationException">Ürünün satış geçmişi varsa.</exception>
        public void Delete(int id)
        {
            using var context = new BarcodeContext();

            var product = context.Products.Find(id);
            if (product == null) return;

            int satisSatiri = context.SaleDetails.Count(d => d.ProductId == id);
            if (satisSatiri > 0)
            {
                throw new InvalidOperationException(
                    $"'{product.Name}' ürünü silinemez: {satisSatiri} adet satış kaydı var.\n\n" +
                    "Bu ürünü silmek geçmiş satış detaylarını da siler ve raporlarınız tutmaz hale gelir.\n\n" +
                    "Ürünü kullanmayacaksanız stoğunu 0 yapın veya adını değiştirip pasife alın.");
            }

            context.Products.Remove(product);
            context.SaveChanges();
        }

        /// <summary>Ürünün silinip silinemeyeceğini önceden söyler (arayüzde buton gizlemek için).</summary>
        public bool SilinebilirMi(int id)
        {
            using var context = new BarcodeContext();
            return !context.SaleDetails.Any(d => d.ProductId == id);
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
