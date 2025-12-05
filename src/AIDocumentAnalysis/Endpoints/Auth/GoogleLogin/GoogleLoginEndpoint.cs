using System.Text.Json.Serialization;

using AIDocumentAnalysis.Services;

namespace AIDocumentAnalysis.Endpoints.Auth.GoogleLogin;

public record GoogleLoginRequest
{
    [JsonPropertyName("id_token")]
    public  required string IdToken { get; init; }
}

public record GoogleLoginResponse
{
    public string? AccessToken { get; init; }
    public required string Message { get; init; }
}

public class GoogleLoginValidator : Validator<GoogleLoginRequest>
{
    public GoogleLoginValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("Google ID Token is missing!");
    }
}

public class GoogleLoginEndpoint(AuthService authService): Endpoint<GoogleLoginRequest, GoogleLoginResponse>
{
    public override void Configure()
    {
        Post("/auth/google-login");
        AllowAnonymous(); 
        Description(b => b
            .Produces<GoogleLoginResponse>(200, "application/json")
            .ProducesProblem(400)
            .ProducesProblem(401));
    }

    public override async Task HandleAsync(GoogleLoginRequest req, CancellationToken ct)
    {
        var token = await authService.LoginWithGoogleAsync(req.IdToken);

        if (token is null)
        {
            ThrowError("Authentication Failed: Invalid Google Token", 401);
        }

        await SendOkAsync(new GoogleLoginResponse
        {
            AccessToken = token,
            Message = "Login Successful"
        }, ct);
    }
}
