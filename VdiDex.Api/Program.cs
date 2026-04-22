using VdiDex.Api.Auth;
using VdiDex.Api.Endpoints;
using VdiDex.Api.Interfaces;
using VdiDex.Api.Models;
using VdiDex.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BasicAuthCredentials>(
    builder.Configuration.GetSection(BasicAuthCredentials.SectionName));

builder.Services
    .AddAuthentication(BasicAuthSchemeOptions.Scheme)
    .AddScheme<BasicAuthSchemeOptions, BasicAuthHandler>(BasicAuthSchemeOptions.Scheme, _ => { });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<IDexParser, DexParser>();
builder.Services.AddScoped<IDexRepository, DexRepository>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapDexEndpoints();

app.Run();
