using System.Text;
using AIUsage.FixtureTool;
using Xunit;

namespace AIUsage.FixtureTool.Tests;

public sealed class FixtureSecretScannerTests
{
    [Fact]
    public void AcceptsSyntheticCredentialPlaceholders()
    {
        var json = Encoding.UTF8.GetBytes("""
            {
              "authorization": "Bearer <fixture-upstream-key>",
              "cookie": "session=<fixture-cookie-a>",
              "email": "alice@example.test",
              "path": "C:\\fixture\\profiles\\account.json"
            }
            """);

        var errors = FixtureSecretScanner.ScanJson(json, "accepted.json");

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("{\"apiKey\":\"sk-ant-realisticsecret123456\"}")]
    [InlineData("{\"authorization\":\"Bearer actual-secret\"}")]
    [InlineData("{\"email\":\"person@company.com\"}")]
    [InlineData("{\"path\":\"C:\\\\Users\\\\chen\\\\secret.json\"}")]
    [InlineData("{\"pem\":\"-----BEGIN PRIVATE KEY-----\"}")]
    public void RejectsLikelyProductionSecrets(string json)
    {
        var errors = FixtureSecretScanner.ScanJson(Encoding.UTF8.GetBytes(json), "rejected.json");

        Assert.NotEmpty(errors);
    }
}
