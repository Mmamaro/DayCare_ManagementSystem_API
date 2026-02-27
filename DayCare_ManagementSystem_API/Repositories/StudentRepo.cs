using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Models.ValueObjects;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DayCare_ManagementSystem_API.Repositories
{
    public interface IStudent
    {
        public Task<Student> AddStudent(Student payload);
        public Task<UpdateResult> AddNextOfKins(List<AddNextOfKin> payload, string StudentId);
        public Task<UpdateResult> AddMedicalConditions(List<AddMedicalCondition> payload, string StudentId);
        public Task<UpdateResult> AddAllergies(List<AddAllergy> payload, string StudentId);
        public Task<DeleteResult> DeleteStudent(string id);
        public Task<List<Student>> GetAllStudents();
        public Task<Student> GetStudentById(string id);
        public Task<Student> GetStudentByStudentIdNumber(string id);
        public Task<List<NextOfKin>> GetNextOfKins(string StudentId);
        public Task<List<Allergy>> GetAllergies(string StudentId);
        public Task<List<MedicalCondition>> GetMedicalConditions(string StudentId);
        public Task<List<Student>> GetStudentsByKinIdNumber(string kinIdNumber);
        public Task<List<Student>> GetStudentsByKinId(string kinId);
        public Task<NextOfKin> GetNextOfKinByIdNumber(string StudentId, string kinIdNumber);
        public Task<Allergy> GetAllergyByName(string StudentId, string allergyName);
        public Task<MedicalCondition> GetMedicalConditionByName(string StudentId, string medicalCName);
        public Task<MedicalCondition> GetMedicalConditionById(string StudentId, string medicalCId);
        public Task<Allergy> GetAllergyById(string StudentId, string allergyId);
        public Task<UpdateResult> UpdateStudentMedicalConditions(string StudentId, MedicalCondition payload);
        public Task<UpdateResult> UpdateStudentAllergies(string StudentId, Allergy payload);
        public Task<UpdateResult> UpdateStudentProfile(string StudentId, StudentProfile payload);
        public Task<UpdateResult> UpdateNextOfKin(string StudentId, NextOfKin payload);
        public Task<UpdateResult> UpdateIsActive(UpdateIsActive payload);
        public Task<UpdateResult> RemoveNextOfKin(string studentId, string nextOfKinId);
        public Task<UpdateResult> UpdateAdress(string applicationId, Address payload);
    }
    public class StudentRepo : IStudent
    {
        #region [ Constructor ]
        private readonly ILogger<StudentRepo> _logger;
        private readonly IMongoCollection<Student> _studentsCollections;

        public StudentRepo(IOptions<DBSettings> Dbsettings, IMongoClient client, ILogger<StudentRepo> logger)
        {
            var database = client.GetDatabase(Dbsettings.Value.DatabaseName);

            _studentsCollections = database.GetCollection<Student>(Dbsettings.Value.StudentsCollection);
            _logger = logger;
        }
        #endregion

        #region [ Add Student ]

        public async Task<Student> AddStudent(Student payload)
        {
            try
            { 

                await _studentsCollections.InsertOneAsync(payload);

                return payload;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the AddStudent method.");
                throw;

            }
        }

        #endregion

        #region [ Add NextOfKins ]
        public async Task<UpdateResult> AddNextOfKins(List<AddNextOfKin> payload, string StudentId)
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

                var filter = Builders<Student>.Filter
                        .Eq(a => a.StudentId, StudentId);

                var update = Builders<Student>.Update
                    .PushEach(a => a.NextOfKins, NextOfKins)
                    .Set(a => a.LastUpdatedAt, DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

                return await _studentsCollections.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the AddNextOfKins method.");
                throw;
            }
        }
        #endregion

        #region [ Add Allergies ]
        public async Task<UpdateResult> AddAllergies(List<AddAllergy> payload, string StudentId)
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

                var filter = Builders<Student>.Filter
                        .Eq(a => a.StudentId, StudentId);

                var update = Builders<Student>.Update
                    .PushEach(a => a.Allergies, Allergies)
                    .Set(a => a.LastUpdatedAt, DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

                return await _studentsCollections.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the AddAllergies method.");
                throw;
            }
        }
        #endregion

        #region [ Add MedicalConditions ]
        public async Task<UpdateResult> AddMedicalConditions(List<AddMedicalCondition> payload, string StudentId)
        {
            try
            {
                var medicalConditions = new List<MedicalCondition>();

                foreach (var x in payload)
                {
                    var medicalCon = new MedicalCondition()
                    {
                        Severity = x.Severity.ToLower(),
                        Notes = x.Notes,
                        Name = x.Name.ToLower(),
                        MedicalConditionId = ObjectId.GenerateNewId().ToString(),
                    };

                    medicalConditions.Add(medicalCon);
                }

                var filter = Builders<Student>.Filter
                        .Eq(a => a.StudentId, StudentId);

                var update = Builders<Student>.Update
                    .PushEach(a => a.MedicalConditions, medicalConditions)
                    .Set(a => a.LastUpdatedAt, DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

                return await _studentsCollections.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the AddAllergies method.");
                throw;
            }
        }
        #endregion

        #region [ Delete Student ]

        public async Task<DeleteResult> DeleteStudent(string id)
        {
            try
            {
                return await _studentsCollections.DeleteOneAsync(c => c.StudentId == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the DeleteStudent method.");
                throw;
            }
        }

        #endregion

        #region [ Get All Students ]

        public async Task<List<Student>> GetAllStudents()
        {
            try
            {
                return await _studentsCollections.Find(c => true).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the GetAllStudents method.");
                throw;
            }
        }

        #endregion

        #region [ Get Student By Id ]

        public async Task<Student> GetStudentById(string id)
        {
            try
            {
                return await _studentsCollections.Find(c => c.StudentId == id).FirstOrDefaultAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the GetStudentById method.");
                throw;
            }
        }

        #endregion

        #region [ Get Students By KinIdNumber ]

        public async Task<List<Student>> GetStudentsByKinIdNumber(string kinIdNumber)
        {
            try
            {

                var filter = Builders<Student>.Filter
                    .ElemMatch(s => s.NextOfKins, k => k.IdNumber == kinIdNumber);

                return await _studentsCollections.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the GetStudentsByKinIdNumber method.");
                throw;
            }
        }

        #endregion

        #region [ Get Students By KinId ]

        public async Task<List<Student>> GetStudentsByKinId(string kinId)
        {
            try
            {

                var filter = Builders<Student>.Filter
                    .ElemMatch(s => s.NextOfKins, k => k.NextOfKinId == kinId);

                return await _studentsCollections.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the GetStudentsByKinId method.");
                throw;
            }
        }

        #endregion

        #region [ Get Student NextOfKins By Id ]

        public async Task<List<NextOfKin>> GetNextOfKins(string StudentId)
        {
            try
            {
                var Student = await _studentsCollections.Find(c => c.StudentId == StudentId).FirstOrDefaultAsync();

                return Student.NextOfKins;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the GetNextOfKins method.");
                throw;
            }
        }

        #endregion

        #region [ Get Student NextOfKin By Id Number ]

        public async Task<NextOfKin> GetNextOfKinByIdNumber(string StudentId, string kinIdNumber)
        {
            try
            {
                var Student = await _studentsCollections.Find(c => c.StudentId == StudentId).FirstOrDefaultAsync();

                var nextOfkin = Student.NextOfKins.FirstOrDefault( x => x.IdNumber == kinIdNumber );

                return nextOfkin;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the GetNextOfKins method.");
                throw;
            }
        }

        #endregion

        #region [ Get Student Allergies By Id ]

        public async Task<List<Allergy>> GetAllergies(string StudentId)
        {
            try
            {
                var Student = await _studentsCollections.Find(c => c.StudentId == StudentId).FirstOrDefaultAsync();

                return Student.Allergies;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the GetAllergies method.");
                throw;
            }
        }

        #endregion

        #region [ Get Student MedicalConditions By Id ]
        public async Task<List<MedicalCondition>> GetMedicalConditions(string StudentId)
        {
            try
            {
                var Student = await _studentsCollections.Find(c => c.StudentId == StudentId).FirstOrDefaultAsync();

                return Student.MedicalConditions;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the GetMedicalConditions method.");
                throw;
            }
        }

        #endregion

        #region [ Get MedicalCondition By Name ]

        public async Task<MedicalCondition> GetMedicalConditionByName(string StudentId, string medicalCName)
        {
            try
            {
                var Student = await _studentsCollections
                    .Find(a => a.StudentId == StudentId)
                    .FirstOrDefaultAsync();

                if (Student == null) return null;

                var medicalCondition = Student?.MedicalConditions.FirstOrDefault(a => a.Name == medicalCName.ToLower());

                if (medicalCondition == null) return null;

                return medicalCondition;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the GetMedicalConditionByName method.");
                throw;
            }
        }


        #endregion

        #region [ Get MedicalCondition By Id ]

        public async Task<MedicalCondition> GetMedicalConditionById(string StudentId, string medicalCId)
        {
            try
            {
                var Student = await _studentsCollections
                    .Find(a => a.StudentId == StudentId)
                    .FirstOrDefaultAsync();

                if (Student == null) return null;

                var medicalCondition = Student?.MedicalConditions.FirstOrDefault(a => a.MedicalConditionId == medicalCId);

                if (medicalCondition == null) return null;

                return medicalCondition;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the GetMedicalConditionById method.");
                throw;
            }
        }


        #endregion

        #region [ Get Allergy By Name ]

        public async Task<Allergy> GetAllergyByName(string StudentId, string allergyName)
        {
            try
            {
                var Student = await _studentsCollections
                    .Find(a => a.StudentId == StudentId)
                    .FirstOrDefaultAsync();

                if (Student == null) return null;

                var allergy = Student?.Allergies.FirstOrDefault(a => a.Name == allergyName.ToLower());

                if (allergy == null) return null;

                return allergy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the GetAllergyByName method.");
                throw;
            }
        }


        #endregion

        #region [ Get Allergy By Name ]

        public async Task<Allergy> GetAllergyById(string StudentId, string allergyId)
        {
            try
            {
                var Student = await _studentsCollections
                    .Find(a => a.StudentId == StudentId)
                    .FirstOrDefaultAsync();

                if (Student == null) return null;

                var allergy = Student?.Allergies.FirstOrDefault(a => a.AllergyId == allergyId);

                if (allergy == null) return null;

                return allergy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the GetAllergyById method.");
                throw;
            }
        }


        #endregion

        #region [ Get Student By Student Id Number ]

        public async Task<Student> GetStudentByStudentIdNumber(string IdNumber)
        {
            try
            {
                return await _studentsCollections.Find(c => c.StudentProfile.IdNumber == IdNumber).FirstOrDefaultAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the GetStudentByStudentIdNumber method.");
                throw;
            }
        }

        #endregion

        #region [ Update Student IsActive ]

        public async Task<UpdateResult> UpdateIsActive(UpdateIsActive payload)
        {
            try
            {
                var filter = Builders<Student>.Filter
                    .Eq(a => a.StudentId, payload.StudentId);

                var update = Builders<Student>.Update
                    .Set(a => a.IsActive, payload.IsActive)
                    .Set(a => a.LastUpdatedAt, DateTime.Now.AddHours(2).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

                return await _studentsCollections.UpdateOneAsync(filter, update);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the UpdateIsActive method.");
                throw;
            }
        }

        #endregion

        #region [ Update Application UpdateAdress ]

        public async Task<UpdateResult> UpdateAdress(string studentId, Address payload)
        {
            try
            {
                var update = Builders<Student>.Update
                    .Set(a => a.Address, payload)
                    .Set(a => a.LastUpdatedAt, DateTime.Now.AddHours(2).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

                var result = await _studentsCollections
                    .UpdateOneAsync(a => a.StudentId == studentId, update);

                return result;


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the UpdateAdress method.");
                throw;
            }
        }

        #endregion

        #region [ Update Student NextOfKin ]

        public async Task<UpdateResult> UpdateNextOfKin(string StudentId, NextOfKin payload)
        {
            try
            {
                var filter = Builders<Student>.Filter.And(
                    Builders<Student>.Filter.Eq(a => a.StudentId, StudentId),
                    Builders<Student>.Filter.ElemMatch(
                        a => a.NextOfKins,
                        k => k.NextOfKinId == payload.NextOfKinId
                    )
                );


                var update = Builders<Student>.Update
                        .Set("NextOfKins.$.FullName", payload.FullName)
                        .Set("NextOfKins.$.Relationship", payload.Relationship)
                        .Set("NextOfKins.$.IdNumber", payload.IdNumber)
                        .Set("NextOfKins.$.PhoneNumber", payload.PhoneNumber)
                        .Set("NextOfKins.$.Email", payload.Email.ToLower())
                        .Set(a => a.LastUpdatedAt, DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

                return await _studentsCollections.UpdateOneAsync(filter, update);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the UpdateNextOfKin method.");
                throw;
            }
        }

        #endregion

        #region [ Update Student ]

        public async Task<UpdateResult> UpdateStudentProfile(string StudentId, StudentProfile payload)
        {
            try
            {
                var filter = Builders<Student>.Filter
                    .Eq(a => a.StudentId, StudentId);

                var updates = new List<UpdateDefinition<Student>>();

                if (!string.IsNullOrEmpty(payload.FirstName))
                {
                    updates.Add(Builders<Student>.Update
                        .Set(a => a.StudentProfile.FirstName, payload.FirstName));
                }

                if (!string.IsNullOrEmpty(payload.LastName))
                {
                    updates.Add(Builders<Student>.Update
                        .Set(a => a.StudentProfile.LastName, payload.LastName));
                }

                if (!string.IsNullOrEmpty(payload.DateOfBirth))
                {
                    updates.Add(Builders<Student>.Update
                        .Set(a => a.StudentProfile.DateOfBirth, payload.DateOfBirth));
                }

                if (!string.IsNullOrEmpty(payload.Gender))
                {
                    updates.Add(Builders<Student>.Update
                        .Set(a => a.StudentProfile.Gender, payload.Gender.ToLower()));
                }

                if (!string.IsNullOrEmpty(payload.IdNumber))
                {
                    updates.Add(Builders<Student>.Update
                        .Set(a => a.StudentProfile.IdNumber, payload.IdNumber));
                }
                updates.Add(Builders<Student>.Update
                    .Set(a => a.LastUpdatedAt, DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")));

                var update = Builders<Student>.Update.Combine(updates);

                return await _studentsCollections.UpdateOneAsync(filter, update);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the UpdateStudentProfile method.");
                throw;
            }
        }

        #endregion

        #region [ Update Student Allergies ]

        public async Task<UpdateResult> UpdateStudentAllergies(string StudentId, Allergy payload)
        {
            try
            {
                var filter = Builders<Student>.Filter.And(
                    Builders<Student>.Filter.Eq(a => a.StudentId, StudentId),
                    Builders<Student>.Filter.ElemMatch(
                        a => a.Allergies,
                        k => k.AllergyId == payload.AllergyId
                    )
                );


                var update = Builders<Student>.Update
                        .Set("Allergies.$.Name", payload.Name.ToLower())
                        .Set("Allergies.$.Severity", payload.Severity.ToLower())
                        .Set("Allergies.$.Notes", payload.Notes)
                        .Set(a => a.LastUpdatedAt, DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

                return await _studentsCollections.UpdateOneAsync(filter, update);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the UpdateAllergies method.");
                throw;
            }
        }

        #endregion

        #region [ Update Student Medical Conditions ]

        public async Task<UpdateResult> UpdateStudentMedicalConditions(string StudentId, MedicalCondition payload)
        {
            try
            {
                var filter = Builders<Student>.Filter.And(
                    Builders<Student>.Filter.Eq(a => a.StudentId, StudentId),
                    Builders<Student>.Filter.ElemMatch(
                        a => a.MedicalConditions,
                        k => k.MedicalConditionId == payload.MedicalConditionId
                    )
                );

                var update = Builders<Student>.Update
                        .Set("MedicalConditions.$.Name", payload.Name.ToLower())
                        .Set("MedicalConditions.$.Notes", payload.Notes)
                        .Set("MedicalConditions.$.Severity", payload.Severity)
                        .Set(a => a.LastUpdatedAt, DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

                return await _studentsCollections.UpdateOneAsync(filter, update);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the UpdateAMedicalConditions method.");
                throw;
            }
        }

        #endregion


        #region [ Remove NextOfKin ]
        public async Task<UpdateResult> RemoveNextOfKin(string studentId, string nextOfKinId)
        {
            try
            {
                var filter = Builders<Student>.Filter.Eq(s => s.StudentId, studentId);

                var update = Builders<Student>.Update.PullFilter(
                    s => s.NextOfKins,
                    k => k.NextOfKinId == nextOfKinId
                );

                return await _studentsCollections.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Student repo in the RemoveNextOfKin method.");
                throw;
            }
        } 
        #endregion

    }
}
