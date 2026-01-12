
namespace DayCare_ManagementSystem_API.Services
{
    public class SeedWorkerService : BackgroundService
    {
        private ILogger<SeedWorkerService> _logger;
        private SeedDbService _SeedDbService;

        public SeedWorkerService(ILogger<SeedWorkerService> logger, SeedDbService seedDbService)
        {
            _logger = logger;
            _SeedDbService = seedDbService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await _SeedDbService.CreateDefaultUser();
                //_logger.LogInformation("Hello World");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Seed Worker Services");
            }
        }
    }
}
