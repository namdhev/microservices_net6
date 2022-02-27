using Mango.Web.Models;

namespace Mango.Web.Services.IServices
{
    public interface ICouponService
    {
        Task<T> GetCoupon<T>(string couponCode, string token = null);
    }
}
