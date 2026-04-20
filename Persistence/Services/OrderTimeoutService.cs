
using Mart.Domain.Interface;

namespace Mart.Persistence.Services
{
    public class OrderTimeoutService : BackgroundService
    {
        private readonly IServiceProvider _services;

        public OrderTimeoutService(IServiceProvider services)
        {
            _services = services;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using(var scope= _services.CreateScope())
                {
                    var repo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                    await repo.HandleExpiredAssignmentsAsync();
                }
                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}
