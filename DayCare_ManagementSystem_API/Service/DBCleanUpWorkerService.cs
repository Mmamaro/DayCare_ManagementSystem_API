
using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Repositories;
using DayCare_ManagementSystem_API.Service;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Net.Http;
using System.Runtime;
using System.Runtime.CompilerServices;
using static QRCoder.PayloadGenerator;

namespace ccod_voyc_integration_workerservice.Services
{
    public class DBCleanUpWorkerService : BackgroundService
    {
        private readonly IMongoDatabase _database;
        IOptions<DBSettings> _Dbsettings;
        private readonly ILogger<PickUpWorkerService> _logger;
        private readonly IMaintenance _maintenanceRepo;

        public DBCleanUpWorkerService(IOptions<DBSettings> Dbsettings, ILogger<PickUpWorkerService> logger, IMongoClient client, IMaintenance maintenanceRepo)
        {
            _Dbsettings = Dbsettings;
            _database = client.GetDatabase(_Dbsettings.Value.DatabaseName);
            _logger = logger;
            _maintenanceRepo = maintenanceRepo;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            while (!stoppingToken.IsCancellationRequested)
            {

                try
                {
                    _logger.LogInformation($"DBCleanUpWorkerService running aat: {DateTime.Now.AddHours(2)}");
                    var today = DateTime.Today;

                    var runYear = await _maintenanceRepo.GetLastRunYear("YearEndDbCleanup");

                    if (runYear != today.Year && today.Month == 12 && today.Day == 31)
                    {
                        var collections = new List<string>()
                        {
                            _Dbsettings.Value.StudentsCollection,
                            _Dbsettings.Value.ApplicationsCollection,
                            _Dbsettings.Value.DocumentsMetadataCollection,
                            _Dbsettings.Value.DropOffPickUpEventsCollection
                        };

                        foreach (var name in collections)
                        {
                            await _database.DropCollectionAsync(name);
                            _logger.LogWarning("Dropped collection: {Collection}", name);
                        }

                        await _maintenanceRepo.SetLastRunYear("YearEndDbCleanup", today.Year);

                    }

                    await Task.Delay(TimeSpan.FromHours(24));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in the DBCleanUpWorkerService");
                    throw;
                }
            }
        }
    }
}
