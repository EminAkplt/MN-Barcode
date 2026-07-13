namespace MN_Barcode.Entities
{
    /// <summary>
    /// Satış türü. Sale.SaleType alanında kullanılır.
    /// Negatif TotalAmount yerine açık bayrak ile iade/satış ayrımı yapılır.
    /// </summary>
    public enum SaleType
    {
        Satis,  // Normal satış
        Iade    // İade işlemi
    }
}
