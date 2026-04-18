using Game.Server.Models;
using Game.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Game.Server.Pages.Queries;

public class IndexModel(IQueryRetrievalService queryRetrievalService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? NameFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedDisplayName { get; set; }

    public IReadOnlyList<PlayerActivityRow> ActivePlayers { get; private set; } = [];
    public IReadOnlyList<PlayerRecentGameRow> PlayersByRecentGame { get; private set; } = [];
    public IReadOnlyList<GameSessionRow> GameSessions { get; private set; } = [];
    public IReadOnlyList<CountryRepresentativeRow> CountryRepresentatives { get; private set; } = [];
    public IReadOnlyList<DisplayNameOptionRow> DisplayNameOptions { get; private set; } = [];
    public IReadOnlyList<SelectListItem> DisplayNameSelectItems { get; private set; } = [];
    public IReadOnlyList<PlayerNameViewRow> PlayersWithSelectedDisplayName { get; private set; } = [];
    public IReadOnlyList<PlayerGameCountRow> PlayerGameCounts { get; private set; } = [];
    public IReadOnlyList<GameParticipationGroupRow> GameParticipationGroups { get; private set; } = [];
    public IReadOnlyList<CountryPlayersRow> CountryPlayers { get; private set; } = [];
    public IReadOnlyList<TopCountryRow> TopCountries { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var players = await queryRetrievalService.GetPlayersAsync(includeGameParticipations: true);
        var countries = await queryRetrievalService.GetCountriesAsync();
        var sessions = await queryRetrievalService.GetGameSessionsAsync(includeParticipants: true);

        ActivePlayers = BuildActivePlayers(players, NameFilter);
        PlayersByRecentGame = BuildPlayersByRecentGame(players);
        GameSessions = BuildGameSessions(sessions);
        CountryRepresentatives = BuildCountryRepresentatives(countries, players);
        DisplayNameOptions = BuildDisplayNameOptions(players);
        SelectedDisplayName ??= DisplayNameOptions.FirstOrDefault()?.DisplayName;
        DisplayNameSelectItems = DisplayNameOptions
            .Select(option => new SelectListItem(
                $"{option.DisplayName} ({option.PlayerCount})",
                option.DisplayName,
                option.DisplayName == SelectedDisplayName))
            .ToArray();
        PlayersWithSelectedDisplayName = BuildPlayersWithSelectedDisplayName(players, SelectedDisplayName);
        PlayerGameCounts = BuildPlayerGameCounts(players);
        GameParticipationGroups = BuildGameParticipationGroups(players);
        CountryPlayers = BuildCountryPlayers(countries, players);
        TopCountries = BuildTopCountries(countries, players);
    }

    private static IReadOnlyList<PlayerActivityRow> BuildActivePlayers(
        IReadOnlyList<Player> players,
        string? nameFilter)
    {
        return players
            .Where(player => player.GameParticipations.Any())
            .Where(player => string.IsNullOrWhiteSpace(nameFilter)
                || player.FirstName.Contains(nameFilter, StringComparison.Ordinal))
            .OrderBy(player => player.FirstName, StringComparer.Ordinal)
            .ThenBy(player => player.ExternalId)
            .Select(player => new PlayerActivityRow(
                player.FirstName,
                player.ExternalId,
                player.PhoneNumber,
                player.Country?.Name ?? string.Empty,
                player.GameParticipations.Count,
                player.GameParticipations.Min(participation => participation.GameSession!.StartedAtUtc),
                player.GameParticipations.Max(participation => participation.GameSession!.StartedAtUtc)))
            .ToArray();
    }

    private static IReadOnlyList<PlayerRecentGameRow> BuildPlayersByRecentGame(IReadOnlyList<Player> players)
    {
        return players
            .OrderByDescending(player => player.FirstName, StringComparer.Ordinal)
            .ThenBy(player => player.ExternalId)
            .Select(player => new PlayerRecentGameRow(
                player.FirstName,
                player.GameParticipations.Any()
                    ? player.GameParticipations.Max(participation => participation.GameSession!.StartedAtUtc)
                    : null))
            .ToArray();
    }

    private static IReadOnlyList<GameSessionRow> BuildGameSessions(IReadOnlyList<GameSession> sessions)
    {
        return sessions
            .OrderByDescending(session => session.StartedAtUtc)
            .ThenBy(session => session.GameSessionId)
            .Select(session => new GameSessionRow(
                session.GameSessionId,
                session.StartedAtUtc,
                session.CompletedAtUtc,
                session.Status.ToString(),
                session.Outcome.ToString(),
                session.MoveTimeLimitSeconds,
                session.ParticipantCount,
                session.Participants
                    .OrderBy(participant => participant.TurnOrder)
                    .Select(participant => participant.Player?.FirstName ?? string.Empty)
                    .ToArray()))
            .ToArray();
    }

    private static IReadOnlyList<CountryRepresentativeRow> BuildCountryRepresentatives(
        IReadOnlyList<Country> countries,
        IReadOnlyList<Player> players)
    {
        var playedPlayers = players.Where(player => player.GameParticipations.Any()).ToArray();

        return countries
            .Select(country =>
            {
                var firstPlayer = playedPlayers
                    .Where(player => player.CountryId == country.CountryId)
                    .OrderBy(player => player.GameParticipations.Min(participation => participation.GameSession!.StartedAtUtc))
                    .ThenBy(player => player.FirstName, StringComparer.Ordinal)
                    .ThenBy(player => player.ExternalId)
                    .FirstOrDefault();

                return firstPlayer is null
                    ? null
                    : new CountryRepresentativeRow(
                        country.Name,
                        firstPlayer.FirstName,
                        firstPlayer.GameParticipations.Min(participation => participation.GameSession!.StartedAtUtc));
            })
            .Where(row => row is not null)
            .Select(row => row!)
            .ToArray();
    }

    private static IReadOnlyList<DisplayNameOptionRow> BuildDisplayNameOptions(IReadOnlyList<Player> players)
    {
        return players
            .Select(player => player.FirstName.Trim())
            .Where(firstName => !string.IsNullOrWhiteSpace(firstName))
            .GroupBy(firstName => firstName, StringComparer.OrdinalIgnoreCase)
            .Select(group => new DisplayNameOptionRow(
                group.OrderBy(name => name, StringComparer.Ordinal).First(),
                group.Count()))
            .OrderBy(option => option.DisplayName, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<PlayerNameViewRow> BuildPlayersWithSelectedDisplayName(
        IReadOnlyList<Player> players,
        string? selectedDisplayName)
    {
        return string.IsNullOrWhiteSpace(selectedDisplayName)
            ? []
            : players
                .Where(player => string.Equals(
                    player.FirstName.Trim(),
                    selectedDisplayName,
                    StringComparison.OrdinalIgnoreCase))
                .OrderBy(player => player.FirstName, StringComparer.Ordinal)
                .ThenBy(player => player.ExternalId)
                .Select(player => new PlayerNameViewRow(
                    player.FirstName,
                    player.ExternalId,
                    player.Country?.Name ?? string.Empty,
                    player.GameParticipations.Count))
                .ToArray();
    }

    private static IReadOnlyList<PlayerGameCountRow> BuildPlayerGameCounts(IReadOnlyList<Player> players)
    {
        return players
            .OrderByDescending(player => player.GameParticipations.Count)
            .ThenBy(player => player.FirstName, StringComparer.Ordinal)
            .ThenBy(player => player.ExternalId)
            .Select(player => new PlayerGameCountRow(
                player.FirstName,
                player.GameParticipations.Count))
            .ToArray();
    }

    private static IReadOnlyList<GameParticipationGroupRow> BuildGameParticipationGroups(IReadOnlyList<Player> players)
    {
        return players
            .GroupBy(player => player.GameParticipations.Count)
            .OrderByDescending(group => group.Key)
            .Select(group => new GameParticipationGroupRow(
                group.Key,
                group.Count(),
                group.OrderBy(player => player.FirstName, StringComparer.Ordinal)
                    .ThenBy(player => player.ExternalId)
                    .Select(player => player.FirstName)
                    .ToArray()))
            .ToArray();
    }

    private static IReadOnlyList<CountryPlayersRow> BuildCountryPlayers(
        IReadOnlyList<Country> countries,
        IReadOnlyList<Player> players)
    {
        var playedPlayers = players.Where(player => player.GameParticipations.Any()).ToArray();

        return countries
            .Select(country => new CountryPlayersRow(
                country.Name,
                playedPlayers.Count(player => player.CountryId == country.CountryId),
                playedPlayers.Where(player => player.CountryId == country.CountryId)
                    .OrderBy(player => player.FirstName, StringComparer.Ordinal)
                    .ThenBy(player => player.ExternalId)
                    .Select(player => player.FirstName)
                    .ToArray()))
            .Where(row => row.PlayerCount > 0)
            .OrderBy(row => row.CountryName, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<TopCountryRow> BuildTopCountries(
        IReadOnlyList<Country> countries,
        IReadOnlyList<Player> players)
    {
        var playedPlayers = players.Where(player => player.GameParticipations.Any()).ToArray();

        return countries
            .Select(country => new TopCountryRow(
                country.Name,
                playedPlayers.Count(player => player.CountryId == country.CountryId)))
            .Where(row => row.PlayerCount > 0)
            .OrderByDescending(row => row.PlayerCount)
            .ThenBy(row => row.CountryName, StringComparer.Ordinal)
            .Take(2)
            .ToArray();
    }

    public sealed record PlayerActivityRow(
        string FirstName,
        int ExternalId,
        string PhoneNumber,
        string CountryName,
        int GameCount,
        DateTime FirstGameUtc,
        DateTime LastGameUtc);

    public sealed record PlayerRecentGameRow(
        string FirstName,
        DateTime? LastGameUtc);

    public sealed record GameSessionRow(
        Guid GameSessionId,
        DateTime StartedAtUtc,
        DateTime? CompletedAtUtc,
        string Status,
        string Outcome,
        int MoveTimeLimitSeconds,
        int ParticipantCount,
        IReadOnlyList<string> ParticipantNames);

    public sealed record CountryRepresentativeRow(
        string CountryName,
        string PlayerName,
        DateTime FirstGameUtc);

    public sealed record DisplayNameOptionRow(
        string DisplayName,
        int PlayerCount);

    public sealed record PlayerNameViewRow(
        string FirstName,
        int ExternalId,
        string CountryName,
        int GameCount);

    public sealed record PlayerGameCountRow(
        string FirstName,
        int GameCount);

    public sealed record GameParticipationGroupRow(
        int GameCount,
        int PlayerCount,
        IReadOnlyList<string> PlayerNames);

    public sealed record CountryPlayersRow(
        string CountryName,
        int PlayerCount,
        IReadOnlyList<string> PlayerNames);

    public sealed record TopCountryRow(
        string CountryName,
        int PlayerCount);
}
