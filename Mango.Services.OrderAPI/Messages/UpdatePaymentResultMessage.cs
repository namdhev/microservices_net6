namespace Mango.Services.OrderAPI.Messages
{
    public class UpdatePaymentResultMessage
    {
        public int OrderId { get; set; }
        public bool Status { get; set; }
    }
}
