using System;
using System.Security.Cryptography;

namespace MN_Barcode.Business
{
    /// <summary>
    /// Şifre saklama ve doğrulama.
    ///
    /// NEDEN: Şifreler veritabanında düz metin duruyordu. Veritabanı dosyasını
    /// eline geçiren (ya da yedeği kopyalayan) herkes tüm şifreleri okuyabiliyordu.
    /// Ticari bir üründe bu kabul edilemez.
    ///
    /// YÖNTEM: PBKDF2-SHA256, kayıt başına rastgele tuz (salt), 100.000 tur.
    /// Saklama biçimi tek bir metin alanında:
    ///     pbkdf2$&lt;tur&gt;$&lt;base64-tuz&gt;$&lt;base64-özet&gt;
    ///
    /// GERİYE UYUMLULUK: Mevcut kurulumlarda şifreler düz metindir. Bu sınıf
    /// düz metin değerleri de doğrular (IsHashed=false); çağıran taraf başarılı
    /// doğrulamadan sonra kaydı hash'e yükseltir. Böylece kimse kilitlenmez ve
    /// geçiş kullanıcının haberi olmadan tamamlanır.
    /// </summary>
    public static class PasswordHasher
    {
        private const string Prefix = "pbkdf2";
        private const int Iterations = 100_000;
        private const int SaltBytes = 16;
        private const int HashBytes = 32;

        /// <summary>Şifreyi saklanabilir hash metnine çevirir.</summary>
        public static string Hash(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            byte[] salt = RandomNumberGenerator.GetBytes(SaltBytes);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, Iterations, HashAlgorithmName.SHA256, HashBytes);

            return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        /// <summary>Saklanan değerin hash biçiminde olup olmadığını söyler.</summary>
        public static bool IsHashed(string stored)
        {
            return !string.IsNullOrEmpty(stored) && stored.StartsWith(Prefix + "$", StringComparison.Ordinal);
        }

        /// <summary>
        /// Girilen şifrenin saklanan değerle eşleşip eşleşmediğini söyler.
        /// Saklanan değer düz metinse (eski kurulum) doğrudan karşılaştırır.
        /// </summary>
        public static bool Verify(string password, string stored)
        {
            if (stored == null) return false;
            if (password == null) return false;

            if (!IsHashed(stored))
            {
                // Eski kayıt: düz metin. Sabit süreli karşılaştırma kullanılır ki
                // yanıt süresinden şifre uzunluğu/içeriği çıkarılamasın.
                return SabitSureliEsit(password, stored);
            }

            try
            {
                string[] parca = stored.Split('$');
                if (parca.Length != 4) return false;

                int tur = int.Parse(parca[1]);
                byte[] salt = Convert.FromBase64String(parca[2]);
                byte[] beklenen = Convert.FromBase64String(parca[3]);

                byte[] hesaplanan = Rfc2898DeriveBytes.Pbkdf2(
                    password, salt, tur, HashAlgorithmName.SHA256, beklenen.Length);

                return CryptographicOperations.FixedTimeEquals(hesaplanan, beklenen);
            }
            catch
            {
                // Bozuk kayıt — doğrulama başarısız sayılır.
                return false;
            }
        }

        /// <summary>
        /// Doğrulama başarılıysa kaydın hash'e yükseltilmesi gerekiyor mu?
        /// Eski düz metin kayıtlar ilk başarılı girişte sessizce hash'lenir.
        /// </summary>
        public static bool NeedsUpgrade(string stored)
        {
            return !IsHashed(stored);
        }

        private static bool SabitSureliEsit(string a, string b)
        {
            byte[] ba = System.Text.Encoding.UTF8.GetBytes(a);
            byte[] bb = System.Text.Encoding.UTF8.GetBytes(b);
            return CryptographicOperations.FixedTimeEquals(
                System.Security.Cryptography.SHA256.HashData(ba),
                System.Security.Cryptography.SHA256.HashData(bb));
        }
    }
}
