using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Repositories;
using DayCare_ManagementSystem_API.Service;

namespace DayCare_ManagementSystem_API.Helper
{
    public class TokensHelper
    {
        #region [ Constructor ]
        private readonly IToken _tokenService;
        private readonly IRefreshToken _refreshTokenRepo;
        private readonly ILogger<TokensHelper> _logger;

        public TokensHelper(IToken tokenService, IRefreshToken refreshTokenRepo, ILogger<TokensHelper> logger)
        {
            _refreshTokenRepo = refreshTokenRepo;
            _tokenService = tokenService;
            _logger = logger;
        }
        #endregion

        #region [ Generate Tokens ]
        public async Task<(string, string)> GenerateTokens(User user)
        {
            try
            {
                var token = _tokenService.GenerateAccessToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken();

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken))
                {
                    return (null, null);
                }

                var existingRefreshToken = await _refreshTokenRepo.GetRefreshTokenByUserId(user.Id);

                if (existingRefreshToken != null)
                {
                    await _refreshTokenRepo.DeleteRefreshToken(existingRefreshToken.Id);
                }

                var refreshTokenModel = new RefreshToken()
                {
                    Token = refreshToken,
                    ExpiresOn = DateTime.Now.AddDays(7),
                    UserId = user.Id
                };

                var isAdded = await _refreshTokenRepo.AddRefreshToken(refreshTokenModel);

                if (isAdded == null)
                {
                    return (null, null);
                }

                return (token, refreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Token Helper while trying to generate tokens");
                throw;
            }
        } 
        #endregion
    }
}
