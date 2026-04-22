namespace VdiDex.Api.Models;

public sealed class DexLaneMeter
{
    public long Id { get; set; }
    public long DexMeterId { get; set; }
    public string ProductIdentifier { get; set; } = string.Empty;
    public long Price { get; set; }
    public long NumberOfVends { get; set; }
    public long ValueOfPaidSales { get; set; }
}
