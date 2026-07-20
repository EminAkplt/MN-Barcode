using System.Collections.Generic;
using MN_Barcode.Entities;

namespace MN_Barcode.Business
{
    /// <summary>
    /// Geçmiş ekranlarının (satış / iade) sayfalı sonucu.
    ///
    /// NEDEN AYRI BİR TİP:
    /// Ekranda gösterilen satır sayısı sınırlanınca toplam tutarı gösterilen
    /// satırlardan hesaplamak YANLIŞ sonuç verir — kullanıcı eksik bir ciro görür.
    /// Bu yüzden toplam ve satır sayısı veritabanında, aralığın TAMAMI üzerinden
    /// hesaplanır ve satırlardan ayrı taşınır.
    /// </summary>
    public class GecmisSonucu
    {
        /// <summary>Ekranda gösterilecek satırlar (sınırlanmış olabilir).</summary>
        public List<SaleDetail> Satirlar { get; set; } = new List<SaleDetail>();

        /// <summary>Aralıktaki TOPLAM satır sayısı (sınırlamadan önce).</summary>
        public int ToplamSatirSayisi { get; set; }

        /// <summary>Aralığın TAMAMININ tutarı — gösterilen satırlardan değil, veritabanından.</summary>
        public decimal ToplamTutar { get; set; }

        /// <summary>Satırlar sınıra takıldı mı? Takıldıysa kullanıcı uyarılmalıdır.</summary>
        public bool Kirpildi => ToplamSatirSayisi > Satirlar.Count;
    }
}
