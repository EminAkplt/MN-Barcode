namespace MN_Barcode.WinForms
{
    /// <summary>
    /// Önbellekten tekrar gösterilen ekranların verilerini tazelemesi için sözleşme.
    ///
    /// NEDEN GEREKLİ:
    /// MainForm ekranları önbelleğe alıp tekrar tekrar kullanıyor; geçişte form
    /// Dispose edilmiyor, yalnızca Hide() ediliyor. Ekranlar ise verilerini
    /// Form.Shown olayında yüklüyordu — ve Shown yalnızca formun İLK gösteriminde
    /// tetiklenir. Sonuç: sabah açılan Ana Sayfa, 200 satış yapıldıktan sonra bile
    /// hâlâ ₺0 ciro gösteriyordu. Aynı sorun Satış Geçmişi, İadeler, Stok ve Gider
    /// ekranlarında da vardı.
    ///
    /// Kullanıcı bunu "program satışlarımı kaydetmiyor" diye algılar ve destek arar.
    /// Bu arayüzü uygulayan ekranlar, önbellekten her açılışta yeniden yüklenir.
    /// </summary>
    public interface IEkranYenileme
    {
        /// <summary>Ekranın verilerini veritabanından yeniden yükler.</summary>
        void EkraniYenile();
    }
}
