using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Database.Indexer.Raw.Decode;

internal class MudStaticLayout
{
    public sealed record FieldSpan(MudField Field, int Start, int Size);

    public static List<FieldSpan> Build(MudTableSchema schema)
    {
        var spans = new List<FieldSpan>(schema.StaticFields.Count);
        int off = 0;
        foreach (var f in schema.StaticFields)
        {
            var size = f.FixedSizeBytes > 0 ? f.FixedSizeBytes : 32; // safe upper bound
            spans.Add(new FieldSpan(f, off, size));
            off += size;
        }
        return spans;
    }

    /// <summary>
    /// Returns all fields affected by a splice [start, start+len).
    /// </summary>
    public static List<FieldSpan> AffectedFields(List<FieldSpan> spans, int start, int len)
    {
        var end = start + Math.Max(0, len);
        return spans.Where(s => RangesOverlap(start, end, s.Start, s.Start + s.Size)).ToList();
    }

    private static bool RangesOverlap(int a0, int a1, int b0, int b1) => a0 < b1 && b0 < a1;
}
