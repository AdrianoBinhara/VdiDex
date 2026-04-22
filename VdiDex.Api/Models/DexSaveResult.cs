namespace VdiDex.Api.Models;

public enum DexSaveStatus
{
    Created,
    Conflict
}

public sealed record DexSaveResult(DexSaveStatus Status, long DexMeterId, int LaneCount);
