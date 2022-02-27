using System.Text;
using Newtonsoft.Json;
using Azure.Messaging.ServiceBus;
using Mango.Services.OrderAPI.Messages;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Repository;

#nullable disable
namespace Mango.Services.OrderAPI.Messaging
{
    public class AzureServiceBusConsumer: IAzureServiceBusConsumer
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IConfiguration _configuration;

        private readonly string _subConnStr;
        private readonly string _subTopicName;
        private readonly string _subSubscriptionName;
        
        private ServiceBusProcessor _checkoutProcessor;

        public AzureServiceBusConsumer(IOrderRepository orderRepository, IConfiguration configuration)
        {
            _orderRepository = orderRepository;
            _configuration = configuration;

            _subConnStr = _configuration.GetValue<string>("ServiceBusSettings:ServiceBusConn");
            _subTopicName = _configuration.GetValue<string>("ServiceBusSettings:TopicName");
            _subSubscriptionName = _configuration.GetValue<string>("ServiceBusSettings:SubscriptionName");

            var client = new ServiceBusClient(_subConnStr);
            _checkoutProcessor = client.CreateProcessor(_subTopicName, _subSubscriptionName);
        }

        public async Task Start()
        {
            _checkoutProcessor.ProcessMessageAsync += OnCheckoutMessageReceived;
            _checkoutProcessor.ProcessErrorAsync += OnCheckoutError;

            await _checkoutProcessor.StartProcessingAsync();
        }

        public async Task Stop()
        {
            await _checkoutProcessor.StopProcessingAsync();
            await _checkoutProcessor.DisposeAsync();
        }

        Task OnCheckoutError(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }

        private async Task OnCheckoutMessageReceived(ProcessMessageEventArgs args)
        {
            var message = args.Message;
            var body = Encoding.UTF8.GetString(message.Body);

            CheckoutHeaderDto checkoutHeaderDto = JsonConvert.DeserializeObject<CheckoutHeaderDto>(body);

            if (checkoutHeaderDto != null)
            {
                OrderHeader orderHeader = new()
                {
                    CouponCode = checkoutHeaderDto.CouponCode,
                    DiscountTotal = checkoutHeaderDto.DiscountTotal,
                    OrderTotal = checkoutHeaderDto.OrderTotal,
                    UserId = checkoutHeaderDto.UserId,
                    CardNumber = checkoutHeaderDto.CardNumber,
                    ExpiryMonthYear = checkoutHeaderDto.ExpiryMonthYear,
                    FirstName = checkoutHeaderDto.FirstName,
                    PickupDateTime = checkoutHeaderDto.PickupDateTime,
                    OrderDetails = new List<OrderDetails>(),
                    OrderTime = DateTime.Now,
                    CVV = checkoutHeaderDto.CVV,
                    CartTotalItems = checkoutHeaderDto.CartTotalItems,
                    Email = checkoutHeaderDto.Email,
                    LastName = checkoutHeaderDto.LastName,
                    Phone = checkoutHeaderDto.Phone
                };

                foreach (var detail in checkoutHeaderDto.CartDetails)
                {
                    OrderDetails orderDetails = new()
                    {
                        Count = detail.Count,
                        ProductId = detail.ProductId,
                        Price = detail.Product.Price,
                        ProductName = detail.Product.Name
                    };

                    orderHeader.CartTotalItems += detail.Count;
                    orderHeader.OrderDetails.Add(orderDetails);
                }

                await _orderRepository.AddOrder(orderHeader);
            }
        }
    }
}
