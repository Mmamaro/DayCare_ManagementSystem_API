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

namespace DayCare_ManagementSystem_API.Services
{
    public class DBCleanUpWorkerService : BackgroundService
    {
        private readonly IMongoDatabase _database;
        IOptions<DBSettings> _Dbsettings;
        private readonly ILogger<DBCleanUpWorkerService> _logger;
        private readonly IMaintenance _maintenanceRepo;

        public DBCleanUpWorkerService(IOptions<DBSettings> Dbsettings, ILogger<DBCleanUpWorkerService> logger, IMongoClient client, IMaintenance maintenanceRepo)
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
                    var dbCleanUpMonth = int.Parse(Environment.GetEnvironmentVariable("DBCleanUpMonth")!);
                    var DBCleanUpDay = int.Parse(Environment.GetEnvironmentVariable("DBCleanUpDay")!);
                    var today = DateTime.Today;

                    var lastRunYear = await _maintenanceRepo.GetLastRunYear("YearEndDbCleanup");

                    if (lastRunYear != today.Year && today.Month == dbCleanUpMonth && today.Day == DBCleanUpDay)
                    {
                        var collections = new List<string>()
                        {
                            _Dbsettings.Value.StudentsCollection,
                            _Dbsettings.Value.ApplicationsCollection,
                            _Dbsettings.Value.DocumentsMetadataCollection,
                            _Dbsettings.Value.DropOffPickUpEventsCollection,
                            _Dbsettings.Value.UserAuditCollection
                        };

                        foreach (var name in collections)
                        {
                            if(await CollectionExists(name))
                            {
                                await _database.DropCollectionAsync(name);
                                _logger.LogWarning("Dropped collection: {Collection}", name);
                            }
                            else
                            {
                                _logger.LogWarning("Collection does not exist: {Collection}", name);
                            }


                        }

                        await _maintenanceRepo.SetLastRunYear("YearEndDbCleanup", today.Year);

                    }

                    _logger.LogInformation($"DBCleanUpWorkerService done running at: {DateTime.Now.AddHours(2)}");

                    await Task.Delay(TimeSpan.FromHours(24));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in the DBCleanUpWorkerService");
                    throw;
                }
            }
        }

        private async Task<bool> CollectionExists(string name)
        {
            var collections = await _database.ListCollectionNames().ToListAsync();
            return collections.Contains(name);
        }
    }
}
