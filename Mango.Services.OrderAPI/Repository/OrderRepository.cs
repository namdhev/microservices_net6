using Mango.Services.OrderAPI.DbContexts;
using Mango.Services.OrderAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.OrderAPI.Repository
{
    public class OrderRepository: IOrderRepository
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbContext;

        public OrderRepository(DbContextOptions<ApplicationDbContext> dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> AddOrder(OrderHeader orderHeader)
        {
            try
            {
                await using var _db = new ApplicationDbContext(_dbContext);
                _db.OrderHeaders.Add(orderHeader);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in Adding order, {0}", e.Message);
                return false;
            }
        }

        public async Task UpdateOrderPaymentStatus(int orderHeaderId, bool paymentStatus)
        {
            await using var _db = new ApplicationDbContext(_dbContext);
            var orderHeaderFromDb = await _db.OrderHeaders.FirstOrDefaultAsync(oh => oh.OrderHeaderId == orderHeaderId);
            if (orderHeaderFromDb != null)
            {
                orderHeaderFromDb.PaymentStatus = paymentStatus;
                await _db.SaveChangesAsync();
            }
        }
    }
}
