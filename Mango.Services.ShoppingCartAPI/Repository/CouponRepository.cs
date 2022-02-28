using System.Text.Json.Serialization;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Newtonsoft.Json;

namespace Mango.Services.ShoppingCartAPI.Repository
{
    public class CouponRepository: ICouponRepository
    {
        private readonly HttpClient _httpClient;

        public CouponRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<CouponDto> GetCoupon(string couponCode)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/coupon/{couponCode}");
                var apiContent = await response.Content.ReadAsStringAsync();
                var res = JsonConvert.DeserializeObject<ResponseDto>(apiContent);
                return (res is {IsSuccess: true} ? JsonConvert.DeserializeObject<CouponDto>(Convert.ToString(res.Result)) : new CouponDto());
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in GetCoupon From shopping cart API, {0}", e.Message);
                return new CouponDto();
            }
        }
    }
}
