using System.ComponentModel.DataAnnotations;
using BikeCarHub.Data;
using BikeCarHub.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BikeCarHub.Pages;

[EnableRateLimiting("forms")]
public class NewsletterModel(IFormRepository forms, IWriteGate gate) : BasePageModel
{
    [BindProperty, Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = "";

    [BindProperty] public string? Website { get; set; }
    public string? Outcome { get; private set; }
    public bool IsDemo { get; private set; }

    // The footer form arrives here by GET, so cached pages never need an anti-forgery token.
    public void OnGet(string? email)
    {
        Email = email?.Trim() ?? "";
        SetMeta();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SetMeta();
        if (!string.IsNullOrEmpty(Website))
        {
            Outcome = "Thank you for subscribing.";
            return Page();
        }
        if (!ModelState.IsValid) return Page();

        if (!gate.Allowed)
        {
            // Integration point: set Database:AllowWrites to true to store addresses in NewsLetters.
            IsDemo = true;
            Outcome = "Demo mode: your address was not saved, because saving to the database is switched off on this site.";
            return Page();
        }

        await forms.SaveNewsletterAsync(Email.Trim());
        Outcome = "Thank you for subscribing.";
        return Page();
    }

    private void SetMeta()
    {
        Meta.Title = "Newsletter | BikeCarHub";
        Meta.Description = "Subscribe to the BikeCarHub newsletter.";
        Meta.CanonicalPath = "/newsletter";
        Meta.NoIndex = true;
    }
}
