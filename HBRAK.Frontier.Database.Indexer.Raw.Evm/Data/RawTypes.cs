using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Database.Indexer.Raw.Evm.Data;

public readonly record struct RawLogRow(
    long BlockNumber,
    byte[] TxHash, 
    int LogIndex,
    byte[] Address,
    byte[] Topic0, 
    byte[]? Topic1,
    byte[]? Topic2,
    byte[]? Topic3,
    byte[]? Data,  
    long BlockTime 
);

internal static class Hex
{
    public static ulong ToULong(string hex)
    {
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
        return hex.Length == 0 ? 0 : ulong.Parse(hex, System.Globalization.NumberStyles.AllowHexSpecifier);
    }

    public static byte[] HexToBytes(string hex)
    {
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
        if (hex.Length == 0) return Array.Empty<byte>();
        if ((hex.Length & 1) == 1) hex = "0" + hex;
        int len = hex.Length / 2;
        var arr = new byte[len];
        for (int i = 0; i < len; i++) arr[i] = byte.Parse(hex.AsSpan(i * 2, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
        return arr;
    }
}
