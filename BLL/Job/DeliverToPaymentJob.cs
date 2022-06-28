using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using DAL.Models;
using DAL.Repositories;
using BLL.Eventhub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Jobs
{
    public class DeliverToPaymentJob : IJob
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        private IUnitOfWork _unitOfWork;
        private IServiceScopeFactory _serviceScopeFactory;
        

        public DeliverToPaymentJob(
            ILogger<DeliverToPaymentJob> logger,
            IConfiguration config,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _config = config;
            _serviceScopeFactory = serviceScopeFactory;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation($"Current Date : {DateTime.UtcNow}");
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    _unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var list_od = await _unitOfWork.ODStatusRepository
                                        .GetAll()
                                        .Where(od => od.Delivered == false)
                                        .ToListAsync();
                    foreach (var od in list_od)
                    {
                        od.Delivered = true;
                        _unitOfWork.ODStatusRepository.Edit(od);
                        await insertIntoEventHub(od);
                        
                    }
                    await _unitOfWork.SaveAsync();

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        public async Task insertIntoEventHub(ODStatus od)
        {
            string connString = _config.GetValue<string>("EventHub:ConnectionString");
            string topic = _config.GetValue<string>("EventHub:PaymentEvent");


            await using var publisher = new EventHubProducerClient(connString, topic);
            using var eventBatch = await publisher.CreateBatchAsync();
            var message = JsonConvert.SerializeObject(od);
            eventBatch.TryAdd(new EventData(new BinaryData(message)));
            await publisher.SendAsync(eventBatch);
        }
       
        
    }
}
