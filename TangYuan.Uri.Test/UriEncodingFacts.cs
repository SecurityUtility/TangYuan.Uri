using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace TangYuan.Uri.Test;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class UriEncodingFacts
{
    [Fact]
    public void should_not_encoding_characters_if_they_are_unreserved_characters()
    {
        const string unreservedCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._~";
        Assert.Equal(unreservedCharacters, UriEncoding.Encode(unreservedCharacters));
    }

    [Theory]
    [InlineData("%", "%25")]
    [InlineData(" <>#%+{}|\\^[]`;/?:@=&$", "%20%3C%3E%23%25%2B%7B%7D%7C%5C%5E%5B%5D%60%3B%2F%3F%3A%40%3D%26%24")]
    public void should_encoding_characters_which_are_not_unreserved_characters(string data, string expectedEncoded)
    {
        Assert.Equal(expectedEncoded, UriEncoding.Encode(data));
    }

    [Theory]
    [InlineData("abc/def", "abc%2Fdef")]
    [InlineData("hello world", "hello%20world")]
    public void should_not_encoding_unreserved_characters_when_mixed_with_not_unreserved_characters(
        string data, string expectedEncoded)
    {
        Assert.Equal(expectedEncoded, UriEncoding.Encode(data));
    }

    [Theory]
    [InlineData("\u5929\u9a6c\u6d41\u661f\u62f3", "%E5%A4%A9%E9%A9%AC%E6%B5%81%E6%98%9F%E6%8B%B3")] // BMP
    [InlineData("\U00012200", "%F0%92%88%80")] // Surrogate pair
    [InlineData("\u5929\u9a6c\U00012200\u6d41\u661f\u62f3", "%E5%A4%A9%E9%A9%AC%F0%92%88%80%E6%B5%81%E6%98%9F%E6%8B%B3")]
    [InlineData("\u5929\u9a6c\u6d41\u661f\U00012200\u62f3", "%E5%A4%A9%E9%A9%AC%E6%B5%81%E6%98%9F%F0%92%88%80%E6%8B%B3")]
    public void should_encoding_to_utf8_first_to_produce_the_percent_encoding(string data, string expectedEncoded)
    {
        Assert.Equal(expectedEncoded, UriEncoding.Encode(data));
    }

    [Fact]
    public void should_throw_if_the_last_character_is_a_surrogate_high_character()
    {
        string endWithSurrogateHighPart = 
            new StringBuilder().Append("normal text").Append((char)0xd801).ToString();
        Assert.Throws<ArgumentException>(() => UriEncoding.Encode(endWithSurrogateHighPart));
    }

    [Fact]
    public void should_throw_if_the_first_surrogate_character_is_the_low_part()
    {
        string surrogateLowPartOnly = 
            new StringBuilder().Append("nomral").Append((char)0xdc00).Append("text").ToString();
        Assert.Throws<ArgumentException>(() => UriEncoding.Encode(surrogateLowPartOnly));
    }
    
    [Fact]
    public void should_throw_if_surrogate_high_part_without_low_part()
    {
        string highWithoutLow = new StringBuilder().Append("normal").Append((char)0xd801).Append("text").ToString();
        Assert.Throws<ArgumentException>(() => UriEncoding.Encode(highWithoutLow));
    }
}