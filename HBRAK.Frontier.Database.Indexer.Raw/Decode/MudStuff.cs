using HBRAK.Frontier.Database.Indexer.Raw.Objects;
using Nethereum.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace HBRAK.Frontier.Database.Indexer.Raw.Decode;

internal class MudStuff
{
    public enum MudStoreEventKind
    {
        SetRecord,
        SpliceStaticData,        // new (no deleteCount)
        SpliceStaticDataLegacy,  // legacy (had deleteCount)
        SpliceDynamicData,
        DeleteRecord
    }

    public sealed record MudStoreEvent
    {
        public MudStoreEventKind Kind { get; init; }
        public string TableIdHex { get; init; } = "0x";     // bytes32
        public string[] KeyTupleHex { get; init; } = Array.Empty<string>(); // bytes32[]

        // SetRecord
        public string? StaticDataHex { get; init; }         // bytes
        public string? EncodedLengthsHex { get; init; }     // bytes32
        public string? DynamicDataHex { get; init; }        // bytes

        // SpliceStaticData
        public ulong? Start { get; init; }                  // uint48
        public string? DataHex { get; init; }               // bytes
        public ulong? DeleteCount { get; init; }            // uint40 (legacy only)

        // SpliceDynamicData
        public byte? DynamicFieldIndex { get; init; }       // uint8
        public ulong? StartWithinField { get; init; }       // uint40
        public ulong? DeleteCountDyn { get; init; }         // uint40

        // Context
        public long BlockNumber { get; init; }
        public long BlockTime { get; init; }
        public string TxHashHex { get; init; } = "0x";
        public int LogIndex { get; init; }
    }

    public static class MudStoreDecoder
    {
        // Canonical event signatures (custom types compile to their underlying ABI: bytes32)
        // SetRecord(bytes32,bytes32[],bytes,bytes32,bytes)
        private static readonly string Sig_SetRecord =
            "Store_SetRecord(bytes32,bytes32[],bytes,bytes32,bytes)";

        // SpliceStaticData new: (bytes32,bytes32[],uint48,bytes)
        private static readonly string Sig_SpliceStaticData_New =
            "Store_SpliceStaticData(bytes32,bytes32[],uint48,bytes)";

        // SpliceStaticData legacy: (bytes32,bytes32[],uint48,uint40,bytes)
        private static readonly string Sig_SpliceStaticData_Legacy =
            "Store_SpliceStaticData(bytes32,bytes32[],uint48,uint40,bytes)";

        // SpliceDynamicData(bytes32,bytes32[],uint8,uint40,uint40,bytes32,bytes)
        private static readonly string Sig_SpliceDynamicData =
            "Store_SpliceDynamicData(bytes32,bytes32[],uint8,uint40,uint40,bytes32,bytes)";

        // DeleteRecord(bytes32,bytes32[])
        private static readonly string Sig_DeleteRecord =
            "Store_DeleteRecord(bytes32,bytes32[])";

        private static readonly byte[] T0_SetRecord = Keccak(Sig_SetRecord);
        private static readonly byte[] T0_SpliceStaticData_New = Keccak(Sig_SpliceStaticData_New);
        private static readonly byte[] T0_SpliceStaticData_Old = Keccak(Sig_SpliceStaticData_Legacy);
        private static readonly byte[] T0_SpliceDynamicData = Keccak(Sig_SpliceDynamicData);
        private static readonly byte[] T0_DeleteRecord = Keccak(Sig_DeleteRecord);

        public static MudStoreEvent? TryDecode(LogRowBase raw)
        {
            if (raw.Topic0 is null || raw.Topic0.Length != 32) return null;

            var topic0 = raw.Topic0;

            if (BytesEq(topic0, T0_SetRecord))
                return Decode_SetRecord(raw);

            if (BytesEq(topic0, T0_SpliceStaticData_New))
                return Decode_SpliceStaticData_New(raw);

            if (BytesEq(topic0, T0_SpliceStaticData_Old))
                return Decode_SpliceStaticData_Old(raw);

            if (BytesEq(topic0, T0_SpliceDynamicData))
                return Decode_SpliceDynamicData(raw);

            if (BytesEq(topic0, T0_DeleteRecord))
                return Decode_DeleteRecord(raw);

            return null;
        }

