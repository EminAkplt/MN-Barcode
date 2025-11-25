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
        [Required]
        [StringLength(100)]

        public string Name { get; set; }

        [StringLength(50)]
        public string Barcode { get; set; } 

        [StringLength(500)]
        public string Description { get; set; }

       
        public decimal? BuyingPrice { get; set; } 
        public decimal SellingPrice { get; set; } 

     
        public double StockQuantity { get; set; }

        [StringLength(20)]
        public string Unit { get; set; } 

     

        public int CategoryId { get; set; } // Veritabanındaki ID'si (Örn: 5)

        
        public Category Category { get; set; }
    }
}
