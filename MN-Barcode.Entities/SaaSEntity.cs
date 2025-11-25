using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MN_Barcode.Entities
{
    public abstract class SaaSEntity : BaseEntity
    {
        [Required] // İşte senin istediğin kural! Boş geçilemez.
        public int CompanyId { get; set; }

        // Navigation Property (İsteğe bağlı, kodlarken kolaylık olsun diye)
        public Company Company { get; set; }
    }
}
