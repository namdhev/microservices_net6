using Mango.Services.OrderAPI.Models;

namespace Mango.Services.OrderAPI.Repository
{
    public interface IOrderRepository
    {
        Task<bool> AddOrder(OrderHeader orderHeader);
        Task UpdateOrderPaymentStatus(int orderHeaderId, bool paymentStatus);
    }
}
