using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MN_Barcode.Entities
{
    public class Sale : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string TransactionCode { get; set; } // İşlem Kodu (Benzersiz bir kod üreteceğiz, örn: Satis-20231025-001)

        public decimal TotalAmount { get; set; } // Fişin Genel Toplamı

        [StringLength(20)]
        public string PaymentType { get; set; } // "Nakit", "Kredi Kartı", "Veresiye"

        // --- İLİŞKİ ---
        // Bir satışın içinde birden fazla ürün satılmış olabilir (Sepet mantığı).
        public ICollection<SaleDetail> SaleDetails { get; set; }
    }
}
