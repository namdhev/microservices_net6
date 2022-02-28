using System.Text;
using Newtonsoft.Json;
using Azure.Messaging.ServiceBus;
using Mango.MessageBus;
using Mango.Services.OrderAPI.Messages;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Repository;

#nullable disable
namespace Mango.Services.OrderAPI.Messaging
{
    public class AzureServiceBusConsumer: IAzureServiceBusConsumer
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IMessageBus _messageBus;

        private readonly string _subConnStr;
        private readonly string _checkoutQueueName;
        private readonly string _checkoutTopicSubscriptionName;
        private readonly string _orderPaymentProcessTopicName;
        private readonly string _orderUpdatePaymentResultTopicName;

        private readonly ServiceBusProcessor _checkoutProcessor;
        private readonly ServiceBusProcessor _orderUpdatePaymentResultProcessor;

        public AzureServiceBusConsumer(
            IOrderRepository orderRepository, 
            IConfiguration configuration,
            IMessageBus messageBus
            )
        {
            _orderRepository = orderRepository;
            var _configuration = configuration;
            _messageBus = messageBus;

            _subConnStr = _configuration.GetValue<string>("ServiceBusSettings:ServiceBusConn");
            _checkoutQueueName = _configuration.GetValue<string>("ServiceBusSettings:CheckoutQueueName");
            _checkoutTopicSubscriptionName = _configuration.GetValue<string>("ServiceBusSettings:CheckoutSubscriptionName");

            _orderPaymentProcessTopicName = _configuration.GetValue<string>("ServiceBusSettings:OrderPaymentProcessTopicName");
            _orderUpdatePaymentResultTopicName = _configuration.GetValue<string>("ServiceBusSettings:OrderUpdatePaymentResultTopicName");

            var client = new ServiceBusClient(_subConnStr);
            _checkoutProcessor = client.CreateProcessor(_checkoutQueueName);
            _orderUpdatePaymentResultProcessor =
                client.CreateProcessor(_orderUpdatePaymentResultTopicName, _checkoutTopicSubscriptionName);
        }

        public async Task Start()
        {
            _checkoutProcessor.ProcessMessageAsync += OnCheckoutMessageReceived;
            _checkoutProcessor.ProcessErrorAsync += OnError;
            await _checkoutProcessor.StartProcessingAsync();

            _orderUpdatePaymentResultProcessor.ProcessMessageAsync += OnOrderPaymentUpdateResultReceived;
            _orderUpdatePaymentResultProcessor.ProcessErrorAsync += OnError;
            await _orderUpdatePaymentResultProcessor.StartProcessingAsync();
        }

        public async Task Stop()
        {
            await _checkoutProcessor.StopProcessingAsync();
            await _checkoutProcessor.DisposeAsync();

            await _orderUpdatePaymentResultProcessor.StopProcessingAsync();
            await _orderUpdatePaymentResultProcessor.DisposeAsync();
        }

        Task OnError(ProcessErrorEventArgs args)
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
                
                PaymentRequestMessage paymentRequestMessage = new()
                {
                    Name = $"{orderHeader.FirstName} {orderHeader.LastName}",
                    OrderTotal = orderHeader.OrderTotal,
                    CardNumber = orderHeader.CardNumber,
                    OrderId = orderHeader.OrderHeaderId,
                    ExpiryMonthYear = orderHeader.ExpiryMonthYear,
                    CVV = orderHeader.CVV
                };

                try
                {
                    await _messageBus.PublishMessage(paymentRequestMessage, _orderPaymentProcessTopicName, _subConnStr);
                    await args.CompleteMessageAsync(args.Message);

                }
                catch (Exception e)
                {
                    Console.WriteLine("Error in publishing order payment request message {0}",e.Message);
                    throw;
                }
            }
        }

        private async Task OnOrderPaymentUpdateResultReceived(ProcessMessageEventArgs args)
        {
            var message = args.Message;
            var body = Encoding.UTF8.GetString(message.Body);

            UpdatePaymentResultMessage paymentResultMessage = JsonConvert.DeserializeObject<UpdatePaymentResultMessage>(body);

            await _orderRepository.UpdateOrderPaymentStatus(paymentResultMessage.OrderId, paymentResultMessage.Status);
            await args.CompleteMessageAsync(args.Message);
        }
    }
}
