namespace VdiDex.Maui.Models;

public sealed class ApiSettings
{
    public const string SectionName = "Api";

    public string BaseUrl { get; set; } = string.Empty;
    public string DexEndpoint { get; set; } = "/vdi-dex";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
