using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations; 

namespace MN_Barcode.Entities
{
    public class Product : BaseEntity 
    {
        [Required, StringLength(100)]
        public string Name { get; set; }

        [StringLength(50)]
        public string Barcode { get; set; }

        public string Description { get; set; }
        public decimal BuyingPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public double StockQuantity { get; set; }
        [StringLength(20)]
        public string Unit { get; set; }

        // Kategori İlişkisi
        public int CategoryId { get; set; }
        public Category Category { get; set; }
    }
}
