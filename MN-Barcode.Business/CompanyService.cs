using MN_Barcode.DataAccess;
using MN_Barcode.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MN_Barcode.Business
{
    public class CompanyService
    {

        private BarcodeContext _context;

        public CompanyService()
        {
            _context = new BarcodeContext();
        }

        // GİRİŞ KONTROLÜ(Login)
        // Eğer kullanıcı adı ve şifre doğruysa Şirket bilgisini döner, yanlışsa null döner.
        public Company Login(string companyCode, string password)
        {
            // Veritabanına git, bu koda ve şifreye sahip birisi var mı bak.
            var sirket = _context.Companies
                .FirstOrDefault(x => x.CompanyCode == companyCode && x.Password == password);

            return sirket;

        }
    }
}
