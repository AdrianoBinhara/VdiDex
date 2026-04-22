using CommunityToolkit.Mvvm.Input;
using VdiDex.Maui.Interfaces;

namespace VdiDex.Maui.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private readonly IDexApiClient _api;
    private readonly IDexFileProvider _dex;

    public MainViewModel(IDexApiClient api, IDexFileProvider dex)
    {
        _api = api;
        _dex = dex;
        Title = "VDI DEX Uploader";
        StatusMessage = "Ready.";

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsBusy))
            {
                SendMachineACommand.NotifyCanExecuteChanged();
                SendMachineBCommand.NotifyCanExecuteChanged();
                ClearTablesCommand.NotifyCanExecuteChanged();
            }
        };
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private Task SendMachineAAsync() => SendAsync("A", _dex.LoadMachineAAsync);

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private Task SendMachineBAsync() => SendAsync("B", _dex.LoadMachineBAsync);

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private async Task ClearTablesAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Clearing tables...";
            var result = await _api.ClearAsync();
            StatusMessage = result.Success
                ? $"Tables cleared ({result.StatusCode})."
                : $"Clear failed: {result.StatusCode} {result.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SendAsync(string machine, Func<Task<string>> loader)
    {
        try
        {
            IsBusy = true;
            StatusMessage = $"Sending Machine {machine}...";

            var dex = await loader();
            var result = await _api.SendDexAsync(machine, dex);

            StatusMessage = result.Success
                ? $"Machine {machine}: {result.StatusCode} OK"
                : $"Machine {machine}: {result.StatusCode} {result.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

}
