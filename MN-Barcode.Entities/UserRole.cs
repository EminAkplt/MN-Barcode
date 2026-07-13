namespace MN_Barcode.Entities
{
    /// <summary>
    /// Kullanıcı rolü. AppUser.Role alanında kullanılır.
    /// Veritabanında string olarak saklanır (HasConversion).
    /// </summary>
    public enum UserRole
    {
        Admin,      // Tam yetki
        Kasiyer,    // Satış yapabilir, rapor göremez
        Viewer      // Sadece görüntüleyebilir
    }
}
