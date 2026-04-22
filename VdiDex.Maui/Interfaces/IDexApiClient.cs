using VdiDex.Maui.Models;

namespace VdiDex.Maui.Interfaces;

public interface IDexApiClient
{
    Task<DexSubmissionResult> SendDexAsync(string machine, string dexContent, CancellationToken cancellationToken = default);
    Task<DexSubmissionResult> ClearAsync(CancellationToken cancellationToken = default);
}
