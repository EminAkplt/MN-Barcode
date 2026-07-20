using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MN_Barcode.DataAccess.Migrations
{
    /// <summary>
    /// Kasadaki hızı ve veri doğruluğunu belirleyen indeksleri ekler.
    ///
    /// - Products.Barcode  : TEKİL. Her okutmada sorgulanır. İndekssiz her okutma
    ///                       tam tablo taramasıydı. Tekillik olmadığı için aynı
    ///                       barkod iki üründe bulunabiliyor ve FirstOrDefault
    ///                       rastgele birini döndürdüğünden YANLIŞ ÜRÜN satılıyordu.
    /// - Sales.CreatedDate : Ana Sayfa, Satış Geçmişi, İadeler ve raporlar bu
    ///                       sütuna göre süzüyor.
    /// - Sales.TransactionCode : Geçmiş/iade ekranları fişleri buna göre grupluyor.
    /// - Expenses.ExpenseDate  : Gider listesi ve günlük özet bu sütuna göre süzüyor.
    /// </summary>
    public partial class AddIndexesAndMoneyPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ──────────────────────────────────────────────────────────────
            // ÖNCE TEMİZLİK — yoksa tekil indeks oluşturulamaz
            //
            // Barkod sütununda bugüne kadar tekillik kuralı yoktu; mevcut bir
            // müşteri veritabanında aynı barkodu taşıyan birden fazla ürün
            // bulunabilir. Böyle bir durumda CREATE UNIQUE INDEX hata verir,
            // migration yarıda kalır ve güncelleme sonrası program hiç açılmaz.
            // ──────────────────────────────────────────────────────────────

            // 1) Boş/boşluk barkodları NULL yap. Boş metin tekillik kuralına
            //    takılır, NULL takılmaz — bu yüzden normalleştiriyoruz.
            migrationBuilder.Sql(@"
                UPDATE Products
                SET Barcode = NULL
                WHERE Barcode IS NOT NULL AND LTRIM(RTRIM(Barcode)) = '';");

            // 2) Aynı barkodu taşıyanlardan en eskisi (en küçük Id) barkodunu korur;
            //    diğerleri izlenebilir bir son ek alır. Veri SİLİNMEZ — mağaza
            //    sahibi listede görüp hangisinin doğru olduğuna kendisi karar verir.
            //    Sütun sınırı 50 karakter olduğu için gövde 30'a kırpılır.
            migrationBuilder.Sql(@"
                UPDATE p
                SET Barcode = LEFT(p.Barcode, 30) + '-KOPYA' + CAST(p.Id AS varchar(10))
                FROM Products p
                WHERE p.Barcode IS NOT NULL
                  AND EXISTS (
                        SELECT 1 FROM Products q
                        WHERE q.Barcode = p.Barcode AND q.Id < p.Id
                  );");

            // ── İndeksler ─────────────────────────────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode",
                unique: true,
                filter: "[Barcode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CreatedDate",
                table: "Sales",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TransactionCode",
                table: "Sales",
                column: "TransactionCode");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ExpenseDate",
                table: "Expenses",
                column: "ExpenseDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Products_Barcode", table: "Products");
            migrationBuilder.DropIndex(name: "IX_Sales_CreatedDate", table: "Sales");
            migrationBuilder.DropIndex(name: "IX_Sales_TransactionCode", table: "Sales");
            migrationBuilder.DropIndex(name: "IX_Expenses_ExpenseDate", table: "Expenses");

            // Not: barkod temizliği geri alınamaz (hangi kaydın hangi barkoda
            // ait olduğu bilgisi kaybolur). Geri alma yalnızca indeksleri kaldırır.
        }
    }
}
