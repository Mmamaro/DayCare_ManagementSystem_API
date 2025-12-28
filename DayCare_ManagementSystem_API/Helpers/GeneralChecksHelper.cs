using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Models.ValueObjects;
using DayCare_ManagementSystem_API.Repositories;
using System.Globalization;

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

                // 4. Luhn checksum
                return PassesLuhnCheck(idNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the GeneralChecksHelper in the IsValidIdNumber");
                throw;
            }
        }

        private bool PassesLuhnCheck(string idNumber)
        {
            try
            {
                int sum = 0;
                bool alternate = false;

                // Exclude last digit (checksum)
                for (int i = idNumber.Length - 2; i >= 0; i--)
                {
                    int n = idNumber[i] - '0';

                    if (alternate)
                    {
                        n *= 2;
                        if (n > 9)
                            n -= 9;
                    }

                    sum += n;
                    alternate = !alternate;
                }

                int checksum = (10 - (sum % 10)) % 10;
                int lastDigit = idNumber[^1] - '0';

                return checksum == lastDigit;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the GeneralChecksHelper in the PassesLuhnCheck");
                throw;
            }
        }

        public (bool,string) IsAgeAppropriate(string DOB)
        {
            try
            {
                var appropriateAge = int.Parse(Environment.GetEnvironmentVariable("AppropriateStudentAge")!);

                if (!DateTime.TryParse(DOB, out var dateOfBirth))
                {
                    return (false, "Invalid date format");
                }

                var today = DateTime.Today;

                var age = today.Year - dateOfBirth.Year;

                if (age > appropriateAge)
                {
                    return (false, "Child is above acceptable age.");
                }


                return (true, "age is appropriate");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the GeneralChecksHelper in the IsAgeAppropriate");
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
                _logger.LogError(ex, "Error in the GeneralChecksHelper in the PassesLuhnCheck");
                throw;
            }
        } 

        public bool HasDuplicateNames(List<MedicalCondition?> medicalConditions, List<Allergy?> allergies)
        {
            try
            {

                if (medicalConditions.Any())
                {
                    var HasDuplicateMedicalCNames = medicalConditions
                    .GroupBy(a => a.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Any(g => g.Count() > 1);

                    if (HasDuplicateMedicalCNames) return true;
                }

                if (allergies.Any())
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
                _logger.LogError(ex, "Error in the GeneralChecksHelper in the PassesLuhnCheck");
                throw;
            }
        }
        public bool HasDuplicateNames(List<AddMedicalCondition?> medicalConditions, List<AddAllergy?> allergies)
        {
            try
            {

                if (medicalConditions.Any())
                {
                    var HasDuplicateMedicalCNames = medicalConditions
                    .GroupBy(a => a.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Any(g => g.Count() > 1);

                    if (HasDuplicateMedicalCNames) return true;
                }

                if (allergies.Any())
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
                _logger.LogError(ex, "Error in the GeneralChecksHelper in the PassesLuhnCheck");
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

                        if (mfaUpdated.IsAcknowledged == false)
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
