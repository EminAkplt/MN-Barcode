using System.ComponentModel.DataAnnotations;

namespace MN_Barcode.Entities
{
    public class Sale : BaseEntity
    {
        [Required, StringLength(50)]
        public string TransactionCode { get; set; }

        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Ödeme yöntemi (Nakit / KrediKarti / Iade).
        /// Veritabanında string olarak saklanır (BarcodeContext.OnModelCreating).
        /// </summary>
        public PaymentType PaymentType { get; set; } = PaymentType.Nakit;

        /// <summary>
        /// Satış türü: normal satış mı, iade mi?
        /// Negatif TotalAmount kontrolünden daha güvenilir.
        /// </summary>
        public SaleType SaleType { get; set; } = SaleType.Satis;

        public ICollection<SaleDetail> SaleDetails { get; set; }
    }
}
