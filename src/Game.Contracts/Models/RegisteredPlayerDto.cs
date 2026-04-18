namespace Game.Contracts.Models;

public sealed record RegisteredPlayerDto(
    Guid PlayerId,
    string FirstName,
    int ExternalId,
    string PhoneNumber,
    string CountryName);
