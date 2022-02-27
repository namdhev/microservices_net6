using Mango.Web.Models;
using Mango.Web.Services.IServices;

namespace Mango.Web.Services
{
    public class CartService : BaseService, ICartService
    {
        private readonly IHttpClientFactory _clientFactory;

        public CartService(IHttpClientFactory clientFactory) : base(clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<T> GetCartByUserIdAsync<T>(string userId, string token = null)
        {
            return await SendAsync<T>(new ApiRequest()
            {
                apiType = SD.ApiType.GET,
                Url = $"{SD.ShoppingCartAPIBase}api/cart/{userId}",
                Token = token
            });
        }

        public async Task<T> AddToCartAsync<T>(CartDto cartDto, string token = null)
        {
            return await SendAsync<T>(new ApiRequest()
            {
                apiType = SD.ApiType.POST,
                Data = cartDto,
                Url = $"{SD.ShoppingCartAPIBase}api/cart/createCart",
                Token = token
            });
        }

        public async Task<T> UpdateCartAsync<T>(CartDto cartDto, string token = null)
        {
            return await SendAsync<T>(new ApiRequest()
            {
                apiType = SD.ApiType.POST,
                Data = cartDto,
                Url = $"{SD.ShoppingCartAPIBase}api/cart/updateCart",
                Token = token
            });
        }

        public async Task<T> RemoveFromCartAsync<T>(int cartDetailsId, string token = null)
        {
            return await SendAsync<T>(new ApiRequest()
            {
                apiType = SD.ApiType.POST,
                Data = cartDetailsId,
                Url = $"{SD.ShoppingCartAPIBase}api/cart/removeFromCart",
                Token = token
            });
        }

        public async Task<T> ApplyCouponAsync<T>(CartDto cartDto, string token = null)
        {
            return await SendAsync<T>(new ApiRequest()
            {
                apiType = SD.ApiType.POST,
                Data = cartDto,
                Url = $"{SD.ShoppingCartAPIBase}api/cart/applyCouponCode",
                Token = token
            });
        }

        public async Task<T> RemoveCouponAsync<T>(string userId, string token = null)
        {
            return await SendAsync<T>(new ApiRequest()
            {
                apiType = SD.ApiType.POST,
                Data = userId,
                Url = $"{SD.ShoppingCartAPIBase}api/cart/removeCouponCode",
                Token = token
            });
        }

        public async Task<T> CheckoutAsync<T>(CartHeaderDto cartHeaderDto, string token = null)
        {
            return await SendAsync<T>(new ApiRequest()
            {
                apiType = SD.ApiType.POST,
                Data = cartHeaderDto,
                Url = $"{SD.ShoppingCartAPIBase}api/cart/checkout",
                Token = token
            });
        }

        public async Task<T> ClearCartAsync<T>(string userId, string token = null)
        {
            return await SendAsync<T>(new ApiRequest()
            {
                apiType = SD.ApiType.POST,
                Data = userId,
                Url = $"{SD.ShoppingCartAPIBase}api/cart/clearCart",
                Token = token
            });
        }
    }
}
