using System.ComponentModel.DataAnnotations;

namespace DayCare_ManagementSystem_API.Models
{
    public class Credentials
    {
        public string Id { get; set; }
        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        [EmailAddress] public string? Email { get; set; }
        public string? Role { get; set; }
        public bool? isMFAEnabled { get; set; }
    }

    public class LoginCredentials
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? MFAToken { get; set; }
        public bool? isMFAEnabled { get; set; }
        public bool? isFirstSignIn { get; set; }
        public bool? isMFA_verified { get; set; }
    }
}
