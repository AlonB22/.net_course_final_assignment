namespace Game.Contracts.Models;

public sealed record SessionRegistrationRequestDto(
    int MoveTimeLimitSeconds,
    IReadOnlyList<ParticipantRegistrationDto> Participants);
