using System.ComponentModel.DataAnnotations;

namespace DayCare_ManagementSystem_API.Models
{
    public class MFAModel
    {
        public string QrCodeUrl { get; set; }
        public string ManualEntryCode { get; set; }
        public string MFAKey { get; set; }
    }

    public class TwoFALoginModel
    {
        [EmailAddress] public string Email { get; set; }
        public string Code { get; set; }
    }

    public class UpdateMfaFieldsModel
    {
        public string userId { get; set; }
        public bool? isFirstSignIn { get; set; }
        public bool? isMFAVerified { get; set; }
        public string QrCodeUrl { get; set; }
        public string ManualEntryCode { get; set; }
        public string MFAKey { get; set; }
    }
}



