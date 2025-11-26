using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MN_Barcode.Entities
{
    public class Expense :BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; } 

        public decimal Amount { get; set; } 

        [StringLength(500)]
        public string Description { get; set; } 

    }
}
