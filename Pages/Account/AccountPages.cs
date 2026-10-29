using System.ComponentModel.DataAnnotations;
using BikeCarHub.Seo;
using BikeCarHub.Services;
using BikeCarHub.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BikeCarHub.Pages.Account;

public abstract class AccountPageModel : BasePageModel
{
    public const string WritesOffMessage =
        "Demo mode: nothing was saved, because saving to the database is switched off on this site.";

    protected void SetMeta(string title, string path)
    {
        Meta.Title = $"{title} | BikeCarHub";
        Meta.Description = $"{title} – BikeCarHub account.";
        Meta.CanonicalPath = path;
        Meta.NoIndex = true;
    }

    /// <summary>Only same-site paths are followed after sign-in.</summary>
    protected string SafeReturnUrl(string? returnUrl) => Url.IsLocalUrl(returnUrl) ? returnUrl! : "/";
}

[EnableRateLimiting("forms")]
public class SignInModel(IAuthService auth) : AccountPageModel
{
    [BindProperty, Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
    [BindProperty, Required, DataType(DataType.Password), StringLength(100)] public string Password { get; set; } = "";
    [BindProperty] public bool RememberMe { get; set; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    public string? Notice { get; private set; }

    public void OnGet(string? notice)
    {
        SetMeta("Sign in", "/account/sign-in");
        Notice = notice switch
        {
            "registered" => "Your account has been created. Please sign in.",
            "reset" => "Your password has been changed. Please sign in.",
            _ => null
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SetMeta("Sign in", "/account/sign-in");
        if (!ModelState.IsValid) return Page();

        var user = await auth.ValidateAsync(Email, Password);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "The email or password is not correct.");
            return Page();
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, auth.CreatePrincipal(user),
            new AuthenticationProperties { IsPersistent = RememberMe });
        return LocalRedirect(SafeReturnUrl(ReturnUrl));
    }
}

[EnableRateLimiting("forms")]
public class SignUpModel(IAuthService auth) : AccountPageModel
{
    [BindProperty, Required, StringLength(100, MinimumLength = 2)] public string FullName { get; set; } = "";
    [BindProperty, Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";

    [BindProperty, Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8, ErrorMessage = "Use at least 8 characters.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Use at least one letter and one number.")]
    public string Password { get; set; } = "";

    [BindProperty, Required, DataType(DataType.Password), System.ComponentModel.DataAnnotations.Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";

    [BindProperty] public string? Website { get; set; }
    public string? DemoNotice { get; private set; }

    public void OnGet() => SetMeta("Create an account", "/account/sign-up");

    public async Task<IActionResult> OnPostAsync()
    {
        SetMeta("Create an account", "/account/sign-up");
        if (!string.IsNullOrEmpty(Website)) return Redirect("/account/sign-in?notice=registered");
        if (!ModelState.IsValid) return Page();

        switch (await auth.RegisterAsync(FullName, Email, Password))
        {
            case AccountResult.Ok:
                return Redirect("/account/sign-in?notice=registered");
            case AccountResult.EmailInUse:
                ModelState.AddModelError(nameof(Email), "An account with this email already exists.");
                return Page();
            default:
                DemoNotice = WritesOffMessage;
                return Page();
        }
    }
}

[EnableRateLimiting("forms")]
public class ForgotPasswordModel(IAuthService auth, IEmailSender email, SiteSettings site, ILogger<ForgotPasswordModel> logger) : AccountPageModel
{
    [BindProperty, Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
    public string? Notice { get; private set; }
    public bool IsDemo { get; private set; }

    public void OnGet() => SetMeta("Forgot password", "/account/forgot-password");

    public async Task<IActionResult> OnPostAsync()
    {
        SetMeta("Forgot password", "/account/forgot-password");
        if (!ModelState.IsValid) return Page();

        if (!email.IsConfigured)
        {
            // Integration point: fill in the Smtp section of the configuration to send reset links.
            IsDemo = true;
            Notice = "Demo mode: no email was sent, because outgoing email is not set up on this site.";
            return Page();
        }

        var token = await auth.CreateResetTokenAsync(Email);
        if (token is not null)
        {
            var link = $"{site.BaseUrl}/account/reset-password?token={Uri.EscapeDataString(token)}";
            try
            {
                await email.SendAsync(Email.Trim(), "Reset your BikeCarHub password",
                    $"Use this link within one hour to choose a new password:\n\n{link}\n\nIf you did not ask for this, you can ignore this email.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Password reset email could not be sent");
            }
        }
        // The same answer whether or not the address has an account.
        Notice = "If an account exists for that address, a reset link has been sent to it.";
        return Page();
    }
}

[EnableRateLimiting("forms")]
public class ResetPasswordModel(IAuthService auth) : AccountPageModel
{
    [BindProperty(SupportsGet = true), Required] public string Token { get; set; } = "";

    [BindProperty, Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8, ErrorMessage = "Use at least 8 characters.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Use at least one letter and one number.")]
    public string Password { get; set; } = "";

    [BindProperty, Required, DataType(DataType.Password), System.ComponentModel.DataAnnotations.Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";

    public string? DemoNotice { get; private set; }

    public void OnGet()
    {
        SetMeta("Choose a new password", "/account/reset-password");
        ModelState.Clear();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SetMeta("Choose a new password", "/account/reset-password");
        if (!ModelState.IsValid) return Page();

        switch (await auth.ResetPasswordAsync(Token, Password))
        {
            case AccountResult.Ok:
                return Redirect("/account/sign-in?notice=reset");
            case AccountResult.InvalidToken:
                ModelState.AddModelError(string.Empty, "This reset link is not valid or has expired. Please request a new one.");
                return Page();
            default:
                DemoNotice = WritesOffMessage;
                return Page();
        }
    }
}

public class SignOutModel : AccountPageModel
{
    public IActionResult OnGet() => Redirect("/");

    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }
}
