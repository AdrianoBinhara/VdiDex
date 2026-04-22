using VdiDex.Api.Models;

namespace VdiDex.Api.Interfaces;

public interface IDexParser
{
    DexMeter Parse(string machine, string dexContent);
}
