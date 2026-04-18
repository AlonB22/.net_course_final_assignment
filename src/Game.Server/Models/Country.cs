namespace Game.Server.Models;

public sealed class Country
{
    public int CountryId { get; set; }

    public required string Name { get; set; }

    public required string IsoCode { get; set; }

    public ICollection<Player> Players { get; set; } = [];
}
