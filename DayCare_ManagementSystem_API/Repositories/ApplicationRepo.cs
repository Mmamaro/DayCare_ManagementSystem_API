using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Models.ValueObjects;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using static QRCoder.PayloadGenerator;
using static System.Net.Mime.MediaTypeNames;
using Application = DayCare_ManagementSystem_API.Models.Application;

namespace DayCare_ManagementSystem_API.Repositories
{
    public interface IApplication
    {
        public Task<Application> AddApplication(Application payload);
        public Task<UpdateResult> AddNextOfKins(List<AddNextOfKin> payload, string applicationId);
        public Task<UpdateResult> AddMedicalConditions(List<AddMedicalCondition> payload, string applicationId);
        public Task<UpdateResult> AddAllergies(List<AddAllergy> payload, string applicationId);
        public Task<DeleteResult> DeleteApplication(string id);
        public Task<List<Application>> GetAllApplications();
        public Task<Application> GetApplicationById(string id);
        public Task<Application> GetApplicationByStudentIdNumber(string IdNumber);
        public Task<UpdateResult> UpdateApplicationMedicalConditions(string applicationId, MedicalCondition payload);
        public Task<UpdateResult> UpdateApplicationAllergies(string applicationId, Allergy payload);
        public Task<UpdateResult> UpdateStudentProfile(string applicationId, StudentProfile payload);
        public Task<UpdateResult> UpdateNextOfKin(string applicationId, NextOfKin payload);
        public Task<UpdateResult> UpdateStatus(string applicationId, UpdateApplicationStatus payload);
        public Task<List<Application>> GetApplicationByFilters(ApplicationFilters payload);
        public Task<UpdateResult> UpdateAreDocumentsSubmitted(string studentIdNumber, bool isSubmitted);

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

                await _ApplicationCollection.InsertOneAsync(payload);

