using System.ComponentModel.DataAnnotations;
using BikeCarHub.Data;
using BikeCarHub.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BikeCarHub.Pages;

[EnableRateLimiting("forms")]
public class AskModel(IFormRepository forms, IWriteGate gate, ILogger<AskModel> logger) : BasePageModel
{
    public sealed class InputModel
    {
        [Required, StringLength(60)] public string Name { get; set; } = "";
        [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = "";
        [Required, StringLength(2000, MinimumLength = 10)] public string Question { get; set; } = "";
        /// <summary>Honeypot: people never see or fill this field.</summary>
        public string? Website { get; set; }
    }

    [BindProperty] public InputModel Input { get; set; } = new();
    public string? Outcome { get; private set; }
    public bool IsDemo { get; private set; }

    public void OnGet() => SetMeta();

    public async Task<IActionResult> OnPostAsync()
    {
        SetMeta();
        if (!string.IsNullOrEmpty(Input.Website))
        {
            Outcome = "Thank you. Your question has been received.";
            return Page();
        }
        if (!ModelState.IsValid) return Page();

        if (!gate.Allowed)
        {
            // Integration point: set Database:AllowWrites to true to store questions in TWOrFWContactUs.
            IsDemo = true;
            Outcome = "Demo mode: your question was not saved or sent, because saving to the database is switched off on this site.";
            return Page();
        }

        await forms.SaveQuestionAsync(Input.Name.Trim(), Input.Email.Trim(), Input.Question.Trim());
        logger.LogInformation("Question stored from the ask-a-question form");
        Outcome = "Thank you. Your question has been received.";
        ModelState.Clear();
        Input = new InputModel();
        return Page();
    }

    private void SetMeta()
    {
        Meta.Title = "Ask a Question | BikeCarHub";
        Meta.Description = "Have a question about a bike, scooter or car? Send it to the BikeCarHub team.";
        Meta.CanonicalPath = "/ask-a-question";
        Meta.Crumbs(("Ask a question", "/ask-a-question"));
    }
}
