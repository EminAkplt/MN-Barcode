using MN_Barcode.DataAccess;
using MN_Barcode.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace MN_Barcode.Business
{
    public class ProductService
    {
        private BarcodeContext _context;

        public ProductService()
        {
            _context = new BarcodeContext();
        }

        // 1. LİSTELEME (Arama destekli)
        public List<Product> GetProducts(string search = "")
        {
            var query = _context.Products.Include(x => x.Category).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                // İsimde veya Barkodda ara
                query = query.Where(x => x.Name.Contains(search) || x.Barcode.Contains(search));
            }

            // En son eklenen en üstte olsun
            return query.OrderByDescending(x => x.Id).ToList();
        }

        // 2. TEK ÜRÜN GETİR (ID ile - Düzenleme için lazım olacak)
        public Product GetById(int id)
        {
            return _context.Products.Find(id);
        }

        // 3. BARKOD İLE GETİR (Satış ekranı için)
        public Product GetByBarcode(string barcode)
        {
            return _context.Products.AsNoTracking().FirstOrDefault(x => x.Barcode == barcode);
        }

        // 4. HIZLI ÜRÜN OLUŞTURMA (Satış ekranındaki o kod)
        public Product GetOrCreateQuickProduct(string name, string barcode, decimal defaultPrice)
        {
            var existing = _context.Products.FirstOrDefault(p => p.Barcode == barcode);
            if (existing != null) return existing;

            var cat = _context.Categories.FirstOrDefault(c => c.Name == "Hızlı Satış");
            if (cat == null)
            {
                cat = new Category { Name = "Hızlı Satış" };
                _context.Categories.Add(cat);
                _context.SaveChanges();
            }

            var newProduct = new Product
            {
                Name = name,
                Barcode = barcode,
                BuyingPrice = 0,
                SellingPrice = defaultPrice,
                StockQuantity = 10000,
                CategoryId = cat.Id
            };
            _context.Products.Add(newProduct);
            _context.SaveChanges();
            return newProduct;
        }

        // 5. EKLEME / GÜNCELLEME
        public void Save(Product product)
        {
            if (product.Id == 0)
            {
                _context.Products.Add(product); // Yeni Kayıt
            }
            else
            {
                _context.Products.Update(product); // Güncelleme
            }
            _context.SaveChanges();
        }

        // 6. SİLME
        public void Delete(int id)
        {
            var product = _context.Products.Find(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                _context.SaveChanges();
            }
        }
    }
}