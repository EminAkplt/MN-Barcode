using MN_Barcode.DataAccess;
using MN_Barcode.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace MN_Barcode.Business
{
    /// <summary>
    /// Kategori iş mantığı servisi.
    ///
    /// ÖNEMLİ TASARIM NOTU (tüm servislerde aynıdır):
    /// Eskiden her servis kurulduğunda TEK bir BarcodeContext açıp uygulama
    /// kapanana kadar tutuyordu. Bu; bayat (eski) veri göstermeye ve
    /// "bu entity zaten takip ediliyor" hatalarına yol açıyordu.
    ///
    /// Yeni yaklaşım: Her metot kendi kısa ömürlü context'ini açar ("using" ile)
    /// ve metot bitince context otomatik kapanır. Böylece her işlem güncel veriyle
    /// ve temiz bir takip durumuyla çalışır. Yerel/tek kullanıcılı uygulamada
    /// bu desenin maliyeti yok denecek kadar azdır.
    /// </summary>
    public class CategoryService
    {
        // Tüm kategorileri getir (ComboBox için), isme göre sıralı.
        public List<Category> GetCategories()
        {
            using var context = new BarcodeContext();
            return context.Categories.OrderBy(x => x.Name).ToList();
        }

        // Yeni kategori ekle.
        public void Add(Category category)
        {
            using var context = new BarcodeContext();
            context.Categories.Add(category);
            context.SaveChanges();
        }

        /// <summary>
        /// Kategoriyi siler.
        ///
        /// İçinde ürün bulunan kategori silinemez: Product.CategoryId yabancı anahtarı
        /// Cascade tanımlı olmadığı için veritabanı DELETE'i reddediyor ve fırlayan
        /// DbUpdateException uygulamayı çökertiyordu. Artık önceden kontrol edilip
        /// anlaşılır bir hata veriliyor.
        /// </summary>
        /// <exception cref="InvalidOperationException">Kategoride ürün varsa.</exception>
        public void Delete(int id)
        {
            using var context = new BarcodeContext();

            var cat = context.Categories.Find(id);
            if (cat == null) return;

            int urunSayisi = context.Products.Count(p => p.CategoryId == id);
            if (urunSayisi > 0)
            {
                throw new InvalidOperationException(
                    $"'{cat.Name}' kategorisi silinemez: içinde {urunSayisi} ürün var.\n\n" +
                    "Önce bu ürünleri başka bir kategoriye taşıyın veya silin.");
            }

            context.Categories.Remove(cat);
            context.SaveChanges();
        }

        // Kategoriyi ID'ye göre getir.
        public Category GetById(int id)
        {
            using var context = new BarcodeContext();
            return context.Categories.Find(id);
        }

        // Kategori adını güncelle.
        public void Update(Category category)
        {
            using var context = new BarcodeContext();
            // Veritabanındaki güncel kaydı bul, sadece adını değiştir.
            var existing = context.Categories.Find(category.Id);
            if (existing != null)
            {
                existing.Name = category.Name;
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Kategorileri ürün sayısıyla birlikte getirir (DB'de hesaplanır).
        /// ProductForm kategori listesinde kullanılır — tüm ürünleri belleğe çekmez.
        /// </summary>
        public List<(Category Category, int ProductCount)> GetCategoriesWithCount()
        {
            using var context = new BarcodeContext();
            return context.Categories
                .Select(c => new { Cat = c, Count = context.Products.Count(p => p.CategoryId == c.Id) })
                .OrderBy(x => x.Cat.Name)
                .AsEnumerable()
                .Select(x => (x.Cat, x.Count))
                .ToList();
        }
    }
}
