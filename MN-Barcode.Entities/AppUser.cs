using System.ComponentModel.DataAnnotations;

namespace MN_Barcode.Entities
{
    public class AppUser : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(20)]
        public string Password { get; set; }

        [StringLength(50)]
        public string FullName { get; set; }

        /// <summary>
        /// Kullanıcı rolü (Admin / Kasiyer / Viewer).
        /// Veritabanında string olarak saklanır (BarcodeContext.OnModelCreating).
        /// </summary>
        public UserRole Role { get; set; } = UserRole.Kasiyer;
    }
}
