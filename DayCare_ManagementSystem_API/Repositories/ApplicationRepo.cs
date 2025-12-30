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
        public Task<Application> AddApplication(ApplicationRequest payload, string SubmittedBy);
        public Task<UpdateResult> AddNextOfKins(List<AddNextOfKin> payload, string applicationId);
        public Task<UpdateResult> AddMedicalConditions(List<AddMedicalCondition> payload, string applicationId);
        public Task<UpdateResult> AddAllergies(List<AddAllergy> payload, string applicationId);
        public Task<DeleteResult> DeleteApplication(string id);
        public Task<DeleteResult> DeleteApplicationByNextOfKinIdNumber(string idNumber);
        public Task<List<Application>> GetAllApplications();
        public Task<Application> GetApplicationById(string id);
        public Task<Application> GetApplicationBySubmittedBy(string SubmittedBy);
        public Task<List<NextOfKin>> GetNextOfKins(string applicationId);
        public Task<Application> GetApplicationByStudentIdNumber(string IdNumber);
        public Task<Allergy> GetAllergyByName(string applicationId, string allergyName);
        public Task<List<Application>> GetApplicationByFilters(ApplicationFilters payload);
        public Task<MedicalCondition> GetMedicalConditionByName(string applicationId, string medicalCName);
        public Task<MedicalCondition> GetMedicalConditionById(string applicationId, string medicalCId);
        public Task<Allergy> GetAllergyById(string applicationId, string allergyId);
        public Task<UpdateResult> UpdateApplicationMedicalConditions(string applicationId, MedicalCondition payload);
        public Task<UpdateResult> UpdateApplicationAllergies(string applicationId, Allergy payload);
        public Task<UpdateResult> UpdateStudentProfile(string applicationId, StudentProfile payload);
        public Task<UpdateResult> UpdateNextOfKin(string applicationId, NextOfKin payload);
        public Task<UpdateResult> UpdateStatus(string applicationId, UpdateApplicationStatus payload);
        public Task<UpdateResult> UpdateSubmittedBy(string applicationId, string SubmittedBy);
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

        public async Task<Application> AddApplication(ApplicationRequest payload, string submittedBy)
        {
            try
            {
                var allergies = new List<Allergy>();
                var nextOfKins = new List<NextOfKin>();
                var medicalConditions = new List<MedicalCondition>();

                if (payload.allergies != null)
                {
                    foreach (var x in payload.allergies)
                    {
                        var allergy = new Allergy()
                        {
                            AllergyId = ObjectId.GenerateNewId().ToString(),
                            Name = x.Name.ToLower(),
                            Notes = x.Notes,
                            Severity = x.Severity.ToLower()
                        };

                        allergies.Add(allergy);
                    }
                }

                foreach (var x in payload.NextOfKins)
                {
                    var nextOfKIn = new NextOfKin()
                    {
                        NextOfKinId = ObjectId.GenerateNewId().ToString(),
                        Email = x.Email,
                        FullName = x.FullName,
                        IdNumber = x.IdNumber,
                        PhoneNumber = x.PhoneNumber,
                        Relationship = x.Relationship,
                    };

                    nextOfKins.Add(nextOfKIn);
                }

                if(payload.MedicalConditions != null)
                {
                    foreach (var x in payload.MedicalConditions)
                    {
                        var medicalC = new MedicalCondition()
                        {
                            MedicalConditionId = ObjectId.GenerateNewId().ToString(),
                            Name = x.Name.ToLower(),
                            Notes = x.Notes,
                            Severity = x.Severity.ToLower()
                        };

                        medicalConditions.Add(medicalC);
                    }
                }

                payload.StudentProfile.StudentProfileId = ObjectId.GenerateNewId().ToString();

                var application = new Application()
                {
                    ApplicationId = ObjectId.GenerateNewId().ToString(),
                    SubmittedAt = DateTime.Now.AddHours(2),
                    SubmittedBy = submittedBy,
                    LastUpdatedAt = DateTime.Now.AddHours(2),
                    Allergies = allergies,
                    EnrollmentYear = payload.EnrollmentYear,
                    MedicalConditions = medicalConditions,
                    NextOfKins = nextOfKins,
                    ApplicationStatus = "waiting",
                    StudentProfile = payload.StudentProfile,
                    AreDocumentsSubmitted = false
                };

                await _ApplicationCollection.InsertOneAsync(application);

                return application;

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
                    .PushEach(a => a.NextOfKins, NextOfKins)
                    .Set(a => a.LastUpdatedAt, DateTime.Now.AddHours(2));

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
                        Name = x.Name.ToLower(),
                        Notes = x.Notes,
                        Severity = x.Severity.ToLower(),
                    };

                    Allergies.Add(allergy);
                }

                var filter = Builders<Application>.Filter
                        .Eq(a => a.ApplicationId, applicationId);

                var update = Builders<Application>.Update
                    .PushEach(a => a.Allergies, Allergies)
                    .Set(a => a.LastUpdatedAt, DateTime.Now.AddHours(2));

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
                        Severity = x.Severity.ToLower(),
                        Notes  = x.Notes,
                        Name = x.Name.ToLower(),
                        MedicalConditionId = ObjectId.GenerateNewId().ToString(),
                    };

                    medicalConditions.Add(medicalCon);
                }

                var filter = Builders<Application>.Filter
                        .Eq(a => a.ApplicationId, applicationId);

                var update = Builders<Application>.Update
                    .PushEach(a => a.MedicalConditions, medicalConditions)
                    .Set(a => a.LastUpdatedAt, DateTime.Now.AddHours(2));

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

        #region [ Delete Application By NextOFKinId ]

        public async Task<DeleteResult> DeleteApplicationByNextOfKinIdNumber(string idNumber)
        {
            try
            {
                var filter = Builders<Application>.Filter.ElemMatch(
                    a => a.NextOfKins,
                    n => n.IdNumber == idNumber
                );

                return await _ApplicationCollection.DeleteOneAsync(filter);
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

        #region [ Get Application By SubmittedBy ]

        public async Task<Application> GetApplicationBySubmittedBy(string SubmittedBy)
        {
            try
            {
                return await _ApplicationCollection.Find(c => c.SubmittedBy == SubmittedBy).FirstOrDefaultAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo in the GetApplicationBySubmittedBy method.");
                throw;
            }
        }

        #endregion

        #region [ Get Application NextOfKins By Id ]

        public async Task<List<NextOfKin>> GetNextOfKins(string applicationId)
        {
            try
            {
                var application = await _ApplicationCollection.Find(c => c.ApplicationId == applicationId).FirstOrDefaultAsync();

                return application.NextOfKins;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo in the GetApplicationById method.");
                throw;
            }
        }

        #endregion

        #region [ Get MedicalCondition By Name ]

        public async Task<MedicalCondition> GetMedicalConditionByName( string applicationId,string medicalCName)
        {
            try
            {
                var application = await _ApplicationCollection
                    .Find(a => a.ApplicationId == applicationId)
                    .FirstOrDefaultAsync();

                if (application == null) return null;

                var medicalCondition = application?.MedicalConditions.FirstOrDefault(a => a.Name == medicalCName.ToLower());

                if (medicalCondition == null) return null;

                return medicalCondition;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo in the GetMedicalConditionByName method.");
                throw;
            }
        }


        #endregion

        #region [ Get MedicalCondition By Id ]

        public async Task<MedicalCondition> GetMedicalConditionById(string applicationId, string medicalCId)
        {
            try
            {
                var application = await _ApplicationCollection
                    .Find(a => a.ApplicationId == applicationId)
                    .FirstOrDefaultAsync();

                if (application == null) return null;

                var medicalCondition = application?.MedicalConditions.FirstOrDefault(a => a.MedicalConditionId == medicalCId);

                if (medicalCondition == null) return null;

                return medicalCondition;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo in the GetMedicalConditionById method.");
                throw;
            }
        }


        #endregion

        #region [ Get Allergy By Name ]

        public async Task<Allergy> GetAllergyByName(string applicationId, string allergyName)
        {
            try
            {
                var application = await _ApplicationCollection
                    .Find(a => a.ApplicationId == applicationId)
                    .FirstOrDefaultAsync();

                if (application == null) return null;

                var allergy = application?.Allergies.FirstOrDefault(a => a.Name == allergyName.ToLower());

                if (allergy == null) return null;

                return allergy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo in the GetAllergyByName method.");
                throw;
            }
        }


        #endregion

        #region [ Get Allergy By Name ]

        public async Task<Allergy> GetAllergyById(string applicationId, string allergyId)
        {
            try
            {
                var application = await _ApplicationCollection
                    .Find(a => a.ApplicationId == applicationId)
                    .FirstOrDefaultAsync();

                if (application == null) return null;

                var allergy = application?.Allergies.FirstOrDefault(a => a.AllergyId == allergyId);

                if (allergy == null) return null;

                return allergy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo in the GetAllergyById method.");
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
                        a => a.NextOfKins,
                        k => k.IdNumber == payload.NextOfKinIdNumber
                    );
                }

                if (payload.EnrollmentYear.HasValue)
                {
                    filter &= builder.Eq(a => a.EnrollmentYear, payload.EnrollmentYear);
                }

                if (payload.AreDocumentsSubmitted.HasValue)
                {
                    filter &= builder.Eq(a => a.AreDocumentsSubmitted, payload.AreDocumentsSubmitted);
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
                    .Set(a => a.ApplicationStatus, payload.Status.ToLower()));

                if (!string.IsNullOrEmpty(payload.RejectionNotes))
                {
                    updates.Add(Builders<Application>.Update
                        .Set(a => a.RejectionNotes, payload.RejectionNotes));
                }

                updates.Add(Builders<Application>.Update
                    .Set(a => a.LastUpdatedAt, DateTime.Now.AddHours(2)));

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

        #region [ Update Application UpdateSubmittedBy ]

        public async Task<UpdateResult> UpdateSubmittedBy(string applicationId, string SubmittedBy)
        {
            try
            {
                var filter = Builders<Application>.Filter
                    .Eq(a => a.ApplicationId, applicationId);

                var updates = new List<UpdateDefinition<Application>>();

                updates.Add(Builders<Application>.Update
                    .Set(a => a.SubmittedBy, SubmittedBy));

                updates.Add(Builders<Application>.Update
                    .Set(a => a.LastUpdatedAt, DateTime.Now.AddHours(2)));

                var update = Builders<Application>.Update.Combine(updates);

                return await _ApplicationCollection.UpdateOneAsync(filter, update);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Application repo in the UpdateSubmittedBy method.");
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
                        a => a.NextOfKins,
                        k => k.NextOfKinId == payload.NextOfKinId
                    )
                );


                var update = Builders<Application>.Update
                        .Set("NextOfKin.$.FullName", payload.FullName)
                        .Set("NextOfKin.$.Relationship", payload.Relationship)
                        .Set("NextOfKin.$.IdNumber", payload.IdNumber)
                        .Set("NextOfKin.$.PhoneNumber", payload.PhoneNumber)
                        .Set("NextOfKin.$.Email", payload.Email.ToLower())
                        .Set(a => a.LastUpdatedAt, DateTime.Now.AddHours(2));

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
                        .Set(a => a.StudentProfile.Gender, payload.Gender.ToLower()));
                }

                if (!string.IsNullOrEmpty(payload.IdNumber))
                {
                    updates.Add(Builders<Application>.Update
                        .Set(a => a.StudentProfile.IdNumber, payload.IdNumber));
                }
                updates.Add(Builders<Application>.Update
                    .Set(a => a.LastUpdatedAt, DateTime.Now.AddHours(2)));

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
                        .Set("Allergies.$.Name", payload.Name.ToLower())
                        .Set("Allergies.$.Severity", payload.Severity.ToLower())
                        .Set("Allergies.$.Notes", payload.Notes)
                        .Set(a => a.LastUpdatedAt, DateTime.Now.AddHours(2));

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
                        .Set("MedicalConditions.$.Name", payload.Name.ToLower())
                        .Set("MedicalConditions.$.Notes", payload.Notes)
                        .Set("MedicalConditions.$.Severity", payload.Severity)
                        .Set(a => a.LastUpdatedAt, DateTime.Now.AddHours(2));

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
                    .Set(a => a.LastUpdatedAt, DateTime.Now.AddHours(2));

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
