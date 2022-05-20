using System;
using System.Text;

namespace TangYuan.Uri;

public static class UriEncoding
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

    private static int GetIndexOfFirstEncodeRequiredCharacter(ReadOnlySpan<char> data)
    {
        int length = data.Length;
        unsafe
        {
            fixed (char* p = data)
            {
                int i = 0;
                for (; i + 8 < length; i += 8)
                {
                    if (!UriCharacters.IsUnreservedCharacter(*(p + i))) { return i; }
                    if (!UriCharacters.IsUnreservedCharacter(*(p + i + 1))) { return i + 1; }
                    if (!UriCharacters.IsUnreservedCharacter(*(p + i + 2))) { return i + 2; }
                    if (!UriCharacters.IsUnreservedCharacter(*(p + i + 3))) { return i + 3; }
                    if (!UriCharacters.IsUnreservedCharacter(*(p + i + 4))) { return i + 4; }
                    if (!UriCharacters.IsUnreservedCharacter(*(p + i + 5))) { return i + 5; }
                    if (!UriCharacters.IsUnreservedCharacter(*(p + i + 6))) { return i + 6; }
                    if (!UriCharacters.IsUnreservedCharacter(*(p + i + 7))) { return i + 7; }
                }

                for (; i < length; ++i)
                {
                    if (!UriCharacters.IsUnreservedCharacter(*(p + i))) { return i; }
                }
            }
        }

        return -1;
    }
    
    public static string Encode(string? data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        // avoid memory allocation if there is no need to encode.
        int index = GetIndexOfFirstEncodeRequiredCharacter(data.AsSpan());

        if (index == -1) { return data; }

        // encode at the first non unreserved character occurred. Of course, the first
        // character can be a non unreserved character.
        var encoded = index == 0
            ? new StringBuilder(data.Length + 64)
            : new StringBuilder(data, 0, index, data.Length + 64);

        for (int i = index; i < data.Length;)
        {
            char c = data[i];
            
            // The UTF-16 encoding may be 1 or 2 word per codepoint. Each word can be:
            //
            // * For U+0000 ~ U+D7FF: the encoded word is also 0x0000 ~ 0xD7FF
            // * For U+E000 ~ U+FFFF: the encoded word is also 0xE000 ~ 0xFFFF
            // * For U+010000 ~ U+10FFFF: these code points will be encoded into
            //       surrogate pairs. Each word will be either in 0xD800 – 0xDBFF or
            //       0xDC00 – 0xDFFF
            //
            // So there is no need to validate if the character is in valid range. We
            // just judge which kind of character it is.

            if (UriCharacters.IsUnreservedCharacter(c))
            {
                encoded.Append(c);
                ++i;
            }
            else if (char.IsHighSurrogate(c))
            {
                if (i + 1 >= data.Length)
                {
                    throw new ArgumentException(
                        "Invalid string: high-surrogate character at the end of string.");
                }

                char next = data[i + 1];
                if (!char.IsLowSurrogate(next))
                {
                    throw new ArgumentException("Invalid string: high-surrogate without low-surrogate.");
                }

                ReadOnlySpan<char> codePointToEncode = data.AsSpan(i, 2);
                EncodingCodePoint(codePointToEncode, encoded);
                i += 2;
            }
            else if (char.IsLowSurrogate(c))
            {
                throw new ArgumentException("Invalid string: low-surrogate without high-surrogate.");
            }
            else
            {
                ReadOnlySpan<char> codePointToEncode = data.AsSpan(i, 1);
                EncodingCodePoint(codePointToEncode, encoded);
                ++i;
            }
        }

        return encoded.ToString();
    }

    private static void EncodingCodePoint(ReadOnlySpan<char> codePoint, StringBuilder encoded)
    {
        // The maximum encoded length of one UTF-8 code point is 4 bytes.
        Span<byte> temporaryEncodingBuffer = stackalloc byte[4];
        // The initial value of stackalloc in undefined so we need to clear first.
        temporaryEncodingBuffer.Clear();
        
        // When a new URI scheme defines a component that represents textual
        // data consisting of characters from the Universal Character Set [UCS],
        // the data should first be encoded as octets according to the UTF-8
        // character encoding
        int encodedBytes = Encoding.UTF8.GetBytes(codePoint, temporaryEncodingBuffer);
        for (int encodedIndex = 0; encodedIndex < encodedBytes; ++encodedIndex)
        {
            byte escapedByte = temporaryEncodingBuffer[encodedIndex];
            encoded.Append(ByteToHex[escapedByte]);
        }
    }
}