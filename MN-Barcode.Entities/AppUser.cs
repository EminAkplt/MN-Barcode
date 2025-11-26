using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MN_Barcode.Entities
{
    public class AppUser : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; } // Kullanıcı Adı (Örn: admin)

        [Required]
        [StringLength(20)]
        public string Password { get; set; } // Şifre

        [StringLength(50)]
        public string FullName { get; set; } // Ad Soyad

        [StringLength(20)]
        public string Role { get; set; } // Admin, Kasiyer vs.
    }
}
