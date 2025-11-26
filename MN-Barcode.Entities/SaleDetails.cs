using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MN_Barcode.Entities
{
    public class SaleDetail : BaseEntity
    {
        public int SaleId { get; set; }
        public Sale Sale { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public decimal SellingPrice { get; set; } 
        public double Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
