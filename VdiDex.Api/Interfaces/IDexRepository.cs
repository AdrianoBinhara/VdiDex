using VdiDex.Api.Models;

namespace VdiDex.Api.Interfaces;

public interface IDexRepository
{
    Task<DexSaveResult> SaveAsync(DexMeter meter, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}
