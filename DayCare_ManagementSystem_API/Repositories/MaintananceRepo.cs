using DayCare_ManagementSystem_API.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Runtime;

namespace DayCare_ManagementSystem_API.Repositories
{
    public interface IMaintenance
    {
        public Task<int?> GetLastRunYear(string jobName);
        public Task SetLastRunYear(string jobName, int year);
    }
    public class MaintenanceRepo : IMaintenance
    {
        private readonly ILogger<MaintenanceRepo> _logger;
        private readonly IMongoCollection<maintenance> _maintenanceCollection;
        public MaintenanceRepo(IOptions<DBSettings> Dbsettings, IMongoClient client, ILogger<MaintenanceRepo> logger)
        {
            var database = client.GetDatabase(Dbsettings.Value.DatabaseName);

            _maintenanceCollection = database.GetCollection<maintenance>(Dbsettings.Value.maintenanceCollection);
            _logger = logger;
        }

        public async Task<int?> GetLastRunYear(string jobName)
        {
            try
            {
                var job = await _maintenanceCollection.Find(j => j.JobName == jobName).FirstOrDefaultAsync();
                
                return job?.LastRunYear;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the MaintenanceRepo in the SetLastRunYear method.");
                return null;
            }
        }

        public async Task SetLastRunYear(string jobName, int year)
        {
            try
            {
                var update = Builders<maintenance>.Update.Set(j => j.LastRunYear, year);
                
                await _maintenanceCollection.UpdateOneAsync(
                    j => j.JobName == jobName,
                    update,
                    new UpdateOptions { IsUpsert = true }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the MaintenanceRepo in the SetLastRunYear method.");
            }
        }
    }
}
