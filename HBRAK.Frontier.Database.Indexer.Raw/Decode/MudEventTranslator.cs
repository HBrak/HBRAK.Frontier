using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HBRAK.Frontier.Database.Indexer.Raw.Decode.MudStuff;

namespace HBRAK.Frontier.Database.Indexer.Raw.Decode;

internal class MudEventTranslator
{
    public static string ToHuman(MudStoreEvent ev, MudTableSchema? schema)
    {
        var table = schema?.Label ?? ev.TableIdHex;

        if (ev.Kind == MudStoreEventKind.DeleteRecord)
        {
            var keyPairs = schema is null
                ? string.Join(",", ev.KeyTupleHex)
                : string.Join(", ", MudRecordDecoder.DecodeKeys(ev.KeyTupleHex, schema.Key).Select(kv => $"{kv.Key}={kv.Value}"));

            return $"Delete {table}: [{keyPairs}]";
        }

        if (ev.Kind == MudStoreEventKind.SetRecord && schema is not null)
        {
            var (key, fields) = MudRecordDecoder.DecodeSetRecord(ev, schema);
            var keyStr = string.Join(", ", key.Select(kv => $"{kv.Key}={kv.Value}"));
            var fldStr = string.Join(", ", fields.Select(kv => $"{kv.Key}={kv.Value}"));
            return $"Set {table}: [{keyStr}] -> {{ {fldStr} }}";
        }

        if (ev.Kind == MudStoreEventKind.SpliceStaticData && schema is not null)
        {
            var spans = MudStaticLayout.Build(schema);
            var data = HexToBytes(ev.DataHex);
            var hit = MudStaticLayout.AffectedFields(spans, (int)(ev.Start ?? 0), data.Length);

            if (hit.Count == 1)
            {
                var f = hit[0];
                // slice exactly the field portion covered by this splice
                var sliceStart = Math.Max(0, (int)(ev.Start ?? 0) - f.Start);
                var take = Math.Min(data.Length, Math.Max(0, f.Size - sliceStart));
                var part = data.AsSpan(sliceStart, take).ToArray();

                // If the splice covers the whole field (common for 32B fields), decode; else show partial
                var value = (sliceStart == 0 && take == f.Size)
                    ? MudRecordDecoder.HumanValue(f.Field.Type, part)
                    : $"partial({take}/{f.Size})";

                return $"SpliceStatic {table}: field={f.Field.Name} value={value}";
            }

            if (hit.Count > 1)
            {
                var names = string.Join(",", hit.Select(h => h.Field.Name));
                return $"SpliceStatic {table}: fields=[{names}] bytes={data.Length}";
            }

            return $"SpliceStatic {table}: start={(ev.Start ?? 0)} bytes={data.Length}";
        }

        if (ev.Kind == MudStoreEventKind.SpliceStaticDataLegacy && schema is not null)
        {
            var data = HexToBytes(ev.DataHex);
            return $"SpliceStatic(legacy) {table}: start={ev.Start} delete={ev.DeleteCount} bytes={data.Length}";
        }

        if (ev.Kind == MudStoreEventKind.SpliceDynamicData && schema is not null)
        {
            int idx = ev.DynamicFieldIndex ?? -1;
            string fname = (idx >= 0 && idx < schema.DynamicFields.Count) ? schema.DynamicFields[idx].Name : $"dyn[{idx}]";
            var data = HexToBytes(ev.DataHex);
            return $"SpliceDynamic {table}: field={fname} start={ev.StartWithinField} delete={ev.DeleteCountDyn} {HumanDynPreview(schema, idx, data)}";
        }

        return $"{ev.Kind} {table}";
    }

    private static string HumanDynPreview(MudTableSchema schema, int dynIndex, byte[] data)
    {
        if (dynIndex < 0 || dynIndex >= schema.DynamicFields.Count) return $"bytes({data.Length})";
        var f = schema.DynamicFields[dynIndex];
        var v = MudRecordDecoder.HumanValue(f.Type, data);
        return $"{f.Name}={v}";
    }

    private static byte[] HexToBytes(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return Array.Empty<byte>();
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
        int len = hex.Length / 2;
        var data = new byte[len];
        for (int i = 0; i < len; i++) data[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return data;
    }
}