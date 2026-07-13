namespace MN_Barcode.Entities
{
    /// <summary>
    /// Ödeme yöntemi. Sale.PaymentType alanında kullanılır.
    /// Veritabanında string olarak saklanır (HasConversion).
    /// </summary>
    public enum PaymentType
    {
        Nakit,
        KrediKarti,
        Iade        // İade işlemleri için
    }
}
