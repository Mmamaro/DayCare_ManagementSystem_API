using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using DayCare_ManagementSystem_API.Models;

namespace DayCare_ManagementSystem_API.Repositories
{
    public interface IUser
    {
        public Task<User> AddUser(User payload);
        public Task<DeleteResult> DeleteUser(string id);
        public Task<UpdateResult> UpdateStaff(string id, StaffUpdate payload);
        Task<UpdateResult> UpdateUser(string id, UserUpdate payload);
        public Task<List<User>> GetAllUsers();
        public Task<User> GetUserById(string id);
        public Task<User> GetUserByIdNumber(string idNumber);
        public Task<User> GetUserByEmail(string email);
        public Task<User> GetUserByTwoFAKeyAsync(string encryptedText);
        public Task<UpdateResult> UpdateMFAfields(UpdateMfaFieldsModel request);
        public Task<UpdateResult> updateFirstSignIn(string email);
        public Task<UpdateResult> updatePassword(LoginModel payload);
        public Task<UpdateResult> EnableMFA(string id, bool isMFAEnabled);
        public Task<User?> Login(LoginModel request);

    }
    public class UserRepo : IUser
    {
        #region [ Constructor ]
        private readonly ILogger<UserRepo> _logger;
        private readonly IMongoCollection<User> _userCollection;

        public UserRepo(IOptions<DBSettings> Dbsettings, IMongoClient client, ILogger<UserRepo> logger)
        {
            var database = client.GetDatabase(Dbsettings.Value.DatabaseName);

            _userCollection = database.GetCollection<User>(Dbsettings.Value.UsersCollection);
            _logger = logger;
        }

        #endregion

        #region [ Add User ]

        public async Task<User> AddUser(User payload)
        {
            try
            {
                payload.Id = ObjectId.GenerateNewId().ToString();

                payload.Password = payload.Password;

                await _userCollection.InsertOneAsync(payload);

                return payload;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the user repo while trying to add user.");
                throw;

            }
        }

        #endregion

        #region [ Delete User ]

        public async Task<DeleteResult> DeleteUser(string id)
        {
            try
            {
                return await _userCollection.DeleteOneAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the user repo while trying to delete user.");
                throw;
            }
        }

        #endregion

        #region [ Get All Users ]

        public async Task<List<User>> GetAllUsers()
        {
            try
            {
                return await _userCollection.Find(c => true).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the user repo while trying to get all users.");
                throw;
            }
        }

        #endregion

        #region [ Get User By Id ]

        public async Task<User> GetUserById(string id)
        {
            try
            {
                return await _userCollection.Find(c => c.Id == id).FirstOrDefaultAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the user repo while trying to get user by id.");
                throw;
            }
        }

        #endregion

        #region [ Get User By Id Number ]

        public async Task<User> GetUserByIdNumber(string idNumber)
        {
            try
            {
                return await _userCollection.Find(c => c.IdNumber == idNumber).FirstOrDefaultAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the user repo while trying to get user by id.");
                throw;
            }
        }

        #endregion

