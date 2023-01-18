using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Ardalis.GuardClauses;
using Microsoft.IdentityModel.Tokens;
using SignalTrader.Authentication.Resources;

namespace SignalTrader.Authentication.Services;

public class AuthenticationService : IAuthenticationService
{
    #region Members

    private readonly ILogger<AuthenticationService> _logger;
    private readonly IConfiguration _configuration;

    #endregion

    #region Constructors

    public AuthenticationService(ILogger<AuthenticationService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    #endregion

    #region IAuthenticationService

    public string? Authenticate(AuthenticateResource resource)
    {
        Guard.Against.Null(resource, nameof(resource));
        Guard.Against.NullOrEmpty(resource.PasswordBase64, "resource.PasswordBase64");

        var expectedPassword = _configuration["User:Password"];
        string receivedPassword;
        try
        {
            receivedPassword = Encoding.UTF8.GetString(Convert.FromBase64String(resource.PasswordBase64));
        }
        catch (FormatException)
        {
            throw new ArgumentException("PasswordBase64 must be base-64 encoded");
        }

        if (!string.IsNullOrWhiteSpace(expectedPassword) &&
            !string.IsNullOrWhiteSpace(receivedPassword) &&
            receivedPassword.Equals(expectedPassword))
        {
               // Create JWT.
                var key = _configuration["Authentication:JwtSigningKey"];
                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Audience = _configuration["Authentication:JwtAudience"],
                    Issuer = _configuration["Authentication:JwtIssuer"],
                    IssuedAt = DateTime.UtcNow,
                    Expires = DateTime.UtcNow.AddMinutes(Constants.JwtDurationMinutes),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha512Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);
        }

        // Unrecognised password.
        return null;
    }

    #endregion
}
