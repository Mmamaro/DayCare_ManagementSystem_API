using DayCare_ManagementSystem_API.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using static QRCoder.PayloadGenerator;

namespace DayCare_ManagementSystem_API.Repositories
{
    #region [ Interface ]
    public interface IRefreshToken
    {
        Task<RefreshToken> AddRefreshToken(RefreshToken refreshToken);
        Task<RefreshToken> GetRefreshTokenByUserId(string id);
        Task<RefreshToken> GetRefreshTokenByRefreshToken(string token);
        Task<DeleteResult> DeleteRefreshToken(string id);

    } 
    #endregion

    public class RefreshTokenRepo : IRefreshToken
    {
        #region [ Constructor ]
        private readonly ILogger<RefreshTokenRepo> _logger;
        private readonly IMongoCollection<RefreshToken> _tokenCollection;

        public RefreshTokenRepo(IOptions<DBSettings> Dbsettings, IMongoClient client, ILogger<RefreshTokenRepo> logger)
        {
            var database = client.GetDatabase(Dbsettings.Value.DatabaseName);

            _tokenCollection = database.GetCollection<RefreshToken>(Dbsettings.Value.RefreshTokenCollection);
            _logger = logger;
        }
        #endregion


        #region [ Add Refresh Token ]

        public async Task<RefreshToken> AddRefreshToken(RefreshToken payload)
        {
            try
            {
                payload.Id = ObjectId.GenerateNewId().ToString();

                await _tokenCollection.InsertOneAsync(payload);

                return payload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the refresh token  repo while trying to add a refresh token to the db");
                throw;
            }
        }
        #endregion

        #region [ Delete Refresh Token ]
        public async Task<DeleteResult> DeleteRefreshToken(string id)
        {
            try
            {

                return await _tokenCollection.DeleteOneAsync(c => c.Id == id);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the refresh token repo while trying to delete a refresh token");
                throw;
            }
        }
        #endregion

        #region [ Get refresh Token By User Id ]
        public async Task<RefreshToken> GetRefreshTokenByUserId(string id)
        {
            try
            {
                return await _tokenCollection.Find(c => c.UserId == id).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the refresh token repo while trying to get a refresh token by user id");
                throw;
            }
        }
        #endregion

        #region [ Get Refresh Token by Token]
        public async Task<RefreshToken> GetRefreshTokenByRefreshToken(string token)
        {
            try
            {
                var refreshToken = await _tokenCollection.Find( t => t.Token == token).FirstOrDefaultAsync();


                if (refreshToken == null)
                {
                    return null;
                }

                return refreshToken;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the refresh token repo while trying to get a refresh token by refresh token");
                throw;
            }
        } 
        #endregion
    }
}