        // ----------------- Decoders -----------------

        private static MudStoreEvent Decode_SetRecord(LogRowBase raw)
        {
            var ev = NewBase(raw, MudStoreEventKind.SetRecord);
            // tableId is indexed (topic1)
            ev = ev with { TableIdHex = To0x(raw.Topic1) };

            var data = raw.Data ?? Array.Empty<byte>();
            int headCount = 4; // keyTuple, staticData, encodedLengths(bytes32), dynamicData

            // heads
            var keyTupleOff = (int)ToUInt256(ReadWord(data, 0));
            var staticDataOff = (int)ToUInt256(ReadWord(data, 1));
            var encodedLengths = ReadWord(data, 2); // bytes32 in head
            var dynamicDataOff = (int)ToUInt256(ReadWord(data, 3));

            var keyTuple = ReadBytes32Array(data, keyTupleOff);
            var staticData = ReadDynBytes(data, staticDataOff);
            var dynamicData = ReadDynBytes(data, dynamicDataOff);

            return ev with
            {
                KeyTupleHex = keyTuple.Select(To0x).ToArray(),
                StaticDataHex = To0x(staticData),
                EncodedLengthsHex = To0x(encodedLengths),
                DynamicDataHex = To0x(dynamicData)
            };
        }

        private static MudStoreEvent Decode_SpliceStaticData_New(LogRowBase raw)
        {
            var ev = NewBase(raw, MudStoreEventKind.SpliceStaticData);
            ev = ev with { TableIdHex = To0x(raw.Topic1) };

            var data = raw.Data ?? Array.Empty<byte>();
            // head: keyTuple(off), start(uint48 in head), data(off)
            var keyTupleOff = (int)ToUInt256(ReadWord(data, 0));
            var start48 = ToUInt256(ReadWord(data, 1));
            var dataOff = (int)ToUInt256(ReadWord(data, 2));

            return ev with
            {
                KeyTupleHex = ReadBytes32Array(data, keyTupleOff).Select(To0x).ToArray(),
                Start = (ulong)start48,
                DataHex = To0x(ReadDynBytes(data, dataOff))
            };
        }

        private static MudStoreEvent Decode_SpliceStaticData_Old(LogRowBase raw)
        {
            var ev = NewBase(raw, MudStoreEventKind.SpliceStaticDataLegacy);
            ev = ev with { TableIdHex = To0x(raw.Topic1) };

            var data = raw.Data ?? Array.Empty<byte>();
            // head: keyTuple(off), start(uint48), deleteCount(uint40), data(off)
            var keyTupleOff = (int)ToUInt256(ReadWord(data, 0));
            var start48 = ToUInt256(ReadWord(data, 1));
            var deleteCount = ToUInt256(ReadWord(data, 2));
            var dataOff = (int)ToUInt256(ReadWord(data, 3));

            return ev with
            {
                KeyTupleHex = ReadBytes32Array(data, keyTupleOff).Select(To0x).ToArray(),
                Start = (ulong)start48,
                DeleteCount = (ulong)deleteCount,
                DataHex = To0x(ReadDynBytes(data, dataOff))
            };
        }

