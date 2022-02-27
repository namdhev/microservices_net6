using Mango.Web.Models;
using Mango.Web.Services.IServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Mango.Web.Controllers
{
    public class CartController : Controller
    {
        private readonly ICouponService _couponService;
        private readonly ICartService _cartService;

        public CartController(ICouponService couponService, ICartService cartService)
        {
            _couponService = couponService;
            _cartService = cartService;
        }
        public async Task<IActionResult> CartIndex()
        {
            return View(await LoadCartDtoBasedOnLoggedInUser());
        }

        public async Task<IActionResult> Remove(int cartDetailsId)
        {
            var userId = User.Claims.Where(u => u.Type == "sub")?.FirstOrDefault()?.Value;
            var accessToken = await HttpContext.GetTokenAsync("access_token");

            var response = await _cartService.RemoveFromCartAsync<ResponseDto>(cartDetailsId, accessToken);
            
            if (response != null && response.IsSuccess)
                return RedirectToAction(nameof(CartIndex));

            return View();
        }

        [HttpPost]
        [ActionName("ApplyCoupon")]
        public async Task<IActionResult> ApplyCoupon(CartDto cartDto)
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");

            var response = await _cartService.ApplyCouponAsync<ResponseDto>(cartDto, accessToken);

            if (response != null && response.IsSuccess)
                return RedirectToAction(nameof(CartIndex));

            return View();
        }

        [HttpPost]
        [ActionName("RemoveCoupon")]
        public async Task<IActionResult> RemoveCoupon(CartDto cartDto)
        {
            var accessToken = await HttpContext.GetTokenAsync("access_token");

            var response = await _cartService.RemoveCouponAsync<ResponseDto>(cartDto.CartHeader.UserId, accessToken);

            if (response != null && response.IsSuccess)
                return RedirectToAction(nameof(CartIndex));

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            return View(await LoadCartDtoBasedOnLoggedInUser());
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(CartDto cartDto)
        {
            try
            {
                var accessToken = await HttpContext.GetTokenAsync("access_token");
                var response = await _cartService.CheckoutAsync<ResponseDto>(cartDto.CartHeader);
                return RedirectToAction(nameof(Confirmation));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return View(cartDto);
            }
        }

        public async Task<IActionResult> Confirmation()
        {
            return View(await LoadCartDtoBasedOnLoggedInUser());
        }

        private async Task<CartDto> LoadCartDtoBasedOnLoggedInUser()
        {
            var userId = User.Claims.Where(u => u.Type == "sub")?.FirstOrDefault()?.Value;
            var accessToken = await HttpContext.GetTokenAsync("access_token");

            var response = await _cartService.GetCartByUserIdAsync<ResponseDto>(userId, accessToken);
            CartDto cartDto = new();
            if (response != null && response.IsSuccess)
                cartDto = JsonConvert.DeserializeObject<CartDto>(Convert.ToString(response.Result));

            if (cartDto.CartHeader != null)
            {
                if (!string.IsNullOrEmpty(cartDto.CartHeader.CouponCode))
                {
                    var couponResponse = await _couponService.GetCoupon<ResponseDto>(cartDto.CartHeader.CouponCode, accessToken);
                    if (couponResponse != null && couponResponse.IsSuccess)
                    {
                        var couponObj = JsonConvert.DeserializeObject<CouponDto>(Convert.ToString(couponResponse.Result));
                        cartDto.CartHeader.DiscountTotal = couponObj.DiscountAmount;
                    }
                }
                foreach (var item in cartDto.CartDetails)
                {
                    cartDto.CartHeader.OrderTotal += (item.Product.Price * item.Count);
                }

                cartDto.CartHeader.OrderTotal -= cartDto.CartHeader.DiscountTotal;
            }
            return cartDto;
        }
    }
}