                return payload;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo in the AddApplication method.");
                throw;

            }
        }

        #endregion

        #region [ Add NextOfKins ]
        public async Task<UpdateResult> AddNextOfKins(List<AddNextOfKin> payload, string applicationId)
        {
            try
            {
                var NextOfKins = new List<NextOfKin>();

                foreach (var x in payload)
                {
                    var person = new NextOfKin()
                    {
                        NextOfKinId = ObjectId.GenerateNewId().ToString(),
                        Email = x.Email,
                        FullName = x.FullName,
                        IdNumber = x.IdNumber,
                        PhoneNumber = x.PhoneNumber,
                        Relationship = x.Relationship

                    };

                    NextOfKins.Add(person);
                }

                var filter = Builders<Application>.Filter
                        .Eq(a => a.ApplicationId, applicationId);

                var update = Builders<Application>.Update
                    .PushEach(a => a.NextOfKin, NextOfKins)
                    .Set(a => a.LastUpdatedAt, DateTime.UtcNow);

                return await _ApplicationCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo in the AddMedicalConditions method.");
                throw;
            }
        }
        #endregion

        #region [ Add Allergies ]
        public async Task<UpdateResult> AddAllergies(List<AddAllergy> payload, string applicationId)
        {
            try
            {
                var Allergies = new List<Allergy>();

                foreach (var x in payload)
                {
                    var allergy = new Allergy()
                    {
                        AllergyId = ObjectId.GenerateNewId().ToString(),
                        Name = x.Name,
                        Notes = x.Notes,
                        Severity = x.Severity
                    };

                    Allergies.Add(allergy);
                }

                var filter = Builders<Application>.Filter
                        .Eq(a => a.ApplicationId, applicationId);

                var update = Builders<Application>.Update
                    .PushEach(a => a.Allergies, Allergies)
                    .Set(a => a.LastUpdatedAt, DateTime.UtcNow);

                return await _ApplicationCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo in the AddAllergies method.");
                throw;
            }
        }
        #endregion

        #region [ Add MedicalConditions ]
        public async Task<UpdateResult> AddMedicalConditions(List<AddMedicalCondition> payload, string applicationId)
        {
            try
            {
                var medicalConditions = new List<MedicalCondition>();

                foreach (var x in payload)
                {
                    var medicalCon = new MedicalCondition()
                    {
                        Severity = x.Severity,
                        Notes  = x.Notes,
                        Name = x.Name,
                        MedicalConditionId = ObjectId.GenerateNewId().ToString(),
                    };

                    medicalConditions.Add(medicalCon);
                }

                var filter = Builders<Application>.Filter
                        .Eq(a => a.ApplicationId, applicationId);

                var update = Builders<Application>.Update
                    .PushEach(a => a.MedicalConditions, medicalConditions)
                    .Set(a => a.LastUpdatedAt, DateTime.UtcNow);

                return await _ApplicationCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo in the AddAllergies method.");
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
                _logger.LogError(ex, "Error in the Application repo in the DeleteApplication method.");
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
                _logger.LogError(ex, "Error in the Application repo in the GetAllApplications method.");
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
                _logger.LogError(ex, "Error in the Application repo in the GetApplicationById method.");
                throw;
            }
        }

        #endregion

        #region [ Get Application By Student Id Number ]

        public async Task<Application> GetApplicationByStudentIdNumber(string IdNumber)
        {
            try
            {
                return await _ApplicationCollection.Find(c => c.StudentProfile.IdNumber == IdNumber).FirstOrDefaultAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo in the GetApplicationByStudentIdNumber method.");
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
                    filter &= builder.Eq(a => a.StudentProfile.IdNumber, payload.StudentIdNumber);
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
                _logger.LogError(ex, "Error in the Application repo in the GetApplicationByFilters method.");
                throw;
            }
        }
        #endregion

        #region [ Update Application Status ]

        public async Task<UpdateResult> UpdateStatus(string applicationId, UpdateApplicationStatus payload)
        {
            try
            {
                var filter = Builders<Application>.Filter
                    .Eq(a => a.ApplicationId, applicationId);

                var updates = new List<UpdateDefinition<Application>>();


                 updates.Add(Builders<Application>.Update
                    .Set(a => a.ApplicationStatus, payload.Status));

                if (!string.IsNullOrEmpty(payload.RejectionNotes))
                {
                    updates.Add(Builders<Application>.Update
                        .Set(a => a.RejectionNotes, payload.RejectionNotes));
                }

                updates.Add(Builders<Application>.Update
                    .Set(a => a.LastUpdatedAt, DateTime.UtcNow));

                var update = Builders<Application>.Update.Combine(updates);

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


                var update = Builders<Application>.Update
                        .Set("NextOfKin.$.FullName", payload.FullName)
                        .Set("NextOfKin.$.Relationship", payload.Relationship)
                        .Set("NextOfKin.$.IdNumber", payload.IdNumber)
                        .Set("NextOfKin.$.PhoneNumber", payload.PhoneNumber)
                        .Set("NextOfKin.$.Email", payload.Email)
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
                var filter = Builders<Application>.Filter
                    .Eq(a => a.ApplicationId, applicationId);

                var updates = new List<UpdateDefinition<Application>>();

                if (!string.IsNullOrEmpty(payload.FirstName))
                {
                    updates.Add(Builders<Application>.Update
                        .Set(a => a.StudentProfile.FirstName, payload.FirstName));
                }

                if (!string.IsNullOrEmpty(payload.LastName))
                {
                    updates.Add(Builders<Application>.Update
                        .Set(a => a.StudentProfile.LastName, payload.LastName));
                }

                if (!string.IsNullOrEmpty(payload.DateOfBirth))
                {
                    updates.Add(Builders<Application>.Update
                        .Set(a => a.StudentProfile.DateOfBirth, payload.DateOfBirth));
                }

                if (!string.IsNullOrEmpty(payload.Gender))
                {
                    updates.Add(Builders<Application>.Update
                        .Set(a => a.StudentProfile.Gender, payload.Gender));
                }

                if (!string.IsNullOrEmpty(payload.IdNumber))
                {
                    updates.Add(Builders<Application>.Update
                        .Set(a => a.StudentProfile.IdNumber, payload.IdNumber));
                }
                updates.Add(Builders<Application>.Update
                    .Set(a => a.LastUpdatedAt, DateTime.UtcNow));

                var update = Builders<Application>.Update.Combine(updates);

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
                        a => a.Allergies,
                        k => k.AllergyId == payload.AllergyId
                    )
                );


                var update = Builders<Application>.Update
                        .Set("Allergies.$.Name", payload.Name)
                        .Set("Allergies.$.Severity", payload.Severity)
                        .Set("Allergies.$.Notes", payload.Notes)
                        .Set(a => a.LastUpdatedAt, DateTime.UtcNow);

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

        public async Task<UpdateResult> UpdateApplicationMedicalConditions(string applicationId, MedicalCondition payload)
        {
            try
            {
                var filter = Builders<Application>.Filter.And(
                    Builders<Application>.Filter.Eq(a => a.ApplicationId, applicationId),
                    Builders<Application>.Filter.ElemMatch(
                        a => a.MedicalConditions,
                        k => k.MedicalConditionId == payload.MedicalConditionId
                    )
                );

                var update = Builders<Application>.Update
                        .Set("MedicalConditions.$.Name", payload.Name)
                        .Set("MedicalConditions.$.Notes", payload.Notes)
                        .Set("MedicalConditions.$.Severity", payload.Severity)
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

        #region [ Update Are Documents Submitted ]
        public async Task<UpdateResult> UpdateAreDocumentsSubmitted(string studentIdNumber, bool isSubmitted)
        {
            try
            {
                var filter = Builders<Application>.Filter
                    .Eq(a => a.StudentProfile.IdNumber, studentIdNumber);

                var update = Builders<Application>.Update
                    .Set(a => a.AreDocumentsSubmitted, isSubmitted)
                    .Set(a => a.LastUpdatedAt, DateTime.UtcNow);

                return await _ApplicationCollection.UpdateOneAsync(filter, update);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo in the UpdateIsDocumentsSubmitted method.");
                throw;
            }
        } 
        #endregion


    }
}
