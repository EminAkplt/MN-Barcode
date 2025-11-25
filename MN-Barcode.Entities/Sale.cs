using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MN_Barcode.Entities
{
    public class Sale : SaaSEntity
    {
        [Required, StringLength(50)]
        public string TransactionCode { get; set; }
        public decimal TotalAmount { get; set; }
        [StringLength(20)]
        public string PaymentType { get; set; }

        public ICollection<SaleDetail> SaleDetails { get; set; }
    }
}
