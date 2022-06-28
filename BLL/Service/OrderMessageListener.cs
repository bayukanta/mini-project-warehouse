using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using DAL.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DAL.Repositories;

namespace BLL.Service
{
    public class OrderMessageListener : IHostedService, IDisposable
    {
        private readonly EventProcessorClient processor;
        private readonly ILogger _logger;
        private IServiceScopeFactory _serviceScopeFactory;
        public OrderMessageListener(
            IConfiguration config, 
            ILogger<OrderMessageListener> logger, 
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            string topic = config.GetValue<string>("EventHub:OrderEvent");
            string azureContainername = config.GetValue<string>("AzureStorage:AzureContainerEventHub");
            string eventHubConn = config.GetValue<string>("EventHub:ConnectionString");
            string azStorageConn = config.GetValue<string>("AzureStorage:ConnectionString");
            string consumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;
            _serviceScopeFactory = serviceScopeFactory;
            BlobContainerClient storageClient = new BlobContainerClient(azStorageConn, azureContainername);

            processor = new EventProcessorClient(storageClient, consumerGroup, eventHubConn, topic);

            processor.ProcessEventAsync += ProcessEventHandler;
            processor.ProcessErrorAsync += ProcessErrorHandler;
        }


        public async Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
        {
            _logger.LogError(eventArgs.Exception.Message);
        }
        public async Task ProcessEventHandler(ProcessEventArgs eventArgs)
        {
            var string_data = Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray());
            JObject json_data = JObject.Parse(string_data);
            _logger.LogInformation(string_data);
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var od_status = new ODStatus()
                {
                    ODId = new Guid(json_data["Id"].ToString()),
                    OrderId = new Guid(json_data["OrderId"].ToString()),
                    UserId = new Guid(json_data["Order"]["UserId"].ToString()),
                    Delivered = false,
                    OrderPrice = json_data["OrderPrice"].ToObject<int>(),
          
                };
                IUnitOfWork _unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                _unitOfWork.ODStatusRepository.Add(od_status);
                await _unitOfWork.SaveAsync();
                //Do your stuff
            }
            await eventArgs.UpdateCheckpointAsync(eventArgs.CancellationToken);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await processor.StartProcessingAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await processor.StopProcessingAsync();
        }
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MessageListernerService()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
        
    }
}
