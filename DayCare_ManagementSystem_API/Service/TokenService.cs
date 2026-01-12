using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Repositories;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DayCare_ManagementSystem_API.Service
{

    #region [ Interface ]
    public interface IToken
    {
        public string? GenerateAccessToken(User payload);
        public string? GenerateChangePasswordToken(User? user);
        public string? GenerateMfaToken(User? user);
        public string GenerateRefreshToken();
    }
    #endregion
    public class TokenService : IToken
    {
        #region [ Constructor ]
        private readonly IUser _userRepo;
        private readonly ILogger<TokenService> _logger;
        private readonly IConfiguration _config;

        public TokenService(IUser userRepo, ILogger<TokenService> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
            _userRepo = userRepo;
        }
        #endregion


        #region [ Generate Change Password Token ]
        public string? GenerateChangePasswordToken(User? user)
        {
            try
            {
                if (user == null)
                {
                    return null;
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("Jwt_Key"));

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Sid, user.Id),
                    new Claim("TokenType", "change-password-token")
                };

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.Now.AddHours(1),
                    Issuer = Environment.GetEnvironmentVariable("Jwt_Issuer"),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the token service while trying to generate change password token");
                return null;
            }
        }
        #endregion

        #region [ Generate Access Token ]

        public string? GenerateAccessToken(User user)
        {
            try
            {
                //Create JWT token handler and get secret key
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("Jwt_Key"));

                //Prepare list of user claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Sid, user.Id),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("TokenType", "access-token")
                };

                // Create a token Descriptor
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    IssuedAt = DateTime.Now.AddHours(2),
                    Issuer = Environment.GetEnvironmentVariable("Jwt_Issuer"),
                    Expires = DateTime.Now.AddHours(1),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                return tokenString;

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in the token service while trying to generate a token: {ex.InnerException?.Message}");
                return null;
            }
        }
        #endregion

        #region [ MFA Token ]
        public string? GenerateMfaToken(User? user)
        {
            try
            {
                if (user == null)
                {
                    return null;
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("Jwt_Key"));

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Sid, user.Id),
                    new Claim("TokenType", "mfa-token")
                };

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = user.isFirstSignIn == true ? DateTime.Now.AddMinutes(15) : DateTime.Now.AddMinutes(5),
                    Issuer = Environment.GetEnvironmentVariable("Jwt_Issuer"),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the token service while trying to generate change password token");
                return null;
            }
        }
        #endregion

        #region [ Refresh Token ]
        public string GenerateRefreshToken()
        {
            try
            {
                return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while trying to generate a refresh token");
                return null;
            }
        }
        #endregion
    }
}
