using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MN_Barcode.DataAccess.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Users.Password sütununu 20'den 200 karaktere genişletir.
    ///
    /// PBKDF2 hash biçimi ("pbkdf2$tur$tuz$ozet") yaklaşık 90 karakter tutar;
    /// 20 karakterlik eski sınır hash'i keser ve kimse giriş yapamaz hale gelirdi.
    /// Genişletme veri kaybı yaratmaz: mevcut düz metin şifreler olduğu gibi kalır
    /// ve ilk başarılı girişte sessizce hash'e yükseltilir.
    ///
    /// DİKKAT: Down yönü gerçekten veri kaybettirir — 200 karakterlik hash'ler
    /// 20 karaktere sığmaz. Geri alma yalnızca henüz hash'e geçilmemiş bir
    /// veritabanında güvenlidir.
    /// </summary>
    public partial class WidenPasswordForHashing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Password",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Password",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);
        }
    }
}
