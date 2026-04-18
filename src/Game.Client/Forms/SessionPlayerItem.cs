using Game.Contracts.Models;

namespace Game.Client.Forms;

public sealed class SessionPlayerItem(RegisteredPlayerDto player, bool isPrimary)
{
    public RegisteredPlayerDto Player { get; } = player;

    public bool IsPrimary { get; } = isPrimary;

    public Guid PlayerId => Player.PlayerId;

    public override string ToString()
    {
        var prefix = IsPrimary ? "*" : string.Empty;
        return $"{prefix}{Player.FirstName} #{Player.ExternalId} ({Player.CountryName})";
    }
}
