using Microsoft.AspNetCore.Mvc;
using VdiDex.Api.Interfaces;
using VdiDex.Api.Models;

namespace VdiDex.Api.Endpoints;

public static class DexEndpoints
{
    public static IEndpointRouteBuilder MapDexEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/vdi-dex", HandleAsync)
            .RequireAuthorization()
            .WithName("SubmitDex");

        app.MapDelete("/vdi-dex", ClearAsync)
            .RequireAuthorization()
            .WithName("ClearDex");

        return app;
    }

    private static async Task<IResult> ClearAsync(
        IDexRepository repository,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        await repository.ClearAsync(cancellationToken);
        logger.LogInformation("DEX tables cleared.");
        return Results.NoContent();
    }

    private static readonly HashSet<string> AllowedMachines = new(StringComparer.Ordinal) { "A", "B" };

    private static bool IsValidMachine(string machine) => AllowedMachines.Contains(machine);

    private static async Task<IResult> HandleAsync(
        [FromQuery] string machine,
        HttpRequest request,
        IDexParser parser,
        IDexRepository repository,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(machine))
            return Results.BadRequest(new { error = "Query parameter 'machine' is required." });

        if (!IsValidMachine(machine))
            return Results.BadRequest(new { error = "Query parameter 'machine' must be 'A' or 'B'." });

        string dex;
        using (var reader = new StreamReader(request.Body))
        {
            dex = await reader.ReadToEndAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(dex))
            return Results.BadRequest(new { error = "Request body is empty." });

        DexMeter meter;
        try
        {
            meter = parser.Parse(machine, dex);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            logger.LogWarning(ex, "DEX parse failed for Machine {Machine}", machine);
            return Results.BadRequest(new { error = ex.Message });
        }

        var result = await repository.SaveAsync(meter, cancellationToken);

        return result.Status switch
        {
            DexSaveStatus.Created => Results.Created($"/vdi-dex/{result.DexMeterId}", new
            {
                dexMeterId = result.DexMeterId,
                machine = meter.Machine,
                dexDateTime = meter.DexDateTime,
                serial = meter.MachineSerialNumber,
                lanes = result.LaneCount
            }),
            DexSaveStatus.Conflict => Results.Conflict(new
            {
                error = "A DEX meter record already exists for this machine and DEX timestamp."
            }),
            _ => Results.Problem("Unknown save result.")
        };
    }
}
