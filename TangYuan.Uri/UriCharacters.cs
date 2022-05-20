using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TangYuan.Uri;

public static class UriCharacters
{
    private static readonly HashSet<char> ReservedCharacters = new HashSet<char>
    {
        ':', '/', '?', '#', '[', ']', '@', // gen-delims
        '!', '$', '&', '\'', '(', ')', '*', '+', ',', ';', '=' // sub-delims
    };

    private static readonly HashSet<char> GeneralDelimiters = new HashSet<char>
    {
        ':', '/', '?', '#', '[', ']', '@',
    };
    
    private static readonly HashSet<char> SubDelimiters = new HashSet<char>
    {
        '!', '$', '&', '\'', '(', ')', '*', '+', ',', ';', '='
    };

    public static bool IsReservedCharacters(char c) => ReservedCharacters.Contains(c);
    public static bool IsGeneralDelimiters(char c) => GeneralDelimiters.Contains(c);
    public static bool IsSubDelimiters(char c) => SubDelimiters.Contains(c);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsUnreservedCharacter(char c)
    {
        return c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '.' or '_' or '~';
    }
}