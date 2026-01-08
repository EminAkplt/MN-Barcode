using System;

namespace MN_Barcode.Entities
{
    /// <summary>
    /// Dashboard grafikleri için günlük satış verisi DTO
    /// </summary>
    public class DailySalesData
    {
        public DateTime Date { get; set; }
        public decimal Total { get; set; }
    }
}
