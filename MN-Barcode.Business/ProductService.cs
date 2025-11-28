using MN_Barcode.DataAccess;
using MN_Barcode.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MN_Barcode.Business
{
    public class ProductService
    {
        private BarcodeContext _context;

        public ProductService()
        {
            _context = new BarcodeContext();
        }




        // --- BU METODU ProductService CLASS'ININ İÇİNE EKLE ---

        public Product GetOrCreateQuickProduct(string name, string barcode, decimal defaultPrice)
        {
            // 1. Ürün veritabanında var mı?
            var existingProduct = _context.Products.FirstOrDefault(p => p.Barcode == barcode);

            if (existingProduct != null)
            {
                // Varsa onu döndür
                return existingProduct;
            }

            // 2. Yoksa, önce "Hızlı Satış" diye bir kategori var mı bakalım
            var category = _context.Categories.FirstOrDefault(c => c.Name == "Hızlı Satış");
            if (category == null)
            {
                // Kategori yoksa oluştur
                category = new Category { Name = "Hızlı Satış" };
                _context.Categories.Add(category);
                _context.SaveChanges();
            }

            // 3. Ürünü Oluştur
            var newProduct = new Product
            {
                Name = name,
                Barcode = barcode,
                BuyingPrice = 0, // Maliyet 0 olsun
                SellingPrice = defaultPrice,
                StockQuantity = 10000, // Stok derdi olmasın
                CategoryId = category.Id, // Az önce bulduğumuz/oluşturduğumuz kategoriye bağla
                
            };

            _context.Products.Add(newProduct);
            _context.SaveChanges();

            return newProduct;
        }

        // --- Controller'ın Aradığı Metot Bu ---
        public Product GetByBarcode(string barcode)
        {
            // Veritabanına git, barkodu eşleşen ürünü bul.
            // .Include(x => x.Category) diyerek kategorisini de getiriyoruz.
            var urun = _context.Products
                               .Include(x => x.Category)
                               .FirstOrDefault(x => x.Barcode == barcode);

            return urun;
        }

        // İleride lazım olur diye bunu da ekleyelim
        public List<Product> GetAll()
        {
            return _context.Products.Include(x => x.Category).ToList();
        }
    }
}
