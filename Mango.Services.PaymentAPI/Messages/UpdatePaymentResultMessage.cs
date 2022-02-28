using Mango.MessageBus;

namespace Mango.Services.PaymentAPI.Messages
{
    public class UpdatePaymentResultMessage: BaseMessage
    {
        public int OrderId { get; set; }
        public bool Status { get; set; }
    }
}
