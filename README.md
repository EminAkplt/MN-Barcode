<div align="center">

# 🧾 MN-Barcode

**Offline barcode POS, stock & expense tracker for small businesses.**
*Küçük işletmeler için internet gerektirmeyen barkodlu satış, stok ve gider programı.*

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-8.0-512BD4)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6?logo=windows&logoColor=white)
![Offline](https://img.shields.io/badge/works-100%25%20offline-success)

</div>

---

## 📖 About

**MN-Barcode** is a desktop point-of-sale (POS) application built for **small shops** —
grocery stores, markets, butchers, boutiques, stationery shops. It runs **fully offline**
on a local database: no internet connection, no monthly cloud bills.

> **Philosophy:** the till keeps working even when the internet is down.
> *"Plug in the USB scanner, install, and sell."* — the simplicity a small merchant actually needs.

The UI is in **Turkish** (its target market), while the codebase and documentation are
kept approachable for international contributors.

---

## ✨ Features

- 🛒 **Fast Sale Screen** — barcode scanning, quick-product buttons, manual price entry
- 📷 **Barcode Scanner Support** — USB "keyboard-wedge" scanners are captured automatically, even when the input box is not focused
- 🔄 **Return Mode** — safely process returns as negative records
- 📦 **Stock Management** — add/edit products, categories, low-stock warnings
- 🏷️ **Barcode & Label Printing** — generate unique barcodes (Code 128) and print product labels with live preview, adjustable label size, and multi-copy layout — *no external library, pure GDI+*
- 💸 **Expense Tracking** — record and review business expenses
- 📊 **Reports & Dashboard** — daily/monthly revenue, best-selling and most-returned products
- 🧾 **Sales History** — date-filtered sale & return breakdown, color-coded per receipt
- 🔐 **Users / Passwords** — simple authentication with role-based access
- 🛡️ **Stock never goes negative** — sales always complete; stock floors at 0

---

## 🖼️ Screenshots

> Add your screenshots under `docs/screenshots/` and link them here.

| Home Dashboard | Fast Sale | Stock Management |
|:---:|:---:|:---:|
| _coming soon_ | _coming soon_ | _coming soon_ |

---

## 🏗️ Architecture (4-Layer N-Tier)

A clean, understandable, growth-ready layered structure:

```
MN-Barcode.Entities     →  Data classes (Product, Sale, Expense, ...)
MN-Barcode.DataAccess   →  EF Core + database access (BarcodeContext, migrations)
MN-Barcode.Business     →  Business-logic services (Sales, Stock, Reporting, ...)
MN-Barcode.WinForms     →  Desktop UI (the till screen)
```

Because business logic is decoupled from the UI, the `Business` and `DataAccess`
layers can be reused as-is if a **web / mobile / cloud** version is built later.

---

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| Language | C# / .NET 9 |
| UI | Windows Forms |
| ORM | Entity Framework Core 8 |
| Database | SQL Server LocalDB (default) / SQL Server Express |

---

## 🚀 Getting Started

### Prerequisites
- Windows 10 / 11
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- **SQL Server LocalDB** (ships with Visual Studio, or install SQL Server Express)

### Run

```bash
# 1. Clone
git clone https://github.com/EminAkplt/MN-Barcode.git
cd MN-Barcode

# 2. Run — the database is created automatically on first launch
dotnet run --project MN-Barcode.WinForms
```

The database schema is created automatically on the first run
(`Database.EnsureCreated()`), so no manual migration step is required.

**Default login:** `admin` / `admin123`

---

## ⚙️ Configuration

The database connection is **not hard-coded** — it is read from
`MN-Barcode.WinForms/appsettings.json`. Point it at any SQL Server instance:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=MNBarcodeLocal;Integrated Security=true;"
  }
}
```

For SQL Server Express, for example:

```json
"DefaultConnection": "Data Source=.\\SQLEXPRESS;Initial Catalog=MNBarcodeLocal;Integrated Security=True;TrustServerCertificate=True;"
```

---

## 📷 Using a Barcode Scanner

No special driver or SDK is required. ~95% of USB barcode scanners act as a
**keyboard wedge**: on scan, they "type" the barcode and press Enter. MN-Barcode
captures this input on the sale screen automatically. For testing without a
scanner, just type the barcode and press Enter.

## 🏷️ Barcode & Label Printing

Under **Stock Management → Barcode & Label** you can:

- Pick an existing product (or enter details manually)
- **Generate a unique barcode** — a store-internal 13-digit code (EAN-13 prefix `200` + check digit) that never collides with existing products
- See a **live preview** of the label (name, barcode, human-readable code, price)
- Adjust label size in millimetres and print any number of copies, tiled across the page
- Save the generated barcode back onto the selected product

Barcodes are rendered with a self-contained **Code 128** encoder (pure GDI+), so scanning
returns exactly the stored value.

---

## 🗺️ Roadmap

- [x] Barcode label generation & printing
- [ ] Receipt printer integration (ESC/POS)
- [ ] Backup & restore
- [ ] _(Optional, later)_ Cloud sync and a mobile edition

---

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for how to
report issues, propose features, and submit pull requests.

---

## 📄 License

Released under the **MIT License** — free to use, modify, distribute, and sell,
including for commercial purposes. See [LICENSE](LICENSE).

---

<details>
<summary>🇹🇷 <b>Türkçe Açıklama</b></summary>

<br>

**MN-Barcode**, bakkal, market, kasap, butik, kırtasiye gibi **küçük işletmeler** için
tasarlanmış, **internet gerektirmeden** (tamamen yerel/offline) çalışan barkodlu satış,
stok ve gider takip programıdır.

> **Felsefe:** İnternet kesilse bile kasa durmaz. Aylık bulut faturası yoktur.
> *"USB'ye tak, kur, sat."*

### Özellikler
- 🛒 **Hızlı Satış** — barkod okutma, hızlı ürün tuşları, manuel tutar
- 📷 **Barkod Okuyucu** — USB okuyucular (klavye taklidi) kutu odakta olmasa bile otomatik yakalanır
- 🔄 **İade Modu** — satışları negatif kayıtla güvenli iade
- 📦 **Stok Yönetimi** — ürün/kategori, kritik stok uyarısı
- 🏷️ **Barkod & Etiket Basma** — benzersiz barkod üretimi (Code 128), canlı önizleme, ayarlanabilir etiket boyutu, çoklu kopya — *harici kütüphane yok*
- 💸 **Gider Takibi**, 📊 **Raporlar & Dashboard**, 🧾 **Satış Geçmişi** (fişe göre renkli)
- 🛡️ **Stok asla eksiye düşmez** — satış her zaman tamamlanır, stok en fazla 0'a iner

### Kurulum
```bash
git clone https://github.com/EminAkplt/MN-Barcode.git
cd MN-Barcode
dotnet run --project MN-Barcode.WinForms
```
Veritabanı ilk açılışta otomatik oluşturulur. **Giriş:** `admin` / `admin123`

Veritabanı bağlantısı `MN-Barcode.WinForms/appsettings.json` dosyasından okunur.

### Lisans
**MIT** — herkes serbestçe kullanabilir, değiştirebilir, dağıtabilir ve **satabilir**.

</details>
