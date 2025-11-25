using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MN_Barcode.Entities
{
    public class Company : BaseEntity // DİKKAT: SaaSEntity DEĞİL!
    {
        [Required, StringLength(100)]
        public string CompanyName { get; set; }

        // Login Bilgileri
        [Required, StringLength(20)]
        public string CompanyCode { get; set; } // Kullanıcı Adı niyetine
        [Required, StringLength(20)]
        public string Password { get; set; }

        // İlişkiler: Bir şirketin neleri olur?
        public ICollection<Product> Products { get; set; }
        public ICollection<Category> Categories { get; set; }
        public ICollection<Sale> Sales { get; set; }
        public ICollection<Expense> Expenses { get; set; }
    }
}