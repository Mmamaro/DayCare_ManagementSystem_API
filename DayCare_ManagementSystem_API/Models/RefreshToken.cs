

namespace DayCare_ManagementSystem_API.Models
{

    public class RefreshToken
    {
        public string? Id { get; set; }
        public string Token { get; set; }
        public string UserId { get; set; }
        public DateTime ExpiresOn { get; set; }
    }

    public class RefreshTokenRequest
    {
        public string Email { get; set; }
        public string RefreshToken { get; set; }
    }
}
