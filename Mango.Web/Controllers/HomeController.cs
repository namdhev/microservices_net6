using Mango.Web.Models;
using Mango.Web.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;

namespace Mango.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICartService _cartService;

        public HomeController(IProductService productService, ICartService cartService)
        {
            _productService = productService;
            _cartService = cartService;
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

        [Authorize][HttpPost][ActionName("Details")]
        public async Task<IActionResult> DetailsPost(ProductDto productDto)
        {
            CartDto cartDto = new()
            {
                CartHeader = new CartHeaderDto()
                {
                    UserId = User.Claims.Where(u => u.Type == "sub")?.FirstOrDefault()?.Value
                }
            };

            CartDetailsDto cartDetailsDto = new()
            {
                Count = productDto.Count,
                ProductId = productDto.ProductId
            };

            // without need for saving product details in hidden field
            var response = await _productService.GetProductByIdAsync<ResponseDto>(productDto.ProductId, "");
            if (response != null && response.IsSuccess)
                cartDetailsDto.Product = JsonConvert.DeserializeObject<ProductDto>(Convert.ToString(response.Result));

            List<CartDetailsDto> cartDetailsDtoList = new() {cartDetailsDto};
            cartDto.CartDetails = cartDetailsDtoList;

            var accessToken = await HttpContext.GetTokenAsync("access_token");
            var addToCartResponse = await _cartService.AddToCartAsync<ResponseDto>(cartDto, accessToken);
            if (addToCartResponse != null && addToCartResponse.IsSuccess)
                return RedirectToAction(nameof(Index));

            return View(productDto);
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
        public Task<IActionResult> Login()
        {            
            return Task.FromResult<IActionResult>(RedirectToAction(nameof(Index)));
        }

        public IActionResult Logout()
        {
            return SignOut("Cookies", "oidc");
        }
    }
}