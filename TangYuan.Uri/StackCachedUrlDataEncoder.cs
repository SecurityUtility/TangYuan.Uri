using System;
using System.Diagnostics;
using System.Text;

namespace TangYuan.Uri;

internal ref struct StackCachedUrlDataEncoder
{
    private static readonly string[] ByteToHex =
    {
        "%00", "%01", "%02", "%03", "%04", "%05", "%06", "%07", "%08", "%09", "%0A", "%0B", "%0C", "%0D", "%0E", "%0F",
        "%10", "%11", "%12", "%13", "%14", "%15", "%16", "%17", "%18", "%19", "%1A", "%1B", "%1C", "%1D", "%1E", "%1F",
        "%20", "%21", "%22", "%23", "%24", "%25", "%26", "%27", "%28", "%29", "%2A", "%2B", "%2C", "%2D", "%2E", "%2F",
        "%30", "%31", "%32", "%33", "%34", "%35", "%36", "%37", "%38", "%39", "%3A", "%3B", "%3C", "%3D", "%3E", "%3F",
        "%40", "%41", "%42", "%43", "%44", "%45", "%46", "%47", "%48", "%49", "%4A", "%4B", "%4C", "%4D", "%4E", "%4F",
        "%50", "%51", "%52", "%53", "%54", "%55", "%56", "%57", "%58", "%59", "%5A", "%5B", "%5C", "%5D", "%5E", "%5F",
        "%60", "%61", "%62", "%63", "%64", "%65", "%66", "%67", "%68", "%69", "%6A", "%6B", "%6C", "%6D", "%6E", "%6F",
        "%70", "%71", "%72", "%73", "%74", "%75", "%76", "%77", "%78", "%79", "%7A", "%7B", "%7C", "%7D", "%7E", "%7F",
        "%80", "%81", "%82", "%83", "%84", "%85", "%86", "%87", "%88", "%89", "%8A", "%8B", "%8C", "%8D", "%8E", "%8F",
        "%90", "%91", "%92", "%93", "%94", "%95", "%96", "%97", "%98", "%99", "%9A", "%9B", "%9C", "%9D", "%9E", "%9F",
        "%A0", "%A1", "%A2", "%A3", "%A4", "%A5", "%A6", "%A7", "%A8", "%A9", "%AA", "%AB", "%AC", "%AD", "%AE", "%AF",
        "%B0", "%B1", "%B2", "%B3", "%B4", "%B5", "%B6", "%B7", "%B8", "%B9", "%BA", "%BB", "%BC", "%BD", "%BE", "%BF",
        "%C0", "%C1", "%C2", "%C3", "%C4", "%C5", "%C6", "%C7", "%C8", "%C9", "%CA", "%CB", "%CC", "%CD", "%CE", "%CF",
        "%D0", "%D1", "%D2", "%D3", "%D4", "%D5", "%D6", "%D7", "%D8", "%D9", "%DA", "%DB", "%DC", "%DD", "%DE", "%DF",
        "%E0", "%E1", "%E2", "%E3", "%E4", "%E5", "%E6", "%E7", "%E8", "%E9", "%EA", "%EB", "%EC", "%ED", "%EE", "%EF",
        "%F0", "%F1", "%F2", "%F3", "%F4", "%F5", "%F6", "%F7", "%F8", "%F9", "%FA", "%FB", "%FC", "%FD", "%FE", "%FF"
    };
    
    private const int CacheStatusCopy = 1;
    private const int CacheStatusEncode = 2;
    private const int CacheStatusInitial = 0;
    private const int MaxCacheSize = 256;
    private const int MinCacheSize = 32;

    private int _cacheStatus = CacheStatusInitial;
    private int _cachePosition = 0;
    private readonly Span<char> _cache;
    private readonly int _cacheSize;
    private readonly StringBuilder _heapBuilder;

    public StackCachedUrlDataEncoder(Span<char> cache, int capacity)
    {
        Debug.Assert(cache.Length is >= MinCacheSize and <= MaxCacheSize);
        
        _cache = cache;
        _cacheSize = cache.Length;
        _heapBuilder = new StringBuilder(capacity);
    }

    public unsafe string Encode(ReadOnlySpan<char> data, int indexToStart)
    {
        // state            |trigger                |action                     |next state
        // ------------------------------------------------------------------------
        // initial          |Unreserved character   |accumulate                 |copy
        // initial          |High surrogate         |get-low & accumulate       |encode
        // initial          |Low surrogate          |(error)                    |(n/a)
        // initial          |Bmp                    |accumulate                 |encode
        // copy             |Unreserved character   |accumulate                 |copy
        // copy             |High surrogate         |flush & get-low & accumu.. |encode
        // copy             |Low surrogate          |(error)                    |(n/a)
        // copy             |Bmp                    |flush & accumulate         |encode
        // encode           |Unreserved character   |flush & accumulate         |copy
        // encode           |High surrogate         |get-low & accumulate       |encode
        // encode           |Low surrogate          |(error)                    |(n/a)
        // encode           |Bmp                    |accumulate                 |encode
        if (indexToStart > 0)
        {
            _heapBuilder.Append(data.Slice(0, indexToStart));
        }

        int length = data.Length;
        fixed (char* p = data)
        {
            for (int i = indexToStart; i < length;)
            {
                char c = *(p + i);

                if (UriCharacters.IsUnreservedCharacter(c))
                {
                    if (_cacheStatus == CacheStatusEncode)
                    {
                        FlushCache();
                    }

                    CopyToHeapBuilder(c);
                    _cacheStatus = CacheStatusCopy;
                    ++i;
                    continue;
                }

                if (char.IsHighSurrogate(c))
                {
                    if (i + 1 >= length)
                    {
                        throw new ArgumentException(
                            "Invalid string: high-surrogate character at the end of string.");
                    }

                    char next = *(p + i + 1);
                    if (!char.IsLowSurrogate(next))
                    {
                        throw new ArgumentException("Invalid string: high-surrogate without low-surrogate.");
                    }

                    AccumulateEncode(p + i, 2);
                    _cacheStatus = CacheStatusEncode;
                    i += 2;
                    continue;
                }

                if (char.IsLowSurrogate(c))
                {
                    throw new ArgumentException("Invalid string: low-surrogate without high-surrogate.");
                }

                {
                    AccumulateEncode(p + i, 1);
                    _cacheStatus = CacheStatusEncode;
                    ++i;
                }
            }
        }
        
        FlushCache();
        return _heapBuilder.ToString();
    }

    private void CopyToHeapBuilder(char c)
    {
        _heapBuilder.Append(c);
    }

    private unsafe void AccumulateEncode(char* firstCharacter, int length)
    {
        ReserveCacheSpace(length);
        var source = new ReadOnlySpan<char>(firstCharacter, length);
        source.CopyTo(_cache.Slice(_cachePosition, length));
        _cachePosition += length;
    }

    private void ReserveCacheSpace(int numberOfCharacters)
    {
        if (_cachePosition + numberOfCharacters - 1 >= _cacheSize)
        {
            FlushCache();
        }
    }

    private void FlushCache()
    {
        if (_cachePosition == 0) { return; }
        FlushCacheEncode();
    }

    private void FlushCacheEncode()
    {
        Span<byte> utf8Encoded = stackalloc byte[_cacheSize * 4];
        int encodedBytesNumber = Encoding.UTF8.GetBytes(_cache.Slice(0, _cachePosition), utf8Encoded);
        for (int i = 0; i < encodedBytesNumber; ++i)
        {
            _heapBuilder.Append(ByteToHex[utf8Encoded[i]]);
        }

        _cachePosition = 0;
    }
}