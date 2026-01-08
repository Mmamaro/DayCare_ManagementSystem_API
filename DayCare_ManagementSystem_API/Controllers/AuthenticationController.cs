using DayCare_ManagementSystem_API.Helper;
using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Models.DTOs;
using DayCare_ManagementSystem_API.Repositories;
using DayCare_ManagementSystem_API.Service;
using DayCare_ManagementSystem_API.Services;
using Google.Authenticator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime;
using System.Security.Claims;

namespace DayCare_ManagementSystem_API.Controllers
{
   
    [Authorize]
    [Route("api/auth")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly ILogger<AuthenticationController> _logger;
        private readonly IUser _userRepo;
        private readonly EmailService _emailService;
        private readonly MFAService _mfaService;
        private readonly IToken _tokenService;
        private readonly IConfiguration _config;
        private readonly TokensHelper _tokensHelper;
        private readonly IRefreshToken _refreshTokenRepo;
        private readonly PasswordHelper _passwordHelper;
        private readonly IUserAudit _userAudit;


        #region [ Constructor ]
        public AuthenticationController(ILogger<AuthenticationController> logger, IUser userRepo, EmailService emailService,
            MFAService mfaService, IToken tokenService, IConfiguration config, TokensHelper tokensHelper,
            IRefreshToken refreshTokenRepo, PasswordHelper passwordHelper, IUserAudit userAudit)
        {


            _emailService = emailService;
            _logger = logger;
            _userRepo = userRepo;
            _mfaService = mfaService;
            _tokenService = tokenService;
            _config = config;
            _tokensHelper = tokensHelper;
            _refreshTokenRepo = refreshTokenRepo;
            _passwordHelper = passwordHelper;
            _userAudit = userAudit;
        }
        #endregion


        #region [ Normal Login ]
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult> NormalLogin(LoginModel request)
        {
            try
            {
                var creds = new LoginCredentials();

                var user = await _userRepo.Login(request);

                if (user == null)
                {
                    return Unauthorized(new { Message = "Bad Credentials" });
                }

                if(user.isMFAEnabled == true)
                {
                    var mfaToken = _tokenService.GenerateMfaToken(user);

                    creds = new LoginCredentials
                    {
                        isMFAEnabled = user.isMFAEnabled,
                        AccessToken = null,
                        RefreshToken = null,
                        MFAToken = mfaToken,
                        isFirstSignIn = user.isFirstSignIn,
                        isMFA_verified = user.isMFAVerified,
                    };
                }
                else
                {

                    var (accessToken, refreshToken) = await _tokensHelper.GenerateTokens(user);

                    if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                    {
                        return BadRequest(new { Messaage = "Could not generate tokens" });
                    }

                    creds = new LoginCredentials
                    {
                        isMFAEnabled = user.isMFAEnabled,
                        AccessToken = accessToken,
                        RefreshToken = refreshToken,
                        MFAToken = null,
                        isFirstSignIn = user.isFirstSignIn,
                        isMFA_verified = user.isMFAVerified,
                    };


                }

                return Ok(creds);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in the controller while trying to log in: {ex}");
                return StatusCode(500, new { Message = "Encountered an error" });
            }
        }

        #endregion

        #region [ 2FA Setup ]
        [HttpGet("mfa-setup")]
        public async Task<ActionResult> MFASetup(string email)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserId = User?.FindFirstValue(ClaimTypes.Sid)?.ToString();
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();

                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(new { Message = "Provide Email" });
                }

                if (email.ToLower() != tokenUserEmail?.ToLower() || tokenType != "mfa-token")
                {
                    return Unauthorized(new { Message = "Unauthorized User/Invalid token" });
                }

                var loginResponse = await _userRepo.GetUserByEmail(email);

                if (loginResponse == null)
                {
                    _logger.LogError($"User {email} does not exist or invalid credentials provided");
                    return Unauthorized(new { Message = "Bad Credentials" });
                }