        private static MudStoreEvent Decode_SpliceDynamicData(LogRowBase raw)
        {
            var ev = NewBase(raw, MudStoreEventKind.SpliceDynamicData);
            ev = ev with { TableIdHex = To0x(raw.Topic1) };

            var data = raw.Data ?? Array.Empty<byte>();
            // head: keyTuple(off), dynamicFieldIndex(uint8), startWithinField(uint40), deleteCount(uint40), encodedLengths(bytes32), data(off)
            var keyTupleOff = (int)ToUInt256(ReadWord(data, 0));
            var dynFieldIndex = (byte)ToUInt256(ReadWord(data, 1));
            var startWithin = ToUInt256(ReadWord(data, 2));
            var deleteCount = ToUInt256(ReadWord(data, 3));
            var encodedLengths = ReadWord(data, 4);
            var dataOff = (int)ToUInt256(ReadWord(data, 5));

            return ev with
            {
                KeyTupleHex = ReadBytes32Array(data, keyTupleOff).Select(To0x).ToArray(),
                DynamicFieldIndex = dynFieldIndex,
                StartWithinField = (ulong)startWithin,
                DeleteCountDyn = (ulong)deleteCount,
                EncodedLengthsHex = To0x(encodedLengths),
                DataHex = To0x(ReadDynBytes(data, dataOff))
            };
        }

        private static MudStoreEvent Decode_DeleteRecord(LogRowBase raw)
        {
            var ev = NewBase(raw, MudStoreEventKind.DeleteRecord);
            ev = ev with { TableIdHex = To0x(raw.Topic1) };

            var data = raw.Data ?? Array.Empty<byte>();
            // head: keyTuple(off)
            var keyTupleOff = (int)ToUInt256(ReadWord(data, 0));

            return ev with
            {
                KeyTupleHex = ReadBytes32Array(data, keyTupleOff).Select(To0x).ToArray()
            };
        }

        // ----------------- Helpers -----------------

        private static MudStoreEvent NewBase(LogRowBase raw, MudStoreEventKind k) => new()
        {
            Kind = k,
            BlockNumber = raw.BlockNumber,
            BlockTime = raw.BlockTime,
            TxHashHex = To0x(raw.TxHash),
            LogIndex = raw.LogIndex
        };

        private static byte[] Keccak(string sig) =>
            Sha3Keccack.Current.CalculateHash(Encoding.ASCII.GetBytes(sig));

        private static bool BytesEq(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        private static byte[] ReadWord(byte[] data, int wordIndex)
        {
            var word = new byte[32];
            int off = wordIndex * 32;
            if (off + 32 <= data.Length) Buffer.BlockCopy(data, off, word, 0, 32);
            return word;
        }

        private static byte[] ReadWordAt(byte[] data, int offset)
        {
            var word = new byte[32];
            if (offset + 32 <= data.Length) Buffer.BlockCopy(data, offset, word, 0, 32);
            return word;
        }

        private static BigInteger ToUInt256(byte[] word)
        {
            var copy = (byte[])word.Clone();
            Array.Reverse(copy);
            return new BigInteger(copy, isUnsigned: true, isBigEndian: false);
        }

        private static byte[][] ReadBytes32Array(byte[] data, int offset)
        {
            if (offset < 0 || offset + 32 > data.Length) return Array.Empty<byte[]>();
            var len = (int)ToUInt256(ReadWordAt(data, offset));
            var arr = new byte[Math.Max(0, len)][];
            int start = offset + 32;
            for (int i = 0; i < len; i++)
            {
                var w = new byte[32];
                int pos = start + i * 32;
                if (pos + 32 <= data.Length) Buffer.BlockCopy(data, pos, w, 0, 32);
                arr[i] = w;
            }
            return arr;
        }

        private static byte[] ReadDynBytes(byte[] data, int offset)
        {
            if (offset < 0 || offset + 32 > data.Length) return Array.Empty<byte>();
            var len = (int)ToUInt256(ReadWordAt(data, offset));
            var dst = new byte[len];
            int start = offset + 32;
            if (start + len > data.Length) len = Math.Max(0, data.Length - start);
            if (len > 0) Buffer.BlockCopy(data, start, dst, 0, len);
            return dst;
        }

        private static string To0x(byte[]? bytes) =>
            bytes is null ? "0x" : "0x" + BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }
}
