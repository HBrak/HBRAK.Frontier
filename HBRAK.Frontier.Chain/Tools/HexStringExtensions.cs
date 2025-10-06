using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Chain.Tools;

public static class HexStringExtensions
{
    public static HexBigInteger? ToHexBigInt(string? hexOrDec)
    {
        if (string.IsNullOrWhiteSpace(hexOrDec)) return null;

        if (hexOrDec.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return new HexBigInteger(hexOrDec.HexToBigInteger(false));

        if (ulong.TryParse(hexOrDec, out var u))
            return new HexBigInteger(u);

        if (BigInteger.TryParse(hexOrDec, out var b))
            return new HexBigInteger(b);

        return null;
    }

    public static BigInteger? ToBigInt(string? hexOrDec)
    {
        if (string.IsNullOrWhiteSpace(hexOrDec)) return null;

        if (hexOrDec.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return hexOrDec.HexToBigInteger(false);

        if (BigInteger.TryParse(hexOrDec, out var b))
            return b;

        return null;
    }
}



