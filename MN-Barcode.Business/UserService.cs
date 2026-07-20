using MN_Barcode.DataAccess;
using MN_Barcode.Entities;
using System.Linq;

namespace MN_Barcode.Business
{
    /// <summary>
    /// Kullanıcı (giriş/login) iş mantığı servisi.
    /// Her metot kendi kısa ömürlü context'ini kullanır (bkz. CategoryService notu).
    /// </summary>
    public class UserService
    {
        /// <summary>
        /// Kullanıcı girişi. Eşleşirse kullanıcıyı, eşleşmezse null döner.
        ///
        /// Şifre artık SQL sorgusunda karşılaştırılmıyor: önce kullanıcı adıyla kayıt
        /// çekilir, sonra şifre PBKDF2 ile doğrulanır. Eski kurulumlardaki düz metin
        /// şifreler de kabul edilir ve ilk başarılı girişte sessizce hash'e yükseltilir —
        /// böylece hiç kimse kilitlenmeden geçiş tamamlanır.
        /// </summary>
        public AppUser Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || password == null) return null;

            using var context = new BarcodeContext();

            var user = context.Users.FirstOrDefault(x => x.Username == username);
            if (user == null) return null;

            if (!PasswordHasher.Verify(password, user.Password)) return null;

            // Eski düz metin kaydı hash'e çevir.
            if (PasswordHasher.NeedsUpgrade(user.Password))
            {
                try
                {
                    user.Password = PasswordHasher.Hash(password);
                    context.SaveChanges();
                }
                catch
                {
                    // Yükseltme başarısız olsa bile giriş engellenmemeli;
                    // bir sonraki girişte tekrar denenir.
                }
            }

            return user;
        }

        /// <summary>Kullanıcının şifresini değiştirir (hash'lenerek saklanır).</summary>
        public bool SifreDegistir(int kullaniciId, string yeniSifre)
        {
            if (string.IsNullOrWhiteSpace(yeniSifre)) return false;

            using var context = new BarcodeContext();
            var user = context.Users.Find(kullaniciId);
            if (user == null) return false;

            user.Password = PasswordHasher.Hash(yeniSifre);
            context.SaveChanges();
            return true;
        }
    }
}
