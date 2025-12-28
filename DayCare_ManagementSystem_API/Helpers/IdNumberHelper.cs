using System.Globalization;

namespace DayCare_ManagementSystem_API.Helpers
{
    public class IdNumberHelper
    {
        private readonly ILogger<IdNumberHelper> _logger;

        public IdNumberHelper(ILogger<IdNumberHelper> logger)
        {
            _logger = logger;
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
                _logger.LogError(ex, "Error in the IdNumberHelper in the IsValidIdNumber");
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
                _logger.LogError(ex, "Error in the IdNumberHelper in the PassesLuhnCheck");
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
                _logger.LogError(ex, "Error in the IdNumberHelper in the IsAgeAppropriate");
                throw;
            }
        }
    }
}
