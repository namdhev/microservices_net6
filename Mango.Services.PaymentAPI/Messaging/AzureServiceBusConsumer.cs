using System.Text;
using Newtonsoft.Json;
using Azure.Messaging.ServiceBus;
using Mango.MessageBus;
using Mango.Services.PaymentAPI.Messages;
using PaymentProcessor;

#nullable disable
namespace Mango.Services.PaymentAPI.Messaging
{
    public class AzureServiceBusConsumer: IAzureServiceBusConsumer
    {
        private readonly IConfiguration _configuration;
        private readonly IMessageBus _messageBus;
        private readonly IProcessPayment _processPayment;

        private readonly string _subConnStr;
        private readonly string _orderPaymentTopicName;
        private readonly string _orderPaymentSubscriptionName;
        private readonly string _orderUpdatePaymentResultTopicName;
        
        private ServiceBusProcessor _orderPaymentProcessor;

        public AzureServiceBusConsumer(
            IConfiguration configuration,
            IMessageBus messageBus,
            IProcessPayment processPayment
            )
        {
            _configuration = configuration;
            _messageBus = messageBus;
            _processPayment = processPayment;

            _subConnStr = _configuration.GetValue<string>("ServiceBusSettings:ServiceBusConn");
            _orderPaymentTopicName = _configuration.GetValue<string>("ServiceBusSettings:OrderPaymentProcessTopicName");
            _orderPaymentSubscriptionName = _configuration.GetValue<string>("ServiceBusSettings:OrderPaymentProcessSubscriptionName");
            _orderUpdatePaymentResultTopicName = _configuration.GetValue<string>("ServiceBusSettings:OrderUpdatePaymentResultTopicName");

            var client = new ServiceBusClient(_subConnStr);
            _orderPaymentProcessor = client.CreateProcessor(_orderPaymentTopicName, _orderPaymentSubscriptionName);
        }

        public async Task Start()
        {
            _orderPaymentProcessor.ProcessMessageAsync += OnProcessPayment;
            _orderPaymentProcessor.ProcessErrorAsync += OnCheckoutError;

            await _orderPaymentProcessor.StartProcessingAsync();
        }

        public async Task Stop()
        {
            await _orderPaymentProcessor.StopProcessingAsync();
            await _orderPaymentProcessor.DisposeAsync();
        }

        Task OnCheckoutError(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }

        private async Task OnProcessPayment(ProcessMessageEventArgs args)
        {
            var message = args.Message;
            var body = Encoding.UTF8.GetString(message.Body);

            PaymentRequestMessage paymentRequestMessage = JsonConvert.DeserializeObject<PaymentRequestMessage>(body);
            var result = _processPayment.PaymentProcessor();

            if (paymentRequestMessage != null)
            {
                UpdatePaymentResultMessage updatePaymentResultMessage = new()
                {
                    OrderId = paymentRequestMessage.OrderId,
                    Status = result
                };

                try
                {
                    await _messageBus.PublishMessage(updatePaymentResultMessage, _orderUpdatePaymentResultTopicName, _subConnStr);
                    await args.CompleteMessageAsync(args.Message);

                }
                catch (Exception e)
                {
                    Console.WriteLine("Error in publishing order payment response message {0}", e.Message);
                    throw;
                }
            }
        }
    }
}
