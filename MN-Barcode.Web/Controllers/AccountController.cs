using Microsoft.AspNetCore.Authentication; // Giriş/Çıkış işlemleri (SignInAsync) için ŞART
using Microsoft.AspNetCore.Mvc;            // Controller ve IActionResult için ŞART
using MN_Barcode.Business;                 // CompanyService'i tanımak için ŞART
using MN_Barcode.Entities;                 // Company (Şirket) nesnesini tanımak için ŞART
using System.Security.Claims;              // Kimlik kartı (Claim) oluşturmak için ŞART
using System.Collections.Generic;          // List<Claim> kullanmak için
using System.Threading.Tasks;

namespace MN_Barcode.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly CompanyService _companyService;
        public AccountController()
        {
            _companyService = new Business.CompanyService();
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Eğer zaten giriş yapmışsa direkt ana sayfaya at
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string companyCode, string password)
        {
            var sirket = _companyService.Login(companyCode, password);

            if (sirket != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, sirket.CompanyName),
                    new Claim("CompanyId", sirket.Id.ToString()),
                    new Claim("CompanyCode", sirket.CompanyCode)
                };

                var userIdentity = new ClaimsIdentity(claims, "CookieAuth");
                var principal = new ClaimsPrincipal(userIdentity);

                await HttpContext.SignInAsync("CookieAuth", principal);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Hatalı İşletme Kodu veya Şifre!";
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login");
        }
    }
}