using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using BikeCarHub.Data;
using BikeCarHub.Domain;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

namespace BikeCarHub.Services;

public enum AccountResult { Ok, WritesDisabled, EmailInUse, InvalidToken }

public interface IAuthService
{
    /// <summary>Checks the password against the existing Identity hash. Reads only.</summary>
    Task<AppUser?> ValidateAsync(string email, string password);
    ClaimsPrincipal CreatePrincipal(AppUser user);
    Task<AccountResult> RegisterAsync(string fullName, string email, string password);
    /// <summary>Null when no account matches. Creating a token writes nothing.</summary>
    Task<string?> CreateResetTokenAsync(string email);
    Task<AccountResult> ResetPasswordAsync(string token, string newPassword);
}

public sealed class AuthService(IUserRepository users, IWriteGate gate, IDataProtectionProvider protection) : IAuthService
{
    private readonly PasswordHasher<AppUser> _hasher = new();
    private readonly ITimeLimitedDataProtector _tokens = protection.CreateProtector("BikeCarHub.PasswordReset").ToTimeLimitedDataProtector();

    public async Task<AppUser?> ValidateAsync(string email, string password)
    {
        var user = await users.FindByEmailAsync(email);
        if (user?.PasswordHash is null) return null;
        // A "rehash needed" result is accepted as-is so that signing in never writes to the database.
        return _hasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed ? null : user;
    }

    public ClaimsPrincipal CreatePrincipal(AppUser user)
    {
        var display = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? user.UserName ?? "Account" : user.FullName;
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, display),
            new Claim(ClaimTypes.Email, user.Email ?? "")
        ], CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    public async Task<AccountResult> RegisterAsync(string fullName, string email, string password)
    {
        if (!gate.Allowed) return AccountResult.WritesDisabled;
        if (await users.FindByEmailAsync(email) is not null) return AccountResult.EmailInUse;
        var hash = _hasher.HashPassword(new AppUser(), password);
        await users.CreateAsync(fullName.Trim(), email.Trim(), hash);
        return AccountResult.Ok;
    }

    public async Task<string?> CreateResetTokenAsync(string email)
    {
        var user = await users.FindByEmailAsync(email);
        return user is null ? null : _tokens.Protect($"{user.Id}|{user.SecurityStamp}", TimeSpan.FromHours(1));
    }

    public async Task<AccountResult> ResetPasswordAsync(string token, string newPassword)
    {
        string payload;
        try
        {
            payload = _tokens.Unprotect(token);
        }
        catch (CryptographicException)
        {
            return AccountResult.InvalidToken;
        }

        var parts = payload.Split('|', 2);
        if (parts.Length != 2 || !int.TryParse(parts[0], out var id)) return AccountResult.InvalidToken;
        var user = await users.FindByIdAsync(id);
        // The stamp changes whenever the password does, so a used link cannot be replayed.
        if (user is null || (user.SecurityStamp ?? "") != parts[1]) return AccountResult.InvalidToken;
        if (!gate.Allowed) return AccountResult.WritesDisabled;

        await users.UpdatePasswordAsync(id, _hasher.HashPassword(user, newPassword));
        return AccountResult.Ok;
    }
}

public interface IEmailSender
{
    bool IsConfigured { get; }
    Task SendAsync(string to, string subject, string body);
}

/// <summary>Integration point for outgoing mail. Does nothing until the Smtp section is filled in.</summary>
public sealed class SmtpEmailSender(IConfiguration config) : IEmailSender
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(config["Smtp:Host"]) && !string.IsNullOrWhiteSpace(config["Smtp:From"]);

    public async Task SendAsync(string to, string subject, string body)
    {
        if (!IsConfigured) throw new InvalidOperationException("SMTP is not configured.");
        using var client = new SmtpClient(config["Smtp:Host"], config.GetValue("Smtp:Port", 587)) { EnableSsl = true };
        var user = config["Smtp:User"];
        if (!string.IsNullOrWhiteSpace(user)) client.Credentials = new NetworkCredential(user, config["Smtp:Password"]);
        using var message = new MailMessage(config["Smtp:From"]!, to, subject, body);
        await client.SendMailAsync(message);
    }
}
