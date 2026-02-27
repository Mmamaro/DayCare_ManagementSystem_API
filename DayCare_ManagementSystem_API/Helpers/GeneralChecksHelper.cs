using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Models.ValueObjects;
using DayCare_ManagementSystem_API.Repositories;
using System.Globalization;
using static System.Net.Mime.MediaTypeNames;
using Application = DayCare_ManagementSystem_API.Models.Application;

namespace DayCare_ManagementSystem_API.Helpers
{
    public class GeneralChecksHelper
    {
        private readonly ILogger<GeneralChecksHelper> _logger;
        private readonly IUser _userRepo;

        public GeneralChecksHelper(ILogger<GeneralChecksHelper> logger, IUser userRepo)
        {
            _logger = logger;
            _userRepo = userRepo;
        }

        public bool IsValidIdNumber(string idNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idNumber))
                    return false;

                // 1. Must be exactly 13 digits
                if (idNumber.Length != 13 || !idNumber.All(char.IsDigit))
                    return false;

                // 2. Validate date (YYMMDD)
                var datePart = idNumber.Substring(0, 6);

                if (!DateTime.TryParseExact(
                        datePart,
                        "yyMMdd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out _))
                    return false;

                // 3. Citizenship digit (position 11, index 10)
                var citizenship = idNumber[10];
                if (citizenship != '0' && citizenship != '1')
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the GeneralChecksHelper in the IsValidIdNumber method.");
                throw;
            }
        }

