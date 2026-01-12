using System.Text.RegularExpressions;

namespace DayCare_ManagementSystem_API.Helper
{
    public class PasswordHelper
    {
        public bool IsPasswordValid(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            // Regex pattern: 
            // - Minimum 8 characters
            // - At least one number (\d)
            // - At least one special character ([^a-zA-Z0-9])
            string pattern = @"^(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$";

            return Regex.IsMatch(password, pattern);
        }

        public string GenerateRandomPassword()
        {
            const int length = 12;
            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string specialChars = "!@#$%&*()_?";

            Random random = new Random();

            // Ensure password contains at least one character from each category
            string allChars = upperCase + lowerCase + digits + specialChars;
            char[] password = new char[length];

            password[0] = upperCase[random.Next(upperCase.Length)];
            password[1] = lowerCase[random.Next(lowerCase.Length)];
            password[2] = digits[random.Next(digits.Length)];
            password[3] = digits[random.Next(digits.Length)]; // Ensure at least two digits
            password[4] = specialChars[random.Next(specialChars.Length)];

            // Fill the rest of the password length with random characters from all categories
            for (int i = 5; i < length; i++)
            {
                password[i] = allChars[random.Next(allChars.Length)];
            }

            // Shuffle the array to prevent predictable sequences
            password = password.OrderBy(x => random.Next()).ToArray();

            return new string(password);
        }

    }
}
