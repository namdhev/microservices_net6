using Mango.Services.CouponAPI.Models.Dtos;

namespace Mango.Services.CouponAPI.Repository
{
    public interface ICouponRepository
    {
        Task<CouponDto> GetCouponByCode(string couponCode);

    }
}
