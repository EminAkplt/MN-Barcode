using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MN_Barcode.DataAccess
{
    /// <summary>Veritabanı başlatma sonucunun durumu.</summary>
    public enum DatabaseStartupStatus
    {
        /// <summary>Veritabanı hazır, uygulama açılabilir.</summary>
        Ready,
        /// <summary>SQL Server LocalDB bu bilgisayarda kurulu değil.</summary>
        LocalDbMissing,
        /// <summary>LocalDB kurulu ama bağlantı kurulamadı.</summary>
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
    /// TASARIM İLKESİ: Kullanıcıya komut satırında bir şeyler yazdırmak çözüm değildir.
    /// Program kendi kendini onarabilmeli. Bu sınıf şu sırayı izler:
    ///
    ///   1. Bağlanmayı dene.
    ///   2. Olmazsa: LocalDB kurulu mu? Değilse net söyle, uğraşma.
    ///   3. Kurulu ama bağlanamıyorsa: örneği başlat (yoksa oluştur).
    ///   4. Hâlâ olmazsa: örnek bozuk olabilir — SIL, YENİDEN OLUŞTUR, BAŞLAT.
    ///      (Yalnızca veri dosyası yoksa. Veri varsa asla silinmez.)
    ///   5. Şema: EnsureCreated ile oluşmuş eski veritabanlarını migration
    ///      geçmişine işaretle (baseline), sonra bekleyen migration'ları uygula.
    ///
    /// Her adımın çıktısı biriktirilir; başarısızlıkta destek ekibine gidecek
    /// teknik rapor kendiliğinden hazırdır. Sessizce yutma yoktur.
    /// </summary>
    public static class DatabaseBootstrapper
    {
        private const string LocalDbInstance = "MSSQLLocalDB";
        private const string SqlLocalDbExe   = "sqllocaldb.exe";

        /// <summary>
        /// EnsureCreated() döneminde üretilen şemanın karşılığı olan SON migration.
        ///
        /// Eski sürümler veritabanını EnsureCreated() ile oluşturuyordu; bu yöntem
        /// __EFMigrationsHistory tablosunu hiç yazmaz. Böyle bir veritabanını
        /// migration düzenine almak için o güne kadarki migration'ları "uygulanmış"
        /// işaretlemek (baseline) gerekir.
        ///
        /// BU SABİT ASLA GÜNCELLENMEMELİDİR. Yeni migration eklendiğinde buraya
        /// dokunulursa o migration mevcut müşterilerde SESSİZCE ATLANIR: şema
        /// güncellenmez ama uygulama "her şey hazır" der. Bu, bugün fark edilen
        /// ve düzeltilen hatanın ta kendisiydi — baseline tüm migration'ları
        /// işaretlediği için şifre sütunu genişlemiyor ve barkod tekil indeksi
        /// oluşmuyordu.
        /// </summary>
        private const string EnsureCreatedDonemiSonMigration = "20260712094552_AddProductUnitAndEnumConversions";

        /// <summary>Adım adım ne olduğunun kaydı — hata raporuna eklenir.</summary>
        private static readonly StringBuilder _gunluk = new StringBuilder();

        private static void Not(string satir)
        {
            _gunluk.Append(DateTime.Now.ToString("HH:mm:ss.fff")).Append("  ").AppendLine(satir);
        }

        public static DatabaseStartupResult Initialize()
        {
            _gunluk.Clear();
            Not($"Baslatiliyor. Kullanici: {Environment.UserDomainName}\\{Environment.UserName}");

            try
            {
                using var context = new BarcodeContext();
                Not($"Baglanti cumlesi: {GizleHassas(context.Database.GetConnectionString())}");

                // ── 1. Doğrudan bağlanmayı dene ──────────────────────
                if (TryConnect(context, out string? hata))
                {
                    Not("Baglanti basarili (ilk denemede).");
                    return SemayiHazirla(context);
                }
                Not($"Ilk baglanti basarisiz: {TekSatir(hata)}");

                // ── 2. LocalDB kurulu mu? ────────────────────────────
                string? localDbYolu = SqlLocalDbYolunuBul();
                if (localDbYolu == null)
                {
                    Not("sqllocaldb.exe bulunamadi -> LocalDB kurulu degil.");
                    return Fail(DatabaseStartupStatus.LocalDbMissing, hata);
                }
                Not($"LocalDB bulundu: {localDbYolu}");

                // ── 3. Örneği başlat (yoksa oluştur) ─────────────────
                OrnegiBaslat(localDbYolu);
                if (TryConnect(context, out hata))
                {
                    Not("Baglanti basarili (ornek baslatildiktan sonra).");
                    return SemayiHazirla(context);
                }
                Not($"Ornek baslatildi ama baglanti yine basarisiz: {TekSatir(hata)}");

                // ── 4. Kendi kendini onar: örneği sil ve yeniden kur ──
                // Bozuk veya surum uyusmazligi olan ornekler baska turlu duzelmiyor.
                // GUVENLIK: veri dosyasi varsa ASLA silme - veriye erisim kaybolur.
                string? veriDosyasi = OlasiVeriDosyasi(context);
                bool veriVar = veriDosyasi != null && File.Exists(veriDosyasi);
                Not($"Veri dosyasi: {veriDosyasi ?? "bilinmiyor"} (mevcut: {veriVar})");

                if (veriVar)
                {
                    Not("Veri dosyasi mevcut -> ornek SILINMEYECEK (veri kaybi riski).");
                    return Fail(DatabaseStartupStatus.ConnectionFailed, hata);
                }

                Not("Veri yok -> ornek sifirdan yeniden olusturuluyor.");
                OrnegiSifirla(localDbYolu);

                if (TryConnect(context, out hata))
                {
                    Not("Baglanti basarili (ornek yeniden olusturuldu).");
                    return SemayiHazirla(context);
                }

                Not($"Yeniden olusturma sonrasi da baglanti kurulamadi: {TekSatir(hata)}");
                return Fail(DatabaseStartupStatus.ConnectionFailed, hata);
            }
            catch (Exception ex)
            {
                Not($"BEKLENMEYEN HATA: {ex}");
                return Fail(DatabaseStartupStatus.MigrationFailed, ex.ToString());
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Şema
        // ─────────────────────────────────────────────────────────────

        private static DatabaseStartupResult SemayiHazirla(BarcodeContext context)
        {
            try
            {
                BaselineIfCreatedWithoutMigrations(context);

                var bekleyen = context.Database.GetPendingMigrations().ToList();
                if (bekleyen.Count > 0)
                {
                    Not($"Bekleyen migration ({bekleyen.Count}): {string.Join(", ", bekleyen)}");
                    context.Database.Migrate();
                    Not("Migration'lar uygulandi.");
                }
                else
                {
                    Not("Bekleyen migration yok.");
                }

                return new DatabaseStartupResult { Status = DatabaseStartupStatus.Ready };
            }
            catch (Exception ex)
            {
                Not($"Sema hazirlanamadi: {ex}");
                return Fail(DatabaseStartupStatus.MigrationFailed, ex.ToString());
            }
        }

        /// <summary>
        /// Veritabanı EnsureCreated() ile oluşturulmuşsa __EFMigrationsHistory tablosu yoktur.
        /// Tablolar zaten mevcut olduğu için Migrate() çakışır. Mevcut migration'ları
        /// "uygulanmış" işaretleyerek (baseline) geçişi sorunsuz hale getiririz.
        /// </summary>
        private static void BaselineIfCreatedWithoutMigrations(BarcodeContext context)
        {
            var connection = (SqlConnection)context.Database.GetDbConnection();
            bool bizActik = false;

            try
            {
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                    bizActik = true;
                }

                if (TabloVarMi(connection, "__EFMigrationsHistory")) return;   // zaten migration'lı
                if (!TabloVarMi(connection, "Products")) return;               // boş veritabanı — Migrate() halleder

                Not("Sema var ama migration gecmisi yok -> baseline uygulaniyor.");

                Calistir(connection, @"
                    CREATE TABLE [__EFMigrationsHistory] (
                        [MigrationId]    nvarchar(150) NOT NULL,
                        [ProductVersion] nvarchar(32)  NOT NULL,
                        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                    );");

                string surum = typeof(DbContext).Assembly.GetName().Version?.ToString() ?? "8.0.11";
                int n = 0;
                bool sinirGecildi = false;

                // YALNIZCA EnsureCreated döneminin migration'ları işaretlenir.
                // Sonrakiler işaretlenmez ki Migrate() onları gerçekten uygulasın.
                // Migration'lar tarih önekli olduğu için GetMigrations() sıralı döner.
                foreach (string id in context.Database.GetMigrations())
                {
                    if (sinirGecildi)
                    {
                        Not($"Baseline disi (Migrate uygulayacak): {id}");
                        continue;
                    }

                    using var cmd = connection.CreateCommand();
                    cmd.CommandText =
                        "INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (@id,@ver);";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@ver", surum);
                    cmd.ExecuteNonQuery();
                    n++;

                    if (string.Equals(id, EnsureCreatedDonemiSonMigration, StringComparison.OrdinalIgnoreCase))
                        sinirGecildi = true;
                }

                if (!sinirGecildi)
                {
                    // Sinir migration'i bulunamadi: ya yeniden adlandirildi ya da silindi.
                    // Bu durumda TUM migration'lar isaretlenmis olur ve sonrakiler
                    // sessizce atlanir. Kayda geciyoruz ki fark edilsin.
                    Not($"UYARI: sinir migration bulunamadi ({EnsureCreatedDonemiSonMigration}). " +
                        "Tum migration'lar baseline'a alindi; yeni semalar uygulanmayabilir.");
                }

                Not($"Baseline tamam ({n} migration isaretlendi).");
            }
            finally
            {
                if (bizActik && connection.State == System.Data.ConnectionState.Open)
                    connection.Close();
            }
        }

        // ─────────────────────────────────────────────────────────────
        // LocalDB yönetimi
        // ─────────────────────────────────────────────────────────────

        private static bool TryConnect(BarcodeContext context, out string? hata)
        {
            try
            {
                // CanConnect istisnayı yutar; gerçek sebebi görmek için doğrudan açıyoruz.
                var conn = context.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open) conn.Open();
                conn.Close();
                hata = null;
                return true;
            }
            catch (Exception ex)
            {
                hata = ex.Message + (ex.InnerException != null ? " | Ic: " + ex.InnerException.Message : "");
                return false;
            }
        }

        /// <summary>
        /// sqllocaldb.exe'nin tam yolunu bulur; yoksa null.
        /// Hata metni ayrıştırmak güvenilir değil (mesajlar Windows diline göre
        /// değişir ve dosya adını içermeyebilir), bu yüzden diskte aranır.
        /// </summary>
        private static string? SqlLocalDbYolunuBul()
        {
            try
            {
                string? path = Environment.GetEnvironmentVariable("PATH");
                if (!string.IsNullOrEmpty(path))
                {
                    foreach (string klasor in path.Split(Path.PathSeparator))
                    {
                        if (string.IsNullOrWhiteSpace(klasor)) continue;
                        try
                        {
                            string aday = Path.Combine(klasor.Trim(), SqlLocalDbExe);
                            if (File.Exists(aday)) return aday;
                        }
                        catch { /* geçersiz PATH girdisi */ }
                    }
                }
            }
            catch { /* PATH okunamadı */ }

            // PATH'te yoksa standart kurulum konumlarına bak (kurulum sonrası
            // PATH yenilenmeden program açılmış olabilir).
            foreach (string kok in new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            })
            {
                if (string.IsNullOrEmpty(kok)) continue;
                try
                {
                    string temel = Path.Combine(kok, "Microsoft SQL Server");
                    if (!Directory.Exists(temel)) continue;

                    // En yeni sürümü tercih et (klasör adları sayısal: 150, 160...)
                    var surumler = Directory.GetDirectories(temel)
                        .OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase);

                    foreach (string surum in surumler)
                    {
                        string aday = Path.Combine(surum, "Tools", "Binn", SqlLocalDbExe);
                        if (File.Exists(aday)) return aday;
                    }
                }
                catch { /* erişim yok */ }
            }

            return null;
        }

        private static void OrnegiBaslat(string exe)
        {
            var (kod, cikti) = Calistir(exe, $"start {LocalDbInstance}");
            Not($"start -> kod {kod}: {TekSatir(cikti)}");
            if (kod == 0) return;

            var (kod2, cikti2) = Calistir(exe, $"create {LocalDbInstance}");
            Not($"create -> kod {kod2}: {TekSatir(cikti2)}");

            var (kod3, cikti3) = Calistir(exe, $"start {LocalDbInstance}");
            Not($"start (2) -> kod {kod3}: {TekSatir(cikti3)}");
        }

        /// <summary>
        /// Örneği tamamen kaldırıp yeniden kurar. Bozuk veya sürüm uyuşmazlığı olan
        /// örnekleri düzeltir. Çağıran taraf veri dosyası olmadığını doğrulamalıdır.
        /// </summary>
        private static void OrnegiSifirla(string exe)
        {
            var (k1, c1) = Calistir(exe, $"stop {LocalDbInstance} -k");
            Not($"stop -> kod {k1}: {TekSatir(c1)}");

            var (k2, c2) = Calistir(exe, $"delete {LocalDbInstance}");
            Not($"delete -> kod {k2}: {TekSatir(c2)}");

            var (k3, c3) = Calistir(exe, $"create {LocalDbInstance}");
            Not($"create -> kod {k3}: {TekSatir(c3)}");

            var (k4, c4) = Calistir(exe, $"start {LocalDbInstance}");
            Not($"start -> kod {k4}: {TekSatir(c4)}");
        }

        private static (int Kod, string Cikti) Calistir(string exe, string argumanlar)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = argumanlar,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return (-1, "Islem baslatilamadi.");

                string cikti = proc.StandardOutput.ReadToEnd();
                string hata  = proc.StandardError.ReadToEnd();

                if (!proc.WaitForExit(30_000))
                {
                    try { proc.Kill(true); } catch { }
                    return (-1, "sqllocaldb yanit vermedi (30 sn).");
                }

                return (proc.ExitCode, string.IsNullOrWhiteSpace(hata) ? cikti : hata);
            }
            catch (Exception ex)
            {
                return (-1, ex.Message);
            }
        }

        /// <summary>
        /// LocalDB'nin veritabanı dosyasını nerede oluşturacağını tahmin eder.
        /// Varsayılan konum: %USERPROFILE%\&lt;KatalogAdi&gt;.mdf
        /// </summary>
        private static string? OlasiVeriDosyasi(BarcodeContext context)
        {
            try
            {
                string katalog = context.Database.GetDbConnection().Database;
                if (string.IsNullOrWhiteSpace(katalog)) return null;
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    katalog + ".mdf");
            }
            catch
            {
                return null;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Yardımcılar
        // ─────────────────────────────────────────────────────────────

        private static bool TabloVarMi(SqlConnection connection, string tablo)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @ad;";
            cmd.Parameters.AddWithValue("@ad", tablo);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static void Calistir(SqlConnection connection, string sql)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private static string TekSatir(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "(bos)";
            return s.Replace("\r", " ").Replace("\n", " ").Trim();
        }

        /// <summary>Bağlantı cümlesindeki parolayı raporda göstermez.</summary>
        private static string GizleHassas(string? cumle)
        {
            if (string.IsNullOrEmpty(cumle)) return "(yok)";
            return System.Text.RegularExpressions.Regex.Replace(
                cumle, @"(password|pwd)\s*=\s*[^;]*", "$1=***",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        private static DatabaseStartupResult Fail(DatabaseStartupStatus durum, string? sonHata)
        {
            string mesaj = durum switch
            {
                DatabaseStartupStatus.LocalDbMissing =>
                    "Veritabanı bileşeni bu bilgisayarda kurulu değil.\n\n" +
                    "Kurulması gereken: SQL Server Express LocalDB (SqlLocalDB.msi)\n\n" +
                    "Kurulum bittikten sonra programı yeniden başlatın.",

                DatabaseStartupStatus.ConnectionFailed =>
                    "Veritabanı bileşeni kurulu ancak başlatılamadı.\n\n" +
                    "Program onarmayı denedi fakat başaramadı.\n" +
                    "Aşağıdaki teknik raporu destek ekibine gönderin.",

                _ =>
                    "Veritabanı güncellenirken hata oluştu.\n\n" +
                    "Verileriniz güvende. Aşağıdaki teknik raporu destek ekibine gönderin."
            };

            var rapor = new StringBuilder();
            rapor.AppendLine("── ISLEM KAYDI ──");
            rapor.Append(_gunluk);
            if (!string.IsNullOrWhiteSpace(sonHata))
            {
                rapor.AppendLine("── SON HATA ──");
                rapor.AppendLine(sonHata);
            }

            return new DatabaseStartupResult
            {
                Status = durum,
                UserMessage = mesaj,
                TechnicalDetail = rapor.ToString()
            };
        }
    }
}
