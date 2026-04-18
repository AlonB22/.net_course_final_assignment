using Game.Contracts.Models;
using Game.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace Game.Server.Services;

public sealed class EfCountryCatalogService(GameDbContext dbContext) : ICountryCatalogService
{
    public async Task<IReadOnlyList<CountryOptionDto>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Countries
            .AsNoTracking()
            .OrderBy(country => country.Name)
            .Select(country => new CountryOptionDto(country.CountryId, country.Name, country.IsoCode))
            .ToListAsync(cancellationToken);
    }
}
