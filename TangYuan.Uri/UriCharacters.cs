using System.Collections.Generic;

namespace TangYuan.Uri
{
    public class UriCharacters
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

        public static bool IsUnreservedCharacter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') ||
                   c == '-' || c == '.' || c == '_' || c == '~';
        }
    }
}