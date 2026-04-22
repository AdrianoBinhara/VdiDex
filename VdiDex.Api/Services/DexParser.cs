using System.Globalization;
using VdiDex.Api.Interfaces;
using VdiDex.Api.Models;

namespace VdiDex.Api.Services;

public sealed class DexParser : IDexParser
{
    public DexMeter Parse(string machine, string dexContent)
    {
        if (string.IsNullOrWhiteSpace(dexContent))
            throw new ArgumentException("DEX content is empty.", nameof(dexContent));

        var meter = new DexMeter { Machine = machine };

        string? pendingProduct = null;
        long pendingPrice = 0;
        var productSet = false;

        foreach (var rawLine in dexContent.Split('\n'))
        {
            var line = rawLine.Trim().TrimEnd('/').Trim();
            if (line.Length == 0) continue;

            var fields = line.Split('*');
            var id = fields[0];

            switch (id)
            {
                case "ID1":
                    meter.MachineSerialNumber = Field(fields, 1);
                    break;
                case "ID5":
                    meter.DexDateTime = ParseDexDateTime(Field(fields, 1), Field(fields, 2));
                    break;
                case "VA1":
                    meter.ValueOfPaidVends = ParseLong(Field(fields, 1));
                    break;
                case "PA1":
                    pendingProduct = Field(fields, 1);
                    pendingPrice = ParseLong(Field(fields, 2));
                    productSet = true;
                    break;
                case "PA2" when productSet:
                    meter.Lanes.Add(new DexLaneMeter
                    {
                        ProductIdentifier = pendingProduct!,
                        Price = pendingPrice,
                        NumberOfVends = ParseLong(Field(fields, 1)),
                        ValueOfPaidSales = ParseLong(Field(fields, 2))
                    });
                    productSet = false;
                    pendingProduct = null;
                    pendingPrice = 0;
                    break;
            }
        }

        if (string.IsNullOrEmpty(meter.MachineSerialNumber))
            throw new FormatException("Missing ID1 segment.");
        if (meter.DexDateTime == default)
            throw new FormatException("Missing ID5 segment.");

        return meter;
    }

    private static string Field(string[] fields, int index) =>
        index < fields.Length ? fields[index] : string.Empty;

    private static long ParseLong(string value) =>
        long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;

    private static DateTime ParseDexDateTime(string datePart, string timePart)
    {
        var date = DateTime.ParseExact(datePart, "yyyyMMdd", CultureInfo.InvariantCulture);
        if (timePart.Length >= 4)
        {
            var hh = int.Parse(timePart[..2], CultureInfo.InvariantCulture);
            var mm = int.Parse(timePart.Substring(2, 2), CultureInfo.InvariantCulture);
            var ss = timePart.Length >= 6
                ? int.Parse(timePart.Substring(4, 2), CultureInfo.InvariantCulture)
                : 0;
            date = date.AddHours(hh).AddMinutes(mm).AddSeconds(ss);
        }
        return DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
    }
}
