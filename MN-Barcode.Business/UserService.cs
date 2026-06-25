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
        // KULLANICI GİRİŞİ (Login)
        // Kullanıcı adı + şifre eşleşirse kullanıcıyı, eşleşmezse null döner.
        public AppUser Login(string username, string password)
        {
            using var context = new BarcodeContext();

            // NOT (güvenlik): Şifre şu an düz metin karşılaştırılıyor. Satışa çıkmadan
            // önce şifrelerin hash'lenmesi önerilir (yol haritasına eklendi).
            var user = context.Users
                .FirstOrDefault(x => x.Username == username && x.Password == password);

            return user;
        }
    }
}
