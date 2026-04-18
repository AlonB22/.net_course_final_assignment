using Game.Contracts.Models;

namespace Game.Server.Services;

public interface ICountryCatalogService
{
    Task<IReadOnlyList<CountryOptionDto>> GetCountriesAsync(CancellationToken cancellationToken = default);
}
