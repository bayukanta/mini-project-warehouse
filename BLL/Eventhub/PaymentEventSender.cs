using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace BLL.Eventhub
{
    public class PaymentEventSender : IPaymentEventSender, IDisposable
    {
        private EventHubProducerClient producer;
        private EventDataBatch eventBatch;

        public PaymentEventSender(IConfiguration config, string eventHubName)
        {
            producer = new EventHubProducerClient(config.GetValue<string>("EventHub:ConnectionString"), eventHubName);
        }

        public async Task CreateEventBatchAsync()
        {
            eventBatch = await producer.CreateBatchAsync();
        }

        public bool AddMessage(object data)
        {
            string message = JsonConvert.SerializeObject(data);
            return eventBatch.TryAdd(new EventData(new BinaryData(message)));
        }

        public async Task SendMessage()
        {
            await producer.SendAsync(eventBatch);
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual async void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    await producer.CloseAsync();
                    await producer.DisposeAsync();
                }
                eventBatch = null;
                producer = null;

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
