using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MN_Barcode.Entities
{
    public class Product : BaseEntity
    {
        [Required, StringLength(100)]
        public string Name { get; set; }

        [StringLength(50)]
        public string Barcode { get; set; }

        public decimal BuyingPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public double StockQuantity { get; set; }

        /// <summary>
        /// Ürün birimi: Adet, Kg, Lt, Paket vs.
        /// Satış ekranında ve raporlarda gösterilir.
        /// </summary>
        [StringLength(20)]
        public string Unit { get; set; } = "Adet";

        // Kategori İlişkisi
        public int? CategoryId { get; set; }
        public Category Category { get; set; }
    }
}
