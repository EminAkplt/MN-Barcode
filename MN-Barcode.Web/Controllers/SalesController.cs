using Microsoft.AspNetCore.Mvc;
using MN_Barcode.Business; // ProductService buradan gelecek
using MN_Barcode.Web.Models; // ViewModel'ler buradan gelecek

namespace MN_Barcode.Web.Controllers
{
    public class SalesController : Controller
    {
        private readonly ProductService _productService;

        public SalesController()
        {
            // Ürün aramak için servisi hazırlıyoruz
            _productService = new ProductService();
        }

        // 1. SATIŞ EKRANINI AÇAN SAYFA
        public IActionResult Index()
        {
            return View();
        }

        // 2. BARKOD SORGULAMA (Javscript Buraya İstek Atacak)
        // Sayfa yenilenmeden arka planda çalışır (AJAX)
        [HttpGet]
        public IActionResult GetProduct(string barcode)
        {
            var product = _productService.GetByBarcode(barcode);

            if (product == null)
            {
                // Ürün yoksa JS'e "Bulamadım" mesajı dön
                return Json(new { success = false, message = "Ürün Bulunamadı!" });
            }

            // Ürün varsa bilgilerini paketleyip JS'e yolla
            return Json(new
            {
                success = true,
                productId = product.Id,
                name = product.Name,
                price = product.SellingPrice,
                barcode = product.Barcode
            });
        }
    }
}