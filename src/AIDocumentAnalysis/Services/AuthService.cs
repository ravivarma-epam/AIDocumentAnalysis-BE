using AIDocumentAnalysis.Configurations;
using FastEndpoints.Security;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace AIDocumentAnalysis.Services;

public class AuthService(IConfiguration config, ILogger<AuthService> logger, IOptions<JWTAuthConfiguration> jwtAuthConfig)
{
    public async Task<string?> LoginWithGoogleAsync(string googleIdToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { config["GoogleConfiguration:ClientId"] }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(googleIdToken, settings);


            var userRole = payload.Email.Equals("ravitejabollineni555@gmail.com") ? "Admin" : "User";

            var jwtToken = JwtBearer.CreateToken(options =>
            {
                options.SigningKey = jwtAuthConfig.Value.SecretKey;
                options.Issuer = jwtAuthConfig.Value.Issuer;
                options.Audience = jwtAuthConfig.Value.Audience;
                options.ExpireAt = DateTime.UtcNow.AddMinutes(jwtAuthConfig.Value.TokenExpiryInMinutes);

                // Add standard claims
                options.User.Claims.Add(("Email", payload.Email));
                options.User.Claims.Add(("Name", payload.Name));

                // Add Roles & Permissions
                options.User.Roles.Add(userRole);
            });

            return jwtToken;
        }
        catch (InvalidJwtException ex)
        {
            logger.LogWarning(ex, "Invalid Google Token attempt.");
            return null;
        }
    }
}
