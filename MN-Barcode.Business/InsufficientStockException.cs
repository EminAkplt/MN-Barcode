using System;

namespace MN_Barcode.Business
{
    /// <summary>
    /// Stoktan fazla satış yapılmaya çalışıldığında fırlatılır.
    ///
    /// Ayrı bir istisna tipi olmasının sebebi: kasadaki kullanıcıya "bir hata oluştu"
    /// yerine "X üründen stokta yalnızca N adet var" demek. Genel Exception yakalamayla
    /// karışmaması için ayrı tutuldu.
    /// </summary>
    public class InsufficientStockException : Exception
    {
        public string UrunAdi { get; }
        public double MevcutStok { get; }
        public double IstenenMiktar { get; }

        public InsufficientStockException(string urunAdi, double mevcutStok, double istenenMiktar)
            : base(Mesaj(urunAdi, mevcutStok, istenenMiktar))
        {
            UrunAdi = urunAdi;
            MevcutStok = mevcutStok;
            IstenenMiktar = istenenMiktar;
        }

        private static string Mesaj(string urunAdi, double mevcut, double istenen)
        {
            return $"'{urunAdi}' ürününde yeterli stok yok.\n\n" +
                   $"Stokta: {Bicimle(mevcut)}\n" +
                   $"İstenen: {Bicimle(istenen)}\n\n" +
                   "Sepetteki miktarı azaltın veya Ürün Yönetimi'nden stok girişi yapın.";
        }

        /// <summary>Tam sayıysa ondalık gösterme (3 yerine 3,00 yazmasın).</summary>
        private static string Bicimle(double d)
        {
            return d == Math.Floor(d) ? d.ToString("N0") : d.ToString("N2");
        }
    }
}
