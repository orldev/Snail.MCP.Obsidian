using Snail.MCP.Vault.Application.Excalidraw.Format;

namespace Snail.MCP.Vault.Tests.Application.Excalidraw;

public class LzStringTests
{
    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("hello hello hello hello hello")]
    [InlineData("Схема архитектуры платёжного сервиса")]
    [InlineData("emoji 🚀 and surrogate pairs 𝄞 survive")]
    [InlineData("ÿĀ boundary between 8 and 16 bit char codes")]
    [InlineData("""{"type":"excalidraw","version":2,"elements":[{"id":"a1","type":"rectangle","x":0,"y":0}]}""")]
    public void CompressToBase64_ShortText_DecompressesBack(string input)
    {
        var compressed = LzString.CompressToBase64(input);

        Assert.Equal(input, LzString.DecompressFromBase64(compressed));
    }

    [Fact]
    public void CompressToBase64_LongRepetitiveText_DecompressesBack()
    {
        var input = string.Concat(Enumerable.Repeat("""{"id":"node","type":"arrow","points":[[0,0],[120,40]]},""", 500));

        Assert.Equal(input, LzString.DecompressFromBase64(LzString.CompressToBase64(input)));
    }

    [Fact]
    public void CompressToBase64_AnyText_StaysWithinTheBase64Alphabet()
    {
        var compressed = LzString.CompressToBase64("any payload at all");

        Assert.All(compressed, character => Assert.Contains(character, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/="));
        Assert.Equal(0, compressed.Length % 4);
    }

    [Fact]
    public void DecompressFromBase64_EmptyInput_IsEmptyText()
    {
        Assert.Equal("", LzString.DecompressFromBase64(""));
    }
}
