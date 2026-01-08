using System.ComponentModel.DataAnnotations;

namespace MN_Barcode.Entities
{
    /// <summary>
    /// Sistem ayarları - şifreler vs.
    /// </summary>
    public class SystemSettings : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string SettingKey { get; set; }

        [Required]
        [StringLength(200)]
        public string SettingValue { get; set; }
    }
}
