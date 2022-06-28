using Microsoft.Extensions.Configuration;

namespace BLL.Eventhub
{
    public class PaymentEventSenderFactory : IPaymentEventSenderFactory
    {
        public IPaymentEventSender Create(IConfiguration config, string eventHubName)
        {
            return new PaymentEventSender(config, eventHubName); ;
        }
    }
}

