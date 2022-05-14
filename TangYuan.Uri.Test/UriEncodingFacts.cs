using System.Diagnostics.CodeAnalysis;

namespace TangYuan.Uri.Test;

public class UriEncodingFacts
{
    [Fact]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
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
}