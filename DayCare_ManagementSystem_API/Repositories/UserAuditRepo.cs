using DayCare_ManagementSystem_API.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Runtime;
using static QRCoder.PayloadGenerator;

namespace DayCare_ManagementSystem_API.Repositories
{
    #region [ Interface ]
    public interface IUserAudit
    {
        public Task<UserAudit> AddAudit(string userId, string userEmail, string Action, string Description);
        public Task<List<UserAudit>> GetAudits(int page, int pageSize, AuditFilters filters);
        public Task<List<string?>> GetActions(DateTime startDate, DateTime endDate);
    } 
    #endregion
    public class UserAuditRepo : IUserAudit
    {
        #region [ Constructor ]
        private readonly ILogger<UserAuditRepo> _logger;
        private readonly IMongoCollection<UserAudit> _userAuditsCollection;
        private readonly string _tableName;

        public UserAuditRepo(IOptions<DBSettings> Dbsettings, IMongoClient client, ILogger<UserAuditRepo> logger)
        {
            var database = client.GetDatabase(Dbsettings.Value.DatabaseName);

            _userAuditsCollection = database.GetCollection<UserAudit>(Dbsettings.Value.UserAuditCollection);
            _logger = logger;
        }
        #endregion

        #region [ Add Audit ]
        public async Task<UserAudit> AddAudit(string userId, string userEmail, string Action, string Description)
        {
            try
            {
                UserAudit userAudit = new UserAudit()
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Action = Action.ToLower(),
                    Description = Description.ToLower(),
                    userEmail = userEmail.ToLower(),
                    userId = userId,
                    CreatedAt = DateTime.Now.AddHours(2).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                await _userAuditsCollection.InsertOneAsync(userAudit);

                return userAudit;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the User Audit Repo while trying to add audit");
                throw;
            }
        }
        #endregion

        #region [ Get Actions ]
        public async Task<List<string?>> GetActions(DateTime startDate, DateTime endDate)
        {
            try
            {

                var filter = Builders<UserAudit>.Filter.And(
                    Builders<UserAudit>.Filter.Gte( x => x.CreatedAt, startDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
                    Builders<UserAudit>.Filter.Lte(x => x.CreatedAt, endDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")));

                var results =  await _userAuditsCollection.Find(filter).ToListAsync();

                var actions = results.Select(x => x.Action).Distinct();

                return actions.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the User Audit Repo while trying to Get Actions.");
                throw;
            }
        } 
        #endregion

        #region [ Get Audits ]
        public async Task<List<UserAudit>> GetAudits(int page, int pageSize, AuditFilters filters)
        {
            try
            {

                var skip = (page - 1) * pageSize;
                var take = pageSize;


                var filter = Builders<UserAudit>.Filter.And(
                    Builders<UserAudit>.Filter.Gte(x => x.CreatedAt, filters.startDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
                    Builders<UserAudit>.Filter.Lte(x => x.CreatedAt, filters.endDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")));

                if (!string.IsNullOrEmpty(filters.userEmail))
                {
                    filter = Builders<UserAudit>.Filter.And( filter, Builders<UserAudit>.Filter.Eq(x => x.userEmail, filters.userEmail.ToLower()));
                }

                if (!string.IsNullOrEmpty(filters.action))
                {
                    filter = Builders<UserAudit>.Filter.And( filter, Builders<UserAudit>.Filter.Eq(x => x.Action, filters.action.ToLower()));
                }


                var results = await _userAuditsCollection.Find(filter).ToListAsync();

                var data = results.OrderBy(r => r.userId)
                    .Skip(skip)
                    .Take(take);

                return data.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the User Audit Repo while trying to get all audits");
                throw;
            }
        }
        #endregion

    }
}
