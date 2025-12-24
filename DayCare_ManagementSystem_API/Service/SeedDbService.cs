using BCrypt.Net;
using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Runtime;

namespace DayCare_ManagementSystem_API.Services
{
    public class SeedDbService
    {
        #region [ Constructor ]
        private readonly IMongoCollection<User> _userCollection;
        private readonly IUser _userRepo;
        private readonly ILogger<SeedDbService> _logger;

        public SeedDbService(IOptions<DBSettings> DbSettings, IMongoClient client,
           IUser userRepo, ILogger<SeedDbService> logger)
        {
            _logger = logger;
            _userRepo = userRepo;

            var database = client.GetDatabase(DbSettings.Value.DatabaseName);
            _userCollection = database.GetCollection<User>(DbSettings.Value.UsersCollection);
        }
        #endregion

        #region [ Seed User ]
        public async Task CreateDefaultUser()
        {
            try
            {

                var email = Environment.GetEnvironmentVariable("DefaultUser_Email");
                var userExists = await _userRepo.GetUserByEmail(email);
                var password = Environment.GetEnvironmentVariable("DefaultUser_Password");

                if (userExists == null)
                {

                    User user = new User
                    {
                        Firstname = "Paballo",
                        Lastname = "Mmamaro",
                        Email = email.ToLower(),
                        Password = BCrypt.Net.BCrypt.HashPassword(password),
                        Role = "admin",
                        isMFAEnabled = false,
                        CreatedAt = DateTime.Now.AddHours(2),
                        UpdatedAt = DateTime.Now.AddHours(2),
                        isFirstSignIn = true,
                        isMFAVerified = false,
                        Active = true
                    };

                    await _userRepo.AddUser(user);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while trying to seed user in the db");
                throw;
            }
        }
        #endregion
    }
}
