using System.ComponentModel.DataAnnotations;
using Game.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Game.Server.Pages.Admin;

public class IndexModel(
    IQueryRetrievalService queryRetrievalService,
    IGamePersistenceService gamePersistenceService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid? SelectedPlayerId { get; set; }

    [BindProperty]
    public PlayerEditInputModel EditForm { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public IReadOnlyList<SelectListItem> CountryOptions { get; private set; } = [];
    public IReadOnlyList<SelectListItem> PlayerOptions { get; private set; } = [];
    public IReadOnlyList<PlayerListRow> Players { get; private set; } = [];
    public IReadOnlyList<GameListRow> Games { get; private set; } = [];
    public PlayerDetailsRow? SelectedPlayer { get; private set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostUpdatePlayerAsync()
    {
        await LoadAsync(EditForm.PlayerId, hydrateEditForm: false);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (SelectedPlayer is null)
        {
            ModelState.AddModelError(string.Empty, "Choose an existing player before updating.");
            return Page();
        }

        try
        {
            await gamePersistenceService.UpdatePlayerAsync(
                EditForm.PlayerId,
                EditForm.FirstName,
                EditForm.PhoneNumber,
                EditForm.CountryId);

            StatusMessage = "Player details were updated.";
            return RedirectToPage(new { selectedPlayerId = EditForm.PlayerId });
        }
        catch (KeyNotFoundException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeletePlayerAsync(Guid playerId)
    {
        try
        {
            await gamePersistenceService.DeletePlayerAsync(playerId);
            StatusMessage = "Player and their games were deleted.";
            return RedirectToPage();
        }
        catch (KeyNotFoundException ex)
        {
            await LoadAsync(SelectedPlayerId);
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteSessionAsync(Guid sessionId)
    {
        try
        {
            await gamePersistenceService.DeleteSessionAsync(sessionId);
            StatusMessage = "Game session was deleted.";
            return RedirectToPage();
        }
        catch (KeyNotFoundException ex)
        {
            await LoadAsync(SelectedPlayerId);
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }

    private async Task LoadAsync(Guid? selectedPlayerId = null, bool hydrateEditForm = true)
    {
        var players = await queryRetrievalService.GetPlayersAsync(includeGameParticipations: true);
        var countries = await queryRetrievalService.GetCountriesAsync();
        var sessions = await queryRetrievalService.GetGameSessionsAsync(includeParticipants: true);

        CountryOptions = countries
            .OrderBy(country => country.Name, StringComparer.Ordinal)
            .Select(country => new SelectListItem(country.Name, country.CountryId.ToString()))
            .ToArray();

        PlayerOptions = players
            .OrderBy(player => player.FirstName, StringComparer.Ordinal)
            .ThenBy(player => player.ExternalId)
            .Select(player => new SelectListItem(
                $"{player.FirstName} ({player.ExternalId})",
                player.PlayerId.ToString()))
            .ToArray();

        Players = players
            .OrderBy(player => player.FirstName, StringComparer.Ordinal)
            .ThenBy(player => player.ExternalId)
            .Select(player => new PlayerListRow(
                player.PlayerId,
                player.FirstName,
                player.ExternalId,
                player.PhoneNumber,
                player.CountryId,
                player.Country?.Name ?? string.Empty,
                player.GameParticipations.Count))
            .ToArray();

        Games = sessions
            .OrderByDescending(session => session.StartedAtUtc)
            .ThenBy(session => session.GameSessionId)
            .Select(session => new GameListRow(
                session.GameSessionId,
                session.StartedAtUtc,
                session.Status.ToString(),
                session.Outcome.ToString(),
                session.ParticipantCount,
                session.Participants
                    .OrderBy(participant => participant.TurnOrder)
                    .Select(participant => participant.Player?.FirstName ?? string.Empty)
                    .ToArray()))
            .ToArray();

        var effectivePlayerId = selectedPlayerId ?? SelectedPlayerId ?? Players.FirstOrDefault()?.PlayerId;
        SelectedPlayerId = effectivePlayerId;

        SelectedPlayer = effectivePlayerId is null
            ? null
            : Players
                .Where(player => player.PlayerId == effectivePlayerId)
                .Select(player => new PlayerDetailsRow(
                    player.PlayerId,
                    player.FirstName,
                    player.ExternalId,
                    player.PhoneNumber,
                    player.CountryId,
                    player.CountryName,
                    player.GameCount))
                .FirstOrDefault();

        if (hydrateEditForm)
        {
            EditForm = SelectedPlayer is null
                ? new PlayerEditInputModel()
                : new PlayerEditInputModel
                {
                    PlayerId = SelectedPlayer.PlayerId,
                    FirstName = SelectedPlayer.FirstName,
                    PhoneNumber = SelectedPlayer.PhoneNumber,
                    CountryId = SelectedPlayer.CountryId
                };
        }
    }

    public sealed class PlayerEditInputModel
    {
        [Required]
        public Guid PlayerId { get; set; }

        [Required(ErrorMessage = "Enter a first name.")]
        [MinLength(2, ErrorMessage = "First name must contain at least 2 letters.")]
        [RegularExpression(@"^[A-Za-z][A-Za-z' -]*$", ErrorMessage = "Use letters only for the first name.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a phone number.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must contain exactly 10 digits.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Choose a country.")]
        public int CountryId { get; set; }
    }

    public sealed record PlayerListRow(
        Guid PlayerId,
        string FirstName,
        int ExternalId,
        string PhoneNumber,
        int CountryId,
        string CountryName,
        int GameCount);

    public sealed record GameListRow(
        Guid GameSessionId,
        DateTime StartedAtUtc,
        string Status,
        string Outcome,
        int ParticipantCount,
        IReadOnlyList<string> ParticipantNames);

    public sealed record PlayerDetailsRow(
        Guid PlayerId,
        string FirstName,
        int ExternalId,
        string PhoneNumber,
        int CountryId,
        string CountryName,
        int GameCount);
}
