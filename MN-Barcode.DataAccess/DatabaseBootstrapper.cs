using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MN_Barcode.DataAccess
{
    /// <summary>Veritabanı başlatma sonucunun durumu.</summary>
    public enum DatabaseStartupStatus
    {
        /// <summary>Veritabanı hazır, uygulama açılabilir.</summary>
        Ready,
        /// <summary>SQL Server LocalDB kurulu değil.</summary>
        LocalDbMissing,
        /// <summary>LocalDB kurulu ama bağlanılamadı.</summary>
        ConnectionFailed,
        /// <summary>Bağlantı kuruldu ama şema güncellenemedi.</summary>
        MigrationFailed
    }

    /// <summary>Başlatma sonucu — arayüze net mesaj verebilmek için.</summary>
    public sealed class DatabaseStartupResult
    {
        public DatabaseStartupStatus Status { get; init; }
        public string UserMessage { get; init; } = "";
        public string? TechnicalDetail { get; init; }
        public bool IsSuccess => Status == DatabaseStartupStatus.Ready;
    }

    /// <summary>
    /// Veritabanını uygulama açılışında kullanılabilir hale getirir.
    ///
    /// Üç işi yapar:
    ///  1. LocalDB örneği kapalıysa başlatır (boşta kalınca kendini kapatır).
    ///  2. Veritabanı daha önce EnsureCreated() ile oluşturulmuşsa migration
    ///     geçmişini geriye dönük işaretler (baseline) — aksi halde Migrate()
    ///     "tablo zaten var" hatası verir.
    ///  3. Bekleyen migration'ları uygular.
    ///
    /// Hata durumunda istisna fırlatmaz; çağıran tarafa kullanıcıya gösterilebilecek
    /// Türkçe bir mesaj döner. Sessizce yutmak yasak — müşteri neyin bozuk olduğunu görmeli.
    /// </summary>
    public static class DatabaseBootstrapper
    {
        private const string LocalDbInstance = "mssqllocaldb";

        public static DatabaseStartupResult Initialize()
        {
            try
            {
                using var context = new BarcodeContext();

                if (!TryConnect(context, out string? connectError))
                {
                    // LocalDB boşta kalınca kendini kapatır; başlatmayı dene.
                    if (TryStartLocalDb(out string? startError))
                    {
                        if (!TryConnect(context, out connectError))
                            return Fail(DatabaseStartupStatus.ConnectionFailed, connectError);
                    }
                    else
                    {
                        return Fail(
                            IsLocalDbToolMissing(startError) ? DatabaseStartupStatus.LocalDbMissing
                                                             : DatabaseStartupStatus.ConnectionFailed,
                            startError ?? connectError);
                    }
                }

                BaselineIfCreatedWithoutMigrations(context);

                var pending = context.Database.GetPendingMigrations().ToList();
                if (pending.Count > 0)
                    context.Database.Migrate();

                return new DatabaseStartupResult { Status = DatabaseStartupStatus.Ready };
            }
            catch (Exception ex)
            {
                return Fail(DatabaseStartupStatus.MigrationFailed, ex.ToString());
            }
        }

        private static bool TryConnect(BarcodeContext context, out string? error)
        {
            try
            {
                bool ok = context.Database.CanConnect();
                error = ok ? null : "Veritabanına bağlanılamadı.";
                return ok;
            }
            catch (Exception ex)
            {
                error = ex.ToString();
                return false;
            }
        }

        /// <summary>
        /// LocalDB örneğini başlatır. Örnek yoksa oluşturur.
        /// sqllocaldb.exe bulunamazsa LocalDB kurulu değildir.
        /// </summary>
        private static bool TryStartLocalDb(out string? error)
        {
            error = null;
            try
            {
                var (startCode, startOut) = RunSqlLocalDb($"start {LocalDbInstance}");
                if (startCode == 0) return true;

                // Örnek hiç yoksa oluşturup tekrar dene.
                var (createCode, createOut) = RunSqlLocalDb($"create {LocalDbInstance}");
                if (createCode != 0)
                {
                    error = string.IsNullOrWhiteSpace(createOut) ? startOut : createOut;
                    return false;
                }

                var (retryCode, retryOut) = RunSqlLocalDb($"start {LocalDbInstance}");
                if (retryCode == 0) return true;

                error = retryOut;
                return false;
            }
            catch (Exception ex)
            {
                error = ex.ToString();
                return false;
            }
        }

        private static (int ExitCode, string Output) RunSqlLocalDb(string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sqllocaldb.exe",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return (-1, "sqllocaldb.exe başlatılamadı.");

            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();

            // 30 saniye yeterli; LocalDB ilk başlatmada yavaş olabilir ama sonsuza dek beklemeyiz.
            if (!proc.WaitForExit(30_000))
            {
                try { proc.Kill(true); } catch { /* zaten kapanmış olabilir */ }
                return (-1, "sqllocaldb.exe yanıt vermedi (30 sn).");
            }

            return (proc.ExitCode, string.IsNullOrWhiteSpace(stderr) ? stdout : stderr);
        }

        private static bool IsLocalDbToolMissing(string? error)
        {
            if (string.IsNullOrEmpty(error)) return false;
            return error.Contains("sqllocaldb", StringComparison.OrdinalIgnoreCase)
                && (error.Contains("bulunamadı", StringComparison.OrdinalIgnoreCase)
                 || error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                 || error.Contains("cannot find", StringComparison.OrdinalIgnoreCase)
                 || error.Contains("The system cannot find the file", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Veritabanı EnsureCreated() ile oluşturulmuşsa __EFMigrationsHistory tablosu yoktur.
        /// Bu durumda tablolar zaten mevcut olduğu için Migrate() çakışır. Mevcut migration'ları
        /// "uygulanmış" olarak işaretleyerek (baseline) geçişi sorunsuz hale getiririz.
        /// </summary>
        private static void BaselineIfCreatedWithoutMigrations(BarcodeContext context)
        {
            var connection = (SqlConnection)context.Database.GetDbConnection();
            bool opened = false;

            try
            {
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                    opened = true;
                }

                if (TableExists(connection, "__EFMigrationsHistory")) return;   // zaten migration'lı
                if (!TableExists(connection, "Products")) return;               // boş veritabanı — Migrate() halleder

                // Şema var ama geçmiş yok: EnsureCreated ile oluşturulmuş.
                ExecuteNonQuery(connection, @"
                    CREATE TABLE [__EFMigrationsHistory] (
                        [MigrationId]    nvarchar(150) NOT NULL,
                        [ProductVersion] nvarchar(32)  NOT NULL,
                        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                    );");

                string productVersion = typeof(DbContext).Assembly.GetName().Version?.ToString() ?? "8.0.11";

                foreach (string migrationId in context.Database.GetMigrations())
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText =
                        "INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (@id,@ver);";
                    cmd.Parameters.AddWithValue("@id", migrationId);
                    cmd.Parameters.AddWithValue("@ver", productVersion);
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (opened && connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
        }

        private static bool TableExists(SqlConnection connection, string tableName)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @name;";
            cmd.Parameters.AddWithValue("@name", tableName);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static void ExecuteNonQuery(SqlConnection connection, string sql)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private static DatabaseStartupResult Fail(DatabaseStartupStatus status, string? detail)
        {
            string message = status switch
            {
                DatabaseStartupStatus.LocalDbMissing =>
                    "Veritabanı bileşeni (SQL Server Express LocalDB) bu bilgisayarda kurulu değil.\n\n" +
                    "Kurulum dosyası: SqlLocalDB.msi\n" +
                    "Kurduktan sonra programı yeniden başlatın.",

                DatabaseStartupStatus.ConnectionFailed =>
                    "Veritabanına bağlanılamadı.\n\n" +
                    "Bilgisayarı yeniden başlatmayı deneyin. Sorun sürerse teknik desteğe başvurun.",

                _ =>
                    "Veritabanı güncellenirken hata oluştu.\n\n" +
                    "Verileriniz güvende. Lütfen teknik desteğe başvurun."
            };

            return new DatabaseStartupResult
            {
                Status = status,
                UserMessage = message,
                TechnicalDetail = detail
            };
        }
    }
}
