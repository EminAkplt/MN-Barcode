using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MN_Barcode.Entities
{
    public class Category : BaseEntity // Şirkete aittir
    {
        [Required, StringLength(50)]
        public string Name { get; set; }

        public ICollection<Product> Products { get; set; }
    }
}
