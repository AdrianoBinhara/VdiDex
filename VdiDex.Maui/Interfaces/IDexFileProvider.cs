namespace VdiDex.Maui.Interfaces;

public interface IDexFileProvider
{
    Task<string> LoadMachineAAsync();
    Task<string> LoadMachineBAsync();
}