        #region [ Get user by two factor key ]
        public async Task<User> GetUserByTwoFAKeyAsync(string encryptedText)
        {
            try
            {
                var user = await _userCollection.Find(u => u.MFAKey == encryptedText).FirstOrDefaultAsync();

                if (user == null)
                {
                    return null;
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the user repo while trying check the two factor key.");
                throw;
            }
        }
        #endregion

        #region [ Get User By Email ]

        public async Task<User> GetUserByEmail(string email)
        {
            try
            {
                var user = await _userCollection.Find(u => u.Email == email.ToLower()).FirstOrDefaultAsync();

                if (user == null)
                {
                    return null;
                }

                return user;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the user repo while trying to get user by username.");
                throw;
            }
        }

        #endregion

        #region [ Update Staff ]

        public async Task<UpdateResult> UpdateStaff(string id, StaffUpdate payload)
        {
            try
            {
                var user = await _userCollection.Find(c => c.Id == id).FirstOrDefaultAsync();

                if (user == null)
                {
                    return default;
                }

                var update = Builders<User>.Update;

                var updateOperations = new List<UpdateDefinition<User>>();

                if (!string.IsNullOrEmpty(payload.Firstname))
                {
                    updateOperations.Add(update.Set(u => u.Firstname, payload.Firstname));
                }

                if (!string.IsNullOrWhiteSpace(payload.Lastname))
                {
                    updateOperations.Add(update.Set(u => u.Lastname, payload.Lastname));
                }

                if (!string.IsNullOrWhiteSpace(payload.Email))
                {
                    updateOperations.Add(update.Set(u => u.Email, payload.Email.ToLower()));
                }

                if (!string.IsNullOrWhiteSpace(payload.Role))
                {
                    updateOperations.Add(update.Set(u => u.Role, payload.Role.ToLower()));
                }

                if (payload.Active.HasValue)
                {
                    updateOperations.Add(update.Set(u => u.Active, payload.Active));
                }

                if (!string.IsNullOrWhiteSpace(payload.IdNumber))
                {
                    updateOperations.Add(update.Set(u => u.IdNumber, payload.IdNumber.ToLower()));
                }

                updateOperations.Add(update.Set(u => u.UpdatedAt, DateTime.Now.AddHours(2)));

                var combinedUpdate = Builders<User>.Update.Combine(updateOperations);

                return await _userCollection.UpdateOneAsync(u => u.Id == id, combinedUpdate);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the user repo while trying to update user.");
                throw;
            }
        }

        #endregion

        #region [ Update User ]

        public async Task<UpdateResult> UpdateUser(string id, UserUpdate payload)
        {
            try
            {
                var user = await _userCollection.Find(c => c.Id == id).FirstOrDefaultAsync();

                if (user == null)
                {
                    return default;
                }

                var update = Builders<User>.Update;

                var updateOperations = new List<UpdateDefinition<User>>();

                if (!string.IsNullOrEmpty(payload.Firstname))
                {
                    updateOperations.Add(update.Set(u => u.Firstname, payload.Firstname));
                }

                if (!string.IsNullOrWhiteSpace(payload.Lastname))
                {
                    updateOperations.Add(update.Set(u => u.Lastname, payload.Lastname));
                }

                if (!string.IsNullOrWhiteSpace(payload.Email))
                {
                    updateOperations.Add(update.Set(u => u.Email, payload.Email.ToLower()));
                }

                if (!string.IsNullOrWhiteSpace(payload.IdNumber))
                {
                    updateOperations.Add(update.Set(u => u.IdNumber, payload.IdNumber.ToLower()));
                }

                updateOperations.Add(update.Set(u => u.UpdatedAt, DateTime.Now.AddHours(2)));

                var combinedUpdate = Builders<User>.Update.Combine(updateOperations);

                return await _userCollection.UpdateOneAsync(u => u.Id == id, combinedUpdate);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the user repo while trying to update user.");
                throw;
            }
        }

        #endregion

        #region [ Update 2FA Fields ]
        public async Task<UpdateResult> UpdateMFAfields(UpdateMfaFieldsModel request)
        {
            try
            {

                var update = Builders<User>.Update
                    .Set(u => u.QRCode, request.QrCodeUrl)
                    .Set(u => u.ManualCode, request.ManualEntryCode)
                    .Set(u => u.MFAKey, request.MFAKey)
                    .Set(u => u.isFirstSignIn, request.isFirstSignIn)
                    .Set(u => u.isMFAVerified, request.isMFAVerified)
                    .Set(u => u.UpdatedAt, DateTime.Now.AddHours(2)); ;


                return await _userCollection.UpdateOneAsync(c => c.Id == request.userId, update);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the user repo while trying to update user.");
                throw;
            }
        }
        #endregion

        #region [ Update First Sign In ]
        public async Task<UpdateResult> updateFirstSignIn(string email)
        {
            try
            {
                var user = await _userCollection.Find(c => c.Email == email.ToLower()).FirstOrDefaultAsync();

                if (user == null)
                {
                    return default;
                }

                var update = Builders<User>.Update
                    .Set(u => u.isFirstSignIn, false)
                    .Set(u => u.isMFAVerified, true)
                    .Set(u => u.UpdatedAt, DateTime.Now.AddHours(2));

                return await _userCollection.UpdateOneAsync(c => c.Email == email, update);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the repo while trying to update mfa fields in the database.");
                throw;
            }
        }
        #endregion

        public async Task<User?> Login(LoginModel payload)
        {
            try
            {
                var user = await _userCollection.Find(c => c.Email == payload.Email.ToLower()).FirstOrDefaultAsync();

                if (user == null || user.Active == false)
                {
                    return null;
                }

                var isValid = BCrypt.Net.BCrypt.Verify(payload.Password, user.Password);

                if (!isValid)
                {
                    return null;
                }

                return user;


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the repo while trying to log in user.");
                throw;
            }
        }

        public async Task<UpdateResult> EnableMFA(string id, bool isMFAEnabled)
        {
            try
            {
                var user = await _userCollection.Find(c => c.Id == id).FirstOrDefaultAsync();

                if (user == null)
                {
                    return default;
                }

                var update = Builders<User>.Update
                    .Set(u => u.isMFAEnabled, isMFAEnabled)
                    .Set(u => u.UpdatedAt, DateTime.Now.AddHours(2));

                return await _userCollection.UpdateOneAsync(c => c.Id == id, update);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the repo while trying to update user MFA status.");
                throw;
            }
        }

        public async Task<UpdateResult> updatePassword(LoginModel payload)
        {
            try
            {
                var user = await _userCollection.Find(c => c.Email == payload.Email.ToLower()).FirstOrDefaultAsync();

                if (user == null)
                {
                    return default;
                }

                var update = Builders<User>.Update
                    .Set(u => u.Password, BCrypt.Net.BCrypt.HashPassword(payload.Password))
                    .Set(u => u.UpdatedAt, DateTime.Now.AddHours(2));

                return await _userCollection.UpdateOneAsync(c => c.Email == payload.Email, update);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the repo while trying to update password.");
                throw;
            }
        }
    }
}
