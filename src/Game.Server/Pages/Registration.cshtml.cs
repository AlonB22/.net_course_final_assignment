using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Game.Contracts.Models;
using Game.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Game.Server.Pages;

public class RegistrationModel : PageModel
{
    private const int MaxParticipants = 10;
    private const int DefaultMoveTimeLimitSeconds = 10;
    private readonly ICountryCatalogService _countryCatalogService;
    private readonly IRegistrationService _registrationService;
    private readonly ILogger<RegistrationModel> _logger;

    public RegistrationModel(
        ICountryCatalogService countryCatalogService,
        IRegistrationService registrationService,
        ILogger<RegistrationModel> logger)
    {
        _countryCatalogService = countryCatalogService;
        _registrationService = registrationService;
        _logger = logger;
    }

    [BindProperty]
    public RegistrationInputModel Form { get; set; } = RegistrationInputModel.CreateDefault();

    [TempData]
    public string? RegistrationResponseJson { get; set; }

    public IReadOnlyList<SelectListItem> ParticipantCountOptions { get; private set; } = CreateParticipantCountOptions();

    public IReadOnlyList<SelectListItem> CountryOptions { get; private set; } = [];

    public async Task OnGetAsync()
    {
        EnsureParticipantRows();
        await LoadLookupsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        EnsureParticipantRows();
        ClearInactiveParticipantValidation();
        await LoadLookupsAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var request = Form.ToRequest(DefaultMoveTimeLimitSeconds);

        try
        {
            var response = await _registrationService.RegisterSessionAsync(request);
            RegistrationResponseJson = JsonSerializer.Serialize(response);
            return RedirectToPage("/StartGame", new { sessionId = response.SessionId });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration failed for the submitted participants.");
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }

    private async Task LoadLookupsAsync()
    {
        ParticipantCountOptions = CreateParticipantCountOptions();
        CountryOptions = (await _countryCatalogService.GetCountriesAsync())
            .Select(country => new SelectListItem($"{country.Name}", country.CountryId.ToString()))
            .ToArray();
    }

    private void EnsureParticipantRows()
    {
        Form.Participants ??= RegistrationInputModel.CreateDefault().Participants;

        if (Form.Participants.Count > MaxParticipants)
        {
            Form.Participants = Form.Participants.Take(MaxParticipants).ToList();
        }

        if (Form.Participants.Count < MaxParticipants)
        {
            Form.Participants.AddRange(
                Enumerable.Range(0, MaxParticipants - Form.Participants.Count)
                    .Select(_ => new ParticipantInputModel()));
        }
    }

    private void ClearInactiveParticipantValidation()
    {
        var inactiveIndexes = Enumerable.Range(Form.ParticipantCount, Math.Max(0, MaxParticipants - Form.ParticipantCount));

        foreach (var key in inactiveIndexes.SelectMany(index => ModelState.Keys
                     .Where(existingKey => existingKey.StartsWith($"Form.Participants[{index}]", StringComparison.Ordinal))
                     .ToArray()))
        {
            ModelState.Remove(key);
        }
    }

    private static IReadOnlyList<SelectListItem> CreateParticipantCountOptions()
        => Enumerable.Range(1, MaxParticipants)
            .Select(count => new SelectListItem(count.ToString(), count.ToString()))
            .ToArray();

    public sealed class RegistrationInputModel : IValidatableObject
    {
        public static RegistrationInputModel CreateDefault()
            => new()
            {
                ParticipantCount = 2,
                Participants = Enumerable.Range(0, MaxParticipants)
                    .Select(_ => new ParticipantInputModel())
                    .ToList()
            };

        [Display(Name = "Participant count")]
        [Range(1, MaxParticipants, ErrorMessage = "Choose between 1 and 10 participants.")]
        public int ParticipantCount { get; set; }

        [Required]
        public List<ParticipantInputModel> Participants { get; set; } = [];

        public SessionRegistrationRequestDto ToRequest(int moveTimeLimitSeconds)
        {
            var activeParticipants = Participants.Take(ParticipantCount)
                .Select(participant => new ParticipantRegistrationDto(
                    participant.FirstName.Trim(),
                    participant.ExternalId,
                    participant.PhoneNumber.Trim(),
                    participant.CountryId))
                .ToArray();

            return new SessionRegistrationRequestDto(moveTimeLimitSeconds, activeParticipants);
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Participants.Count < ParticipantCount)
            {
                yield return new ValidationResult(
                    "Fill in all participant rows before creating the session.",
                    [nameof(Participants)]);
            }

            var duplicateIds = Participants.Take(ParticipantCount)
                .Where(participant => participant.ExternalId > 0)
                .GroupBy(participant => participant.ExternalId)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();

            if (duplicateIds.Length > 0)
            {
                yield return new ValidationResult(
                    "Each participant must use a unique numeric ID within the same game.",
                    [nameof(Participants)]);
            }
        }
    }

    public sealed class ParticipantInputModel
    {
        [Display(Name = "First name")]
        [Required(ErrorMessage = "Enter a first name.")]
        [MinLength(2, ErrorMessage = "First name must contain at least 2 letters.")]
        [RegularExpression(@"^[A-Za-z][A-Za-z' -]*$", ErrorMessage = "Use letters only for the first name.")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Numeric ID")]
        [Range(1, 1000, ErrorMessage = "Choose an ID between 1 and 1000.")]
        public int ExternalId { get; set; }

        [Display(Name = "Phone number")]
        [Required(ErrorMessage = "Enter a 10-digit phone number.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must contain exactly 10 digits.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Country")]
        [Range(1, int.MaxValue, ErrorMessage = "Choose a country.")]
        public int CountryId { get; set; }
    }
}
