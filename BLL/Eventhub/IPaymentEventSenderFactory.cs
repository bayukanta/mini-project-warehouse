using Microsoft.Extensions.Configuration;

namespace BLL.Eventhub
{
    public interface IPaymentEventSenderFactory
    {
        IPaymentEventSender Create(IConfiguration config, string eventHubName);
    }
}
