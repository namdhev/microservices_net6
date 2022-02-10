using Mango.Web.Models;
using Mango.Web.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Mango.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductService _productService;

        public HomeController(ILogger<HomeController> logger, IProductService productService)
        {
            _logger = logger;
            _productService = productService;
        }

        public async Task<IActionResult> Index()
        {
            List<ProductDto> products = new ();
            var response = await _productService.GetAllProductsAsync<ResponseDto>("");
            if (response != null && response.IsSuccess)
                products = JsonConvert.DeserializeObject<List<ProductDto>>(Convert.ToString(response.Result));
            return View(products);
        }

        [Authorize]
        public async Task<IActionResult> Details(int productId)
        {
            ProductDto product = new ();
            var response = await _productService.GetProductByIdAsync<ResponseDto>(productId, "");
            if (response != null && response.IsSuccess)
                product = JsonConvert.DeserializeObject<ProductDto>(Convert.ToString(response.Result));
            return View(product);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Authorize]
        public async Task<IActionResult> Login()
        {            
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Logout()
        {
            return SignOut("Cookies", "oidc");
        }
    }
}