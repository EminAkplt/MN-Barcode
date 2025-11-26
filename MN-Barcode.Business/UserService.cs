using MN_Barcode.DataAccess;
using MN_Barcode.Entities;
using System.Linq;

namespace MN_Barcode.Business
{
    public class UserService
    {
        private BarcodeContext _context;

        public UserService()
        {
            _context = new BarcodeContext();
        }

        // KULLANICI GİRİŞİ (Login)
        public AppUser Login(string username, string password)
        {
            // Kullanıcılar tablosunda (Users) arama yap
            var user = _context.Users
                .FirstOrDefault(x => x.Username == username && x.Password == password);

            return user;
        }
    }
}