# 🧾 MN-Barcode — Küçük İşletmeler İçin Barkod & Satış Otomasyonu

Bakkal, market, kasap, butik, kırtasiye gibi **küçük işletmeler** için tasarlanmış,
**internet gerektirmeden** (tamamen yerel / offline) çalışan barkodlu satış, stok ve
gider takip programı.

> **Felsefe:** İnternet kesilse bile kasa durmaz. Aylık bulut faturası yoktur.
> "USB'ye tak, kur, sat." — esnafın gerçekten ihtiyacı olan sadelik.

---

## ✨ Özellikler

- 🛒 **Hızlı Satış Ekranı** — Barkod okutma, hızlı ürün tuşları, manuel tutar girişi
- 📷 **Barkod Okuyucu Desteği** — USB barkod okuyucular (klavye taklidi) kutu odakta olmasa bile otomatik yakalanır
- 🔄 **İade Modu** — Satışları negatif kayıtla güvenli şekilde iade alma
- 📦 **Stok Yönetimi** — Ürün ekleme/düzenleme, kategori, kritik stok uyarısı
- 💸 **Gider Takibi** — İşletme giderlerini kaydetme ve raporlama
- 📊 **Raporlar & Dashboard** — Günlük/aylık ciro, en çok satan ürünler, en çok iade edilenler
- 🧾 **Satış Geçmişi** — Tarih filtreli satış ve iade dökümü
- 🔐 **Kullanıcı / Şifre** — Basit kullanıcı doğrulama altyapısı

---

## 🏗️ Mimari (4 Katmanlı N-Tier)

Sade, anlaşılır ve büyümeye uygun katmanlı yapı:

```
MN-Barcode.Entities     →  Veri sınıfları (Product, Sale, Expense...)
MN-Barcode.DataAccess   →  EF Core + Veritabanı erişimi (BarcodeContext)
MN-Barcode.Business     →  İş mantığı servisleri (Satış, Stok, Rapor...)
MN-Barcode.WinForms     →  Masaüstü arayüz (kasa ekranı)
```

İş mantığı arayüzden ayrı durduğu için ileride **web/mobil/bulut** sürümü istenirse
`Business` ve `DataAccess` katmanları aynen kullanılabilir.

---

## 🛠️ Teknolojiler

| Katman | Teknoloji |
|---|---|
| Dil | C# / .NET |
| Arayüz | Windows Forms (.NET 9) |
| ORM | Entity Framework Core 8 |
| Veritabanı | SQL Server Express (yerel) |

---

## 🚀 Kurulum

### Gereksinimler
- Windows 10/11
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server Express (yerel veritabanı için)

### Adımlar

```bash
# 1. Projeyi klonla
git clone https://github.com/<kullanici>/MN-Barcode.git
cd MN-Barcode

# 2. Veritabanı bağlantısını ayarla
#    MN-Barcode.WinForms/appsettings.json içindeki "DefaultConnection" satırını
#    kendi SQL Server adresinize göre düzenleyin.

# 3. Veritabanını oluştur (EF Core migration'larını uygula)
dotnet ef database update --project MN-Barcode.DataAccess --startup-project MN-Barcode.WinForms

# 4. Çalıştır
dotnet run --project MN-Barcode.WinForms
```

---

## ⚙️ Yapılandırma

Veritabanı bağlantısı **koda gömülü değildir**; `MN-Barcode.WinForms/appsettings.json`
dosyasından okunur. Farklı bir sunucu/veritabanı için sadece bu dosyayı düzenleyin:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=.\\SQLEXPRESS; Initial Catalog=MNBarcodeLocal; Integrated Security=True; TrustServerCertificate=True;"
  }
}
```

---

## 📷 Barkod Okuyucu Kullanımı

Programa özel sürücü/SDK **gerekmez**. Piyasadaki USB barkod okuyucuların %95'i
"klavye taklidi" (keyboard wedge) yapar: okuttuğunda barkodu yazıp Enter'a basar.
MN-Barcode bu girişi satış ekranında otomatik yakalar. Test için okuyucu olmadan da
klavyeden barkodu yazıp Enter'a basabilirsiniz.

---

## 🗺️ Yol Haritası

- [ ] Fiş/makbuz yazıcısı entegrasyonu (ESC/POS)
- [ ] Barkod etiketi üretme/yazdırma
- [ ] Yedekleme & geri yükleme
- [ ] (Opsiyonel, ileride) Bulut senkronizasyonu ve mobil sürüm

---

## 📄 Lisans

Bu proje özel bir lisans altındadır. Detaylar için proje sahibiyle iletişime geçin.
