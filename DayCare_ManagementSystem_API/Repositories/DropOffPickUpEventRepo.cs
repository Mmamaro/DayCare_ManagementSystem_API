using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Models.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using static QRCoder.PayloadGenerator;
using static System.Net.Mime.MediaTypeNames;
using Application = DayCare_ManagementSystem_API.Models.Application;

namespace DayCare_ManagementSystem_API.Repositories
{
    public interface IEvent
    {
        public Task<DropOffPickUpEvent> AddEvent(DropOffPickUpEvent payload);
        public Task<List<DropOffPickUpEvent>> GetAllEvents();
        public Task<List<DropOffPickUpEvent>> GetEventsByFilters(EventFilter payload);
        public Task<List<DropOffPickUpEvent>> GetEventsByStudentId(string id);
        public Task<List<DropOffPickUpEvent>> GetEventById(string id);
        public Task<List<DropOffPickUpEvent>> GetEventsByKinId(string id);
        public Task<UpdateResult> UpdateEvent(DropOffPickUpEvent payload);
        public Task<DropOffPickUpEvent?> GetLastEventBefore(string studentId, DateTime occurredAt);
        public Task<DeleteResult> DeleteEvent(string id);

    }
    public class DropOffPickUpEventRepo : IEvent
    {
        #region [ Constructor ]
        private readonly ILogger<DropOffPickUpEventRepo> _logger;
        private readonly IMongoCollection<DropOffPickUpEvent> _eventsCollection;

        public DropOffPickUpEventRepo(IOptions<DBSettings> Dbsettings, IMongoClient client, ILogger<DropOffPickUpEventRepo> logger)
        {
            var database = client.GetDatabase(Dbsettings.Value.DatabaseName);

            _eventsCollection = database.GetCollection<DropOffPickUpEvent>(Dbsettings.Value.DropOffPickUpEventsCollection);
            _logger = logger;
        }
        #endregion

        public async Task<DropOffPickUpEvent> AddEvent(DropOffPickUpEvent payload)
        {
            try
            {
                await _eventsCollection.InsertOneAsync(payload);

                return payload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DropOffPickUpEventRepo in the AddEvent method.");
                throw;
            }
        }

        public async Task<DeleteResult> DeleteEvent(string id)
        {
            try
            {
                return await _eventsCollection.DeleteOneAsync(c => c.EventId == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DropOffPickUpEventRepo in the DeleteEvent method.");
                throw;
            }
        }

        public async Task<List<DropOffPickUpEvent>> GetAllEvents()
        {
            try
            {
                return await _eventsCollection.Find(x => true).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DropOffPickUpEventRepo in the GetAllEvents method.");
                throw;
            }
        }

        public async Task<List<DropOffPickUpEvent>> GetEventsByFilters(EventFilter payload)
        {
            try
            {
                var builder = Builders<DropOffPickUpEvent>.Filter;
                var filter = builder.Empty;

                var start = payload.StartDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                var end = payload.EndDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

                filter &= builder.Gte(a => a.OccurredAt, start)
                                & builder.Lte(a => a.OccurredAt, end);

                if (!string.IsNullOrWhiteSpace(payload.EventId))
                {
                    filter &= builder.Eq(a => a.EventId, payload.EventId);
                }

                if (!string.IsNullOrWhiteSpace(payload.NextOfKinId))
                {
                    filter &= builder.Eq(a => a.NextOfKinId, payload.NextOfKinId);
                }

                if (!string.IsNullOrWhiteSpace(payload.StudentId))
                {
                    filter &= builder.Eq(a => a.StudentId, payload.StudentId);
                }

                if (!string.IsNullOrWhiteSpace(payload.EventType))
                {
                    filter &= builder.Eq(a => a.EventType, payload.EventType);
                }

                return await _eventsCollection
                                .Find(filter)
                                .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DropOffPickUpEventRepo in the GetEventsByFilters method.");
                throw;
            }
        }

        public async Task<List<DropOffPickUpEvent>> GetEventsByKinId(string id)
        {
            try
            {
                return await _eventsCollection.Find(x => x.NextOfKinId == id).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DropOffPickUpEventRepo in the GetEventsByKinId method.");
                throw;
            }
        }

        public async Task<List<DropOffPickUpEvent>> GetEventById(string id)
        {
            try
            {
                return await _eventsCollection.Find(x => x.EventId == id).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DropOffPickUpEventRepo in the GetEventById method.");
                throw;
            }
        }

        public async Task<List<DropOffPickUpEvent>> GetEventsByStudentId(string id)
        {
            try
            {
                return await _eventsCollection.Find(x => x.StudentId == id).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DropOffPickUpEventRepo in the GetEventsByStudentId method");
                throw;
            }
        }

        public async Task<DropOffPickUpEvent?> GetLastEventBefore(string studentId,DateTime occurredAt)
        {
            try
            {
                var filter = Builders<DropOffPickUpEvent>.Filter.And(
                    Builders<DropOffPickUpEvent>.Filter.Eq(x => x.StudentId, studentId),
                    Builders<DropOffPickUpEvent>.Filter.Lt(x => x.OccurredAt, occurredAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
                );

                return await _eventsCollection
                    .Find(filter)
                    .SortByDescending(x => x.OccurredAt)
                    .Limit(1)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DropOffPickUpEventRepo in the GetLastEventBefore method.");
                throw;
            }
        }

        public async Task<UpdateResult> UpdateEvent(DropOffPickUpEvent payload)
        {
            try
            {
                var filter = Builders<DropOffPickUpEvent>.Filter.Eq(
                    e => e.EventId, payload.EventId);

                var update = Builders<DropOffPickUpEvent>.Update
                    .Set(e => e.EventType, payload.EventType)
                    .Set(e => e.OccurredAt, payload.OccurredAt)
                    .Set(e => e.Notes, payload.Notes)
                    .Set(e => e.StudentId, payload.StudentId)
                    .Set(e => e.NextOfKinId, payload.NextOfKinId);

                return await _eventsCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the DropOffPickUpEventRepo in the UpdateEvent method.");
                throw;
            }
        }




    }
}
