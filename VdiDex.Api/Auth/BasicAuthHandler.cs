using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using VdiDex.Api.Models;

namespace VdiDex.Api.Auth;

public sealed class BasicAuthSchemeOptions : AuthenticationSchemeOptions
{
    public const string Scheme = "Basic";
}

public sealed class BasicAuthHandler : AuthenticationHandler<BasicAuthSchemeOptions>
{
    private readonly BasicAuthCredentials _credentials;

    public BasicAuthHandler(
        IOptionsMonitor<BasicAuthSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<BasicAuthCredentials> credentials)
        : base(options, logger, encoder)
    {
        _credentials = credentials.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var header))
            return Task.FromResult(AuthenticateResult.NoResult());

        var raw = header.ToString();
        if (!raw.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.Fail("Invalid auth scheme."));

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(raw[6..].Trim()));
        }
        catch (FormatException)
        {
            return Task.FromResult(AuthenticateResult.Fail("Malformed Authorization header."));
        }

        var separator = decoded.IndexOf(':');
        if (separator <= 0)
            return Task.FromResult(AuthenticateResult.Fail("Malformed credentials."));

        var user = decoded[..separator];
        var pass = decoded[(separator + 1)..];

        if (!FixedTimeEquals(user, _credentials.Username) || !FixedTimeEquals(pass, _credentials.Password))
            return Task.FromResult(AuthenticateResult.Fail("Invalid credentials."));

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, user) }, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers["WWW-Authenticate"] = "Basic realm=\"VdiDex\", charset=\"UTF-8\"";
        return base.HandleChallengeAsync(properties);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return ba.Length == bb.Length && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}
