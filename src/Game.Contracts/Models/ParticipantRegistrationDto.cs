namespace Game.Contracts.Models;

public sealed record ParticipantRegistrationDto(
    string FirstName,
    int ExternalId,
    string PhoneNumber,
    int CountryId);
