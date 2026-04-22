using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using VdiDex.Maui.Interfaces;
using VdiDex.Maui.Models;

namespace VdiDex.Maui.Services;

public sealed class DexApiClient : IDexApiClient
{
    private readonly HttpClient _http;
    private readonly ApiSettings _settings;

    public DexApiClient(HttpClient http, IOptions<ApiSettings> options)
    {
        _settings = options.Value;
        _http = http;
        _http.BaseAddress ??= new Uri(_settings.BaseUrl);
        _http.DefaultRequestHeaders.Authorization = BuildBasicAuth(_settings.Username, _settings.Password);
    }

    public async Task<DexSubmissionResult> SendDexAsync(string machine, string dexContent, CancellationToken cancellationToken = default)
    {
        var url = $"{_settings.DexEndpoint}?machine={Uri.EscapeDataString(machine)}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(dexContent, Encoding.UTF8, "text/plain")
        };

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return new DexSubmissionResult(
                response.IsSuccessStatusCode,
                (int)response.StatusCode,
                response.IsSuccessStatusCode ? "OK" : $"{response.ReasonPhrase}: {body}");
        }
        catch (Exception ex)
        {
            return new DexSubmissionResult(false, 0, ex.Message);
        }
    }

    public async Task<DexSubmissionResult> ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _http.DeleteAsync(_settings.DexEndpoint, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return new DexSubmissionResult(
                response.IsSuccessStatusCode,
                (int)response.StatusCode,
                response.IsSuccessStatusCode ? "Cleared" : $"{response.ReasonPhrase}: {body}");
        }
        catch (Exception ex)
        {
            return new DexSubmissionResult(false, 0, ex.Message);
        }
    }

    private static AuthenticationHeaderValue BuildBasicAuth(string user, string pass)
    {
        var raw = Encoding.UTF8.GetBytes($"{user}:{pass}");
        return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(raw));
    }
}
