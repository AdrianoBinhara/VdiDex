using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using VdiDex.Api.Interfaces;
using VdiDex.Api.Models;

namespace VdiDex.Api.Services;

public sealed class DexRepository : IDexRepository
{
    private const int SqlUniqueConstraintError = 2627;
    private const int SqlUniqueIndexError = 2601;

    private readonly string _connectionString;
    private readonly ILogger<DexRepository> _logger;

    public DexRepository(IConfiguration configuration, ILogger<DexRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("VdiDex")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:VdiDex.");
        _logger = logger;
    }

    public async Task<DexSaveResult> SaveAsync(DexMeter meter, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var meterId = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
                "dbo.SaveDEXMeter",
                new
                {
                    Machine = meter.Machine,
                    DEXDateTime = meter.DexDateTime,
                    MachineSerialNumber = meter.MachineSerialNumber,
                    ValueOfPaidVends = meter.ValueOfPaidVends
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

            if (meter.Lanes.Count > 0)
            {
                var laneTable = BuildLaneTable(meterId, meter.Lanes);
                var parameters = new DynamicParameters();
                parameters.Add("@Lanes", laneTable.AsTableValuedParameter("dbo.DEXLaneMeterType"));

                await connection.ExecuteAsync(new CommandDefinition(
                    "dbo.SaveDEXLaneMeters",
                    parameters,
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));
            }

            await transaction.CommitAsync(cancellationToken);
            return new DexSaveResult(DexSaveStatus.Created, meterId, meter.Lanes.Count);
        }
        catch (SqlException ex) when (ex.Number is SqlUniqueConstraintError or SqlUniqueIndexError)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogWarning("Duplicate DEX meter for Machine {Machine} at {DexDateTime}", meter.Machine, meter.DexDateTime);
            return new DexSaveResult(DexSaveStatus.Conflict, 0, 0);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "dbo.ClearDexTables",
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));
    }

    private static DataTable BuildLaneTable(long meterId, IReadOnlyList<DexLaneMeter> lanes)
    {
        var table = new DataTable();
        table.Columns.Add("DEXMeterId", typeof(long));
        table.Columns.Add("ProductIdentifier", typeof(string));
        table.Columns.Add("Price", typeof(long));
        table.Columns.Add("NumberOfVends", typeof(long));
        table.Columns.Add("ValueOfPaidSales", typeof(long));

        foreach (var lane in lanes)
        {
            table.Rows.Add(meterId, lane.ProductIdentifier, lane.Price, lane.NumberOfVends, lane.ValueOfPaidSales);
        }

        return table;
    }
}
