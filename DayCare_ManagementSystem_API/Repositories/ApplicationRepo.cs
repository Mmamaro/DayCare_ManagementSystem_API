using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Models.ValueObjects;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using static System.Net.Mime.MediaTypeNames;
using Application = DayCare_ManagementSystem_API.Models.Application;

namespace DayCare_ManagementSystem_API.Repositories
{
    public interface IApplication
    {
        public Task<Application> AddApplication(Application payload);
        public Task<DeleteResult> DeleteApplication(string id);
        public Task<List<Application>> GetAllApplications();
        public Task<Application> GetApplicationById(string id);
        public Task<UpdateResult> UpdateApplicationMedicalConditions(string applicationId, Allergy payload);
        public Task<UpdateResult> UpdateApplicationAllergies(string applicationId, Allergy payload);
        public Task<UpdateResult> UpdateStudentProfile(string applicationId, StudentProfile payload);
        public Task<UpdateResult> UpdateNextOfKin(string applicationId, NextOfKin payload);
        public Task<UpdateResult> UpdateStatus(string applicationId, string Status);
        public Task<List<Application>> GetApplicationByFilters(ApplicationFilters payload);

    }
    public class ApplicationRepo : IApplication
    {
        #region [ Constructor ]
        private readonly ILogger<ApplicationRepo> _logger;
        private readonly IMongoCollection<Application> _ApplicationCollection;

        public ApplicationRepo(IOptions<DBSettings> Dbsettings, IMongoClient client, ILogger<ApplicationRepo> logger)
        {
            var database = client.GetDatabase(Dbsettings.Value.DatabaseName);

            _ApplicationCollection = database.GetCollection<Application>(Dbsettings.Value.ApplicationsCollection);
            _logger = logger;
        }

        #endregion

        #region [ Add Application ]

        public async Task<Application> AddApplication(Application payload)
        {
            try
            {
                payload.ApplicationId = ObjectId.GenerateNewId().ToString();


                await _ApplicationCollection.InsertOneAsync(payload);

                return payload;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo while trying to add Application.");
                throw;

            }
        }

        #endregion

        #region [ Delete Application ]

