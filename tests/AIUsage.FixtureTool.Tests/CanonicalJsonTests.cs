using System.Text;
using AIUsage.FixtureTool;
using Xunit;

namespace AIUsage.FixtureTool.Tests;

public sealed class CanonicalJsonTests
{
    [Fact]
    public void SortsObjectKeysAndPreservesArrayOrder()
    {
        var input = Encoding.UTF8.GetBytes("""{"z":1,"array":[3,2,1],"a":{"b":2,"a":1}}""");

        var actual = Encoding.UTF8.GetString(CanonicalJson.Canonicalize(input));

        Assert.Equal("{\"a\":{\"a\":1,\"b\":2},\"array\":[3,2,1],\"z\":1}\n", actual);
    }

    [Fact]
    public void WritesUtf8WithoutEscapingSlashesOrUnicode()
    {
        var input = Encoding.UTF8.GetBytes("""{"url":"https://example.test/路径"}""");

        var actual = Encoding.UTF8.GetString(CanonicalJson.Canonicalize(input));

        Assert.Equal("{\"url\":\"https://example.test/路径\"}\n", actual);
    }
}
