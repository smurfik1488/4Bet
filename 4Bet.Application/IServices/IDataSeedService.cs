using _4Bet.Application.DTOs;

namespace _4Bet.Application.IServices;

public interface IDataSeedService
{
    Task<SeedResultDto> SeedHybridAsync(CancellationToken cancellationToken = default);
}
