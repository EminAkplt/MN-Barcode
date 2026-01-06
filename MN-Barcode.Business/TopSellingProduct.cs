namespace MN_Barcode.Business
{
    public class TopSellingProduct
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Barcode { get; set; }
        public double TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
