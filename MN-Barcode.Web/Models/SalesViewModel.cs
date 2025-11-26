namespace MN_Barcode.Web.Models
{
    // SEPETTEKİ TEK BİR SATIR
    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string Barcode { get; set; }
        public string ProductName { get; set; }

        // Birim Fiyat (O anki fiyatı buraya kopyalayacağız)
        public decimal Price { get; set; }

        // Müşterinin sepete attığı adet
        public int Quantity { get; set; } = 1;

        // Satır Toplamı (Fiyat * Adet)
        public decimal Total => Price * Quantity;
    }

    // SATIŞ EKRANININ TAMAMI
    public class SalesPageViewModel
    {
        // Sepetteki ürünlerin listesi
        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();

        // Ekranın sağındaki Dev Tabela için Genel Toplam
        public decimal GrandTotal => CartItems.Sum(x => x.Total);
    }
}