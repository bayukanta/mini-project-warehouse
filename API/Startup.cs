using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL;
using DAL.Repositories;
using BLL.Eventhub;
using BLL.Service;
using BLL.Jobs;
using System.Reflection;

namespace API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddDbContext<WarehouseDbContext>(options =>
              options
                .UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
            );
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IPaymentEventSenderFactory, PaymentEventSenderFactory>();
            services.AddHostedService<OrderMessageListener>();
            services.AddTransient<DeliverToPaymentJob>();
            services.AddTransient<QuartzJobFactory>();
            services.AddSingleton<ISchedulerService, SchedulerService>();



        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            var schedulerService = app.ApplicationServices.GetRequiredService<ISchedulerService>();
            schedulerService.Initialize();
            schedulerService.Start();
        }
    }
}
