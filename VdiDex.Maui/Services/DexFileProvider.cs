using VdiDex.Maui.Interfaces;

namespace VdiDex.Maui.Services;

public sealed class DexFileProvider : IDexFileProvider
{
    private string? _machineA;
    private string? _machineB;

    public async Task<string> LoadMachineAAsync() =>
        _machineA ??= await ReadAssetAsync("dex_machine_a.txt");

    public async Task<string> LoadMachineBAsync() =>
        _machineB ??= await ReadAssetAsync("dex_machine_b.txt");

    private static async Task<string> ReadAssetAsync(string fileName)
    {
        await using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
