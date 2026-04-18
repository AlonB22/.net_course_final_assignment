using Game.Server.Models;

namespace Game.Server.Data;

internal static class CountrySeedData
{
    internal static readonly Country[] Countries =
    [
        new() { CountryId = 1, Name = "Israel", IsoCode = "IL" },
        new() { CountryId = 2, Name = "United States", IsoCode = "US" },
        new() { CountryId = 3, Name = "Germany", IsoCode = "DE" },
        new() { CountryId = 4, Name = "France", IsoCode = "FR" },
        new() { CountryId = 5, Name = "United Kingdom", IsoCode = "GB" },
        new() { CountryId = 6, Name = "Canada", IsoCode = "CA" },
        new() { CountryId = 7, Name = "Italy", IsoCode = "IT" },
        new() { CountryId = 8, Name = "Spain", IsoCode = "ES" },
        new() { CountryId = 9, Name = "Japan", IsoCode = "JP" },
        new() { CountryId = 10, Name = "Brazil", IsoCode = "BR" }
    ];
}