                if (loginResponse.isMFAEnabled == true)
                {

                    // Setup MFA for the user
                    var twoFAConfig = await _mfaService.MFASetup(email);

                    UpdateMfaFieldsModel mfaFieldsModel = new UpdateMfaFieldsModel()
                    {
                        isFirstSignIn = true,
                        isMFAVerified = false,
                        ManualEntryCode = twoFAConfig.ManualEntryCode,
                        userId = loginResponse.Id,
                        MFAKey = twoFAConfig.MFAKey,
                        QrCodeUrl = twoFAConfig.QrCodeUrl,
                    };

                    var mfaFieldUpdate = await _userRepo.UpdateMFAfields(mfaFieldsModel);

                    if (mfaFieldUpdate.ModifiedCount <= 0)
                    {
                        _logger.LogError("Could not update mfa fields");
                        return BadRequest(new { Message = "Could set up MFA" });
                    }

                    var user = await _userRepo.GetUserByEmail(loginResponse.Email);

                    var response = new
                    {
                        ManualCode = user.ManualCode,
                        QrCode = user.QRCode
                    };

                    return Ok(response);
                }

                    
                return BadRequest(new { Message = "You cannot setup MFA if you have not enabled it" });

                    
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in the controller while trying to set up mfa: {ex.Message}");
                return StatusCode(500, new { Message = "Encountered an error" });
            }
        }
        #endregion

        #region [ 2FA Login ]
        [HttpPost("mfa-login")]
        public async Task<ActionResult> MFALogin(TwoFALoginModel request)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserId = User?.FindFirstValue(ClaimTypes.Sid)?.ToString();
                var tokenUserEmail = User?.FindFirstValue(ClaimTypes.Email)?.ToString();

                if (request.Email.ToLower() != tokenUserEmail?.ToLower() || tokenType != "mfa-token")
                {
                    return Unauthorized(new { Message = "Unauthorized User/Invalid token" });
                }

                var user = await _userRepo.GetUserByEmail(request.Email);

                if (user == null)
                {
                    return NotFound(new { Message = "User not found" });
                }

                if (user.isMFAEnabled == true)
                {
                    byte[] encryptedBytes = Convert.FromBase64String(user.MFAKey);
                    string decryptedSecret = await _mfaService.DecryptString(encryptedBytes);

                    TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
                    var isValid = tfa.ValidateTwoFactorPIN(decryptedSecret, request.Code);

                    if (!isValid)
                    {
                        return Unauthorized(new { Message = "Bad Credentials" });
                    }

                    if (user.isFirstSignIn == true && user.isMFAVerified == false)
                    {
                        var isUpdated = await _userRepo.updateFirstSignIn(user.Email);

                        if (isUpdated.ModifiedCount <= 0)
                        {
                            return BadRequest(new { Messaage = "Could not update mfa fields" });
                        }
                    }

                    var (token, refreshToken) = await _tokensHelper.GenerateTokens(user);

                    if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken))
                    {
                        return BadRequest(new { Messaage = "Could not generate tokens" });
                    }

                    return Ok(new { AccessToken = token, RefreshToken = refreshToken });
                }

                return BadRequest(new { Message = "You cannot setup MFA if you have not enabled it" });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the controller while trying to log in with MFA");
                return StatusCode(500, new { Message = "Encountered an error" });
            }
        }
        #endregion

        #region [ Refresh Token ]
        [AllowAnonymous]
        [HttpPost("refreshtoken")]
        public async Task<ActionResult> RefreshToken(RefreshTokenRequest payload)
        {
            try
            {
                var refreshToken = await _refreshTokenRepo.GetRefreshTokenByRefreshToken(payload.RefreshToken);

                var user = await _userRepo.GetUserByEmail(payload.Email);

                if (user == null)
                {
                    return NotFound(new { Message = "User not found" });
                }

                if (refreshToken == null || refreshToken.ExpiresOn < DateTime.Now || refreshToken.UserId != user.Id)
                {
                    return Unauthorized(new { Message = "Invalid refresh token, please log in" });
                }

                var accessToken = _tokenService.GenerateAccessToken(user);


                if (string.IsNullOrEmpty(accessToken))
                {
                    return BadRequest("Could not generate access token");
                }

                return Ok(new { AccessToken = accessToken });


            }
            catch (Exception ex)
            {
                _logger.LogError("Error while trying to get a new access token: {ex}");
                return StatusCode(500, new { Message = "Encountered an error" });
            }
        }
        #endregion

        #region [ Forgot Password ]

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPassword payload)
        {
            try
            {
                var email = payload.Email;

                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(new { Message = "Enter email" });
                }

                var user = await _userRepo.GetUserByEmail(email);

                if (user == null)
                {
                    return NotFound();
                }

                var token = _tokenService.GenerateChangePasswordToken(user);

                string baseUrl = Environment.GetEnvironmentVariable("FrontEndBaseUrl")!;
                var url = $"{baseUrl}/auth/change-password?token={token}&email={user.Email}";
                var recipients = new List<string>() { user.Email };
                var path = $"./Templates/forgot-password.html";
                var template = System.IO.File.ReadAllText(path).Replace("\n", "");

                template = template.Replace("{{NAME}}", user.Firstname)
                           .Replace("{{LOGIN_LINK}}", url);

                var subject = "Forgot Password Request {{School Name}} Portal";


                var emailResponse = await _emailService.SendTemplateEmail(recipients, "Forgot password", template);

                if (emailResponse.ToLower() != "sent")
                {
                    _logger.LogError($"Could not send email");
                    return BadRequest(new { Message = "Could not send email" });
                }

                await _userAudit.AddAudit(user.Id!, user.Email, "forgot-password", $"Requested to change password");

                return Accepted(new { Message = "Email sent" });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the forgot password endpoint");
                return StatusCode(500, new { Message = "Encountered an error" });
            }
        }
        #endregion

        #region [ Change Password ]
        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword(LoginModel request)
        {
            try
            {
                var tokenType = User.Claims.FirstOrDefault(c => c.Type == "TokenType")?.Value;
                var tokenUserId = User?.FindFirstValue(ClaimTypes.Sid)?.ToString();
                var user = await _userRepo.GetUserByEmail(request.Email);

                if (!_passwordHelper.IsPasswordValid(request.Password))
                {
                    return BadRequest(new
                    {
                        Message = "Your password must be 8 characters long or more, " +
                        "contain at least 1 number and special character"
                    });
                }

                if (user == null)
                {
                    return NotFound(new { Message = "User not found" });
                }

                if (tokenType != "change-password-token")
                {
                    return Unauthorized(new { Message = "Bad Token" });
                }

                if (tokenUserId != user.Id)
                {
                    return Unauthorized(new { Message = "Unauthorized User" });
                }

                var isUpdated = await _userRepo.updatePassword(request);

                if (isUpdated.ModifiedCount <= 0)
                {
                    return BadRequest(new { Message = "Could not change password" });
                }

                var token = _tokenService.GenerateChangePasswordToken(user);

                string baseUrl = Environment.GetEnvironmentVariable("FrontEndBaseUrl")!;
                var url = $"{baseUrl}/auth/change-password?token={token}&email={user.Email}";
                var recipients = new List<string>() { user.Email };
                var path = $"./Templates/changed-password.html";
                var template = System.IO.File.ReadAllText(path).Replace("\n", "");

                template = template.Replace("{{NAME}}", user.Firstname)
                           .Replace("{{LOGIN_LINK}}", url);

                var emailResponse = await _emailService.SendTemplateEmail(recipients, "Password changed successfully", template);

                if (emailResponse.ToLower() != "sent")
                {
                    _logger.LogError($"Could not send email");
                    return BadRequest(new { Message = "Could not send email" });
                }

                await _userAudit.AddAudit(tokenUserId, user.Email, "update", $"Updated their password");

                return Accepted(new { Messsage = "Password has been changed" });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the change password end point");
                return StatusCode(500, new { Message = "Encountered an error" });
            }
        }
        #endregion
    }
}
