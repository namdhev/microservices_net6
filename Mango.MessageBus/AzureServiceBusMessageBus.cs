using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;

namespace Mango.MessageBus
{
    public class AzureServiceBusMessageBus : IMessageBus
    {
        public async Task PublishMessage(BaseMessage message, string topicName, string topicConnectionString)
        {
            ServiceBusClient client = new(topicConnectionString);
            var sender = client.CreateSender(topicName);

            var jsonMessage = JsonConvert.SerializeObject(message);
            var finalMessage = new ServiceBusMessage(jsonMessage)
            {
                CorrelationId = Guid.NewGuid().ToString()
            };

            await sender.SendMessageAsync(finalMessage);
            await client.DisposeAsync();
        }
    }
}
