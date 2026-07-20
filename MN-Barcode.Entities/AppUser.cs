using System.ComponentModel.DataAnnotations;

namespace MN_Barcode.Entities
{
    public class AppUser : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        /// <summary>
        /// Şifrenin PBKDF2 hash'i (bkz. PasswordHasher).
        /// Eski kurulumlarda düz metin olabilir; ilk başarılı girişte hash'e yükseltilir.
        /// Uzunluk 200: hash biçimi "pbkdf2$tur$tuz$ozet" ~90 karakter tutar,
        /// eski sınır olan 20 karakter hash'i kesip girişleri kalıcı olarak bozardı.
        /// </summary>
        [Required]
        [StringLength(200)]
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