        public async Task<DeleteResult> DeleteApplication(string id)
        {
            try
            {
                return await _ApplicationCollection.DeleteOneAsync(c => c.ApplicationId == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo while trying to delete Application.");
                throw;
            }
        }

        #endregion

        #region [ Get All Applications ]

        public async Task<List<Application>> GetAllApplications()
        {
            try
            {
                return await _ApplicationCollection.Find(c => true).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo while trying to get all Applications.");
                throw;
            }
        }

        #endregion

        #region [ Get Application By Id ]

        public async Task<Application> GetApplicationById(string id)
        {
            try
            {
                return await _ApplicationCollection.Find(c => c.ApplicationId == id).FirstOrDefaultAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo while trying to get Application by id.");
                throw;
            }
        }

        #endregion

        #region [ GetApplicationByFilters ]

        public async Task<List<Application>> GetApplicationByFilters(ApplicationFilters payload)
        {
            try
            {
                var builder = Builders<Application>.Filter;
                var filter = builder.Empty;

                if (!string.IsNullOrWhiteSpace(payload.StudentIdNumber))
                {
                    filter &= builder.Eq(a => a.Student.IdNumber, payload.StudentIdNumber);
                }

                if (!string.IsNullOrWhiteSpace(payload.NextOfKinIdNumber))
                {
                    filter &= builder.ElemMatch(
                        a => a.NextOfKin,
                        k => k.IdNumber == payload.NextOfKinIdNumber
                    );
                }

                if (!string.IsNullOrWhiteSpace(payload.EnrollmentYear))
                {
                    filter &= builder.Eq(a => a.EnrollmentYear, payload.EnrollmentYear);
                }

                return await _ApplicationCollection
                                .Find(filter)
                                .ToListAsync();


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo while trying to get Application by filters");
                throw;
            }
        }
        #endregion

        #region [ Update Application Status ]

        public async Task<UpdateResult> UpdateStatus(string applicationId, string Status)
        {
            try
            {
                var filter = Builders<Application>.Filter.And(
                    Builders<Application>.Filter.Eq(a => a.ApplicationId, applicationId)
                );


                //Updates the Matched element which should be one.
                var update = Builders<Application>.Update
                        .Set(a => a.Status, Status)
                        .Set(a => a.LastUpdatedAt, DateTime.UtcNow);

                return await _ApplicationCollection.UpdateOneAsync(filter, update);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo in the UpdateStatus method.");
                throw;
            }
        }

        #endregion


        #region [ Update Application NextOfKin ]

        public async Task<UpdateResult> UpdateNextOfKin(string applicationId, NextOfKin payload)
        {
            try
            {
                var filter = Builders<Application>.Filter.And(
                    Builders<Application>.Filter.Eq(a => a.ApplicationId, applicationId),
                    Builders<Application>.Filter.ElemMatch(
                        a => a.NextOfKin,
                        k => k.NextOfKinId == payload.NextOfKinId
                    )
                );


                //Updates the Matched element which should be one.
                var update = Builders<Application>.Update
                        .Set(a => a.NextOfKin[-1], payload)
                        .Set(a => a.LastUpdatedAt, DateTime.UtcNow);

                return await _ApplicationCollection.UpdateOneAsync(filter, update);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo in the UpdateNextOfKin method.");
                throw;
            }
        }

        #endregion

        #region [ Update Application Student ]

        public async Task<UpdateResult> UpdateStudentProfile(string applicationId, StudentProfile payload)
        {
            try
            {
                var filter = Builders<Application>.Filter.And(
                    Builders<Application>.Filter.Eq(a => a.ApplicationId, applicationId),
                    Builders<Application>.Filter.Eq(a => a.Student.StudentProfileId, payload.StudentProfileId)
                );


                //Updates the Matched element which should be one.
                var update = Builders<Application>.Update
                        .Set(a => a.Student, payload)
                         .Set(a => a.LastUpdatedAt, DateTime.UtcNow); ;

                return await _ApplicationCollection.UpdateOneAsync(filter, update);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo in the UpdateStudentProfile method.");
                throw;
            }
        }

        #endregion

        #region [ Update Application Allergies ]

        public async Task<UpdateResult> UpdateApplicationAllergies(string applicationId, Allergy payload)
        {
            try
            {
                var filter = Builders<Application>.Filter.And(
                    Builders<Application>.Filter.Eq(a => a.ApplicationId, applicationId),
                    Builders<Application>.Filter.ElemMatch(
                        a => a.allergies,
                        k => k.AllergyId == payload.AllergyId
                    )
                );


                //Updates the Matched element which should be one.
                var update = Builders<Application>.Update
                        .Set(a => a.allergies[-1], payload)
                         .Set(a => a.LastUpdatedAt, DateTime.UtcNow); ;

                return await _ApplicationCollection.UpdateOneAsync(filter, update);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo in the UpdateAllergies method.");
                throw;
            }
        }

        #endregion

        #region [ Update Application Medical Conditions ]

        public async Task<UpdateResult> UpdateApplicationMedicalConditions(string applicationId, Allergy payload)
        {
            try
            {
                var filter = Builders<Application>.Filter.And(
                    Builders<Application>.Filter.Eq(a => a.ApplicationId, applicationId),
                    Builders<Application>.Filter.ElemMatch(
                        a => a.allergies,
                        k => k.AllergyId == payload.AllergyId
                    )
                );


                //Updates the Matched element which should be one.
                var update = Builders<Application>.Update
                        .Set(a => a.allergies[-1], payload)
                        .Set(a => a.LastUpdatedAt, DateTime.UtcNow);

                return await _ApplicationCollection.UpdateOneAsync(filter, update);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo in the UpdateAMedicalConditions method.");
                throw;
            }
        }

        #endregion


    }
}