        public bool DoesDobMatchIdNumber(string idNumber, string DOB)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idNumber))
                    return false;

                if (!DateTime.TryParse(DOB, out var dateOfBirth))
                    return false;


                // Extract YYMMDD
                var yy = int.Parse(idNumber.Substring(0, 2));
                var mm = int.Parse(idNumber.Substring(2, 2));
                var dd = int.Parse(idNumber.Substring(4, 2));

                // Determine century
                var currentYearTwoDigits = DateTime.Now.Year % 100;
                var century = yy <= currentYearTwoDigits ? 2000 : 1900;

                var dobFromId = new DateTime(century + yy, mm, dd);

                return dobFromId.Date == dateOfBirth.Date;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the GeneralChecksHelper in the DoesDobMatchIdNumber method.");
                throw;
            }
        }


        public (bool,string) IsAgeAppropriate(int? enrollmentYear, string DOB)
        {
            try
            {
                var appropriateAge = int.Parse(Environment.GetEnvironmentVariable("AppropriateStudentAge")!);
                var enrollmentYr = Convert.ToInt16(enrollmentYear);

                if (!DateTime.TryParse(DOB, out var dateOfBirth))
                {
                    return (false, "Invalid date format");
                }

                var enrollmentDate = new DateTime(enrollmentYr, 1, 1);

                if (dateOfBirth > enrollmentDate)
                {
                    return (false, "Date of birth cannot be after the enrollment date.");
                }

                var totalDays = (enrollmentDate.Date - dateOfBirth.Date).Days;

                // Convert days to years (leap-year safe)
                var ageInYears = (int)(totalDays / 365.2425);

                if (ageInYears > appropriateAge)
                {
                    return (false, "Child is above acceptable age.");
                }

                return (true, "Age is appropriate.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the GeneralChecksHelper in the IsAgeAppropriate method");
                throw;
            }
        }

        public int GetAge(string DOB)
        {
            try
            {

                var today = DateTime.Today;

                DateTime.TryParse(DOB, out var dateOfBirth);


                var totalDays = (today.Date - dateOfBirth.Date).Days;

                // Convert days to years (leap-year safe)
                var ageInYears = (int)(totalDays / 365.2425);


                return ageInYears;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the GeneralChecksHelper in the GetAge");
                throw;
            }
        }

        public bool HasDuplicateAllergyNames(List<Allergy> allergies)
        {
            try
            {
                return allergies
                    .GroupBy(a => a.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Any(g => g.Count() > 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the GeneralChecksHelper in the HasDuplicateAllergyNames method");
                throw;
            }
        } 

        public bool HasDuplicateNames(List<MedicalCondition?> medicalConditions, List<Allergy?> allergies)
        {
            try
            {

                if (medicalConditions != null)
                {
                    var HasDuplicateMedicalCNames = medicalConditions
                    .GroupBy(a => a.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Any(g => g.Count() > 1);

                    if (HasDuplicateMedicalCNames) return true;
                }

                if (allergies != null)
                {
                    var HasDuplicateAllergyNames = allergies
                    .GroupBy(a => a.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Any(g => g.Count() > 1);

                    if (HasDuplicateAllergyNames) return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the GeneralChecksHelper in the HasDuplicateNames method");
                throw;
            }
        }
        public bool HasDuplicateNames(List<AddMedicalCondition?> medicalConditions, List<AddAllergy?> allergies, List<AddNextOfKin?> nextOfKins)
        {
            try
            {

                if (medicalConditions != null)
                {
                    var HasDuplicateMedicalCNames = medicalConditions
                    .GroupBy(a => a.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Any(g => g.Count() > 1);

                    if (HasDuplicateMedicalCNames) return true;
                }

                if (allergies != null)
                {
                    var HasDuplicateAllergyNames = allergies
                    .GroupBy(a => a.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Any(g => g.Count() > 1);

                    if (HasDuplicateAllergyNames) return true;
                }

                if (nextOfKins != null)
                {
                    var HasDuplicateIdNumberss = nextOfKins
                    .GroupBy(a => a.IdNumber.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Any(g => g.Count() > 1);

                    if (HasDuplicateIdNumberss) return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the GeneralChecksHelper in the HasDuplicateNames method");
                throw;
            }
        }

        public (bool,string) IsValidSeverity(List<AddMedicalCondition?> medicalConditions, List<AddAllergy?> allergies)
        {
            try
            {

                var validSeverities = new List<string>() { "low", "medium", "high" };


                if (medicalConditions != null)
                {
                    foreach (var medicalC in medicalConditions)
                    {
                        if (!validSeverities.Contains(medicalC.Severity.ToLower()))
                        {
                            return (false, $"{medicalC.Name} has invalid severity. Valid Severities are: low, medium and high");
                        }
                    }
                }
                
                if (allergies != null)
                {
                    foreach (var allergy in allergies)
                    {
                        if (!validSeverities.Contains(allergy.Severity.ToLower()))
                        {
                            return (false, $"{allergy.Name} has invalid severity. Valid Severities are: low, medium and high");
                        }
                    }
                }

                return (true,"success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the GeneralChecksHelper in the IsValidSeverity method");
                throw;
            }
        }

        public (bool,string) ValidApplicationPeriod(int? enrollmentYear)
        {
            try
            {

                var dayOfApplication = DateTime.Now;
                var monthOfApplication = dayOfApplication.Month;
                var yearOfApplication = dayOfApplication.Year;
                var followingYear = dayOfApplication.AddYears(1).Year;

                if ((monthOfApplication == 1 || monthOfApplication == 2) && enrollmentYear == yearOfApplication)
                {
                    return (true, "can apply for the provided year");
                }

                if (monthOfApplication >= 9 && monthOfApplication <= 12 && enrollmentYear == followingYear)
                {
                    return (true, "can apply for the provided year");
                }

                return (false, "Cannot apply for the provided year during this time");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the GeneralChecksHelper in the ValidApplicationPeriod method");
                throw;
            }
        }

        public string SetFullAddress(Address address)
        {
            try
            {

                var fullAddress = address.StreetNumber == null ? $"{address.StreetName},{address.Suburb},{address.City},{address.Province},{address.PostalCode},{address.Country}" : $"{address.StreetNumber},{address.StreetName},{address.Suburb},{address.City},{address.Province},{address.PostalCode},{address.Country}";

                return fullAddress;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the GeneralChecksHelper in the SetFullAddress mrthod");
                throw;
            }
        }

        public async Task<(int, string)> EmailAndIdNumberCheck(string id, string email, string idNUmber, User user)
        {
            try
            {
                if (!string.IsNullOrEmpty(email))
                {
                    var emailExists = await _userRepo.GetUserByEmail(email.ToLower());

                    if (emailExists != null && id != emailExists.Id)
                    {
                        return (409, "Email belongs to a different user");
                    }

                    if (emailExists == null && user.isMFAEnabled == true)
                    {
                        UpdateMfaFieldsModel mfaFieldsModel = new UpdateMfaFieldsModel()
                        {
                            isFirstSignIn = true,
                            isMFAVerified = false,
                            ManualEntryCode = null,
                            userId = user.Id,
                            MFAKey = null,
                            QrCodeUrl = null,
                        };

                        var mfaUpdated = await _userRepo.UpdateMFAfields(mfaFieldsModel);

                        if (mfaUpdated.ModifiedCount <= 0)
                        {
                            _logger.LogError("Could not update mfa fields");
                            return (400, "Could not update mfa fields");
                        }
                    }
                }

                if (!string.IsNullOrEmpty(idNUmber))
                {
                    var idNumberExists = await _userRepo.GetUserByIdNumber(idNUmber.ToLower());

                    if (idNumberExists != null && id != idNumberExists.Id)
                    {
                        return (409, "Id Number belongs to a different user");
                    }
                    else
                    {

                    }
                }

                    return (200, "Success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Users Controller in the EmailAndIdNumberCheck method.");
                throw;
            }
        }
    }
}
