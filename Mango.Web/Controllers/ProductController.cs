using Mango.Web.Models;
using Mango.Web.Services.IServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Mango.Web.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService productService;

        public ProductController(IProductService _productService)
        {
            productService = _productService;
        }

        public async Task<IActionResult> ProductIndex()
        {
            List<ProductDto> products = new();
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            var response = await productService.GetAllProductsAsync<ResponseDto>(accessToken);
            if (response != null && response.IsSuccess == true)
            {
                products = JsonConvert.DeserializeObject<List<ProductDto>>(Convert.ToString(response.Result));
            }

            return View(products);
        }

        public async Task<IActionResult> ProductCreate()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProductCreate(ProductDto model)
        {
            if (ModelState.IsValid)
            {
                var accessToken = await HttpContext.GetTokenAsync("access_token");
                var response = await productService.CreateProductAsync<ResponseDto>(model, accessToken);
                if (response != null && response.IsSuccess == true)
                    return RedirectToAction(nameof(ProductIndex));
            }
            return View(model);
        }

        public async Task<IActionResult> ProductEdit(int ProductId)
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            var response  = await productService.GetProductByIdAsync<ResponseDto>(ProductId, accessToken);

            if (response != null && response.IsSuccess == true)
            {
                ProductDto productDto = JsonConvert.DeserializeObject<ProductDto>(Convert.ToString(response.Result));
                return View(productDto);
            }
           
            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProductEdit(ProductDto model)
        {
            if (ModelState.IsValid)
            {
                var accessToken = await HttpContext.GetTokenAsync("access_token");
                var response = await productService.UpdateProductAsync<ResponseDto>(model, accessToken);
                if (response != null && response.IsSuccess == true)
                    return RedirectToAction(nameof(ProductIndex));
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ProductDelete(int ProductId)
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");
            var response = await productService.GetProductByIdAsync<ResponseDto>(ProductId, accessToken);

            if (response != null && response.IsSuccess == true)
            {
                ProductDto productDto = JsonConvert.DeserializeObject<ProductDto>(Convert.ToString(response.Result));
                return View(productDto);
            }

            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ProductDelete(ProductDto model)
        {
            if (ModelState.IsValid)
            {
                var accessToken = await HttpContext.GetTokenAsync("access_token");
                var response = await productService.DeleteProductAsync<ResponseDto>(model.ProductId, accessToken);
                if (response != null && response.IsSuccess == true)
                    return RedirectToAction(nameof(ProductIndex));
            }
            return View(model);
        }
    }
}
