namespace Game.Server.Models;

public sealed class Player
{
    public Guid PlayerId { get; set; }

    public int ExternalId { get; set; }

    public required string FirstName { get; set; }

    public required string PhoneNumber { get; set; }

    public int CountryId { get; set; }

    public Country? Country { get; set; }

    public ICollection<GameParticipant> GameParticipations { get; set; } = [];
}
