using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MN_Barcode.Entities
{
    public class SaleDetail : BaseEntity
    {
        // Hangi Satışa ait? (Fiş No)
        public int SaleId { get; set; }
        public Sale Sale { get; set; }

        // Hangi Ürün satıldı?
        public int ProductId { get; set; }
        public Product Product { get; set; }

        // --- TARİHÇE VERİSİ ---
        public decimal SellingPrice { get; set; } // O anki satış fiyatı (Mühürlenen Fiyat)

        public double Quantity { get; set; } // Kaç tane satıldı?

        public decimal TotalPrice { get; set; } // (Birim Fiyat * Adet) - Satır toplamı
    }
}
