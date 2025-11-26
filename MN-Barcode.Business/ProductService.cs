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
