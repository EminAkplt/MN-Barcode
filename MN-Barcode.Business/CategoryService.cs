using MN_Barcode.DataAccess;
using MN_Barcode.Entities;
using System.Collections.Generic;
using System.Linq;

namespace MN_Barcode.Business
{
    public class CategoryService
    {
        private BarcodeContext _context;

        public CategoryService()
        {
            _context = new BarcodeContext();
        }

        // Tüm kategorileri getir (ComboBox için)
        public List<Category> GetCategories()
        {
            return _context.Categories.OrderBy(x => x.Name).ToList();
        }

        // Kategori ekle
        public void Add(Category category)
        {
            _context.Categories.Add(category);
            _context.SaveChanges();
        }

        // Kategori sil
        public void Delete(int id)
        {
            var cat = _context.Categories.Find(id);
            if (cat != null)
            {
                _context.Categories.Remove(cat);
                _context.SaveChanges();
            }
        }
    }
}
