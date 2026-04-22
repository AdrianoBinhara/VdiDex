namespace VdiDex.Api.Models;

public sealed class DexMeter
{
    public long Id { get; set; }
    public string Machine { get; set; } = string.Empty;
    public DateTime DexDateTime { get; set; }
    public string MachineSerialNumber { get; set; } = string.Empty;
    public long ValueOfPaidVends { get; set; }
    public DateTime ReceivedAt { get; set; }
    public List<DexLaneMeter> Lanes { get; set; } = new();
}
