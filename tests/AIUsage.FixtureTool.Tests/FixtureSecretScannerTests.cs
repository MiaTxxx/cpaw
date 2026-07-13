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

    [Fact]
    public void AcceptsLongStructuredFixtureIdentifiers()
    {
        var json = Encoding.UTF8.GetBytes("""
            {"id":"contracts/provider/provider-result-failure-no-summary"}
            """);

        var errors = FixtureSecretScanner.ScanJson(json, "identifier.json");

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("{\"apiKey\":\"sk-ant-realisticsecret123456\"}")]
    [InlineData("{\"authorization\":\"Bearer actual-secret\"}")]
    [InlineData("{\"email\":\"person@company.com\"}")]
    [InlineData("{\"path\":\"C:\\\\Users\\\\chen\\\\secret.json\"}")]
    [InlineData("{\"pem\":\"-----BEGIN PRIVATE KEY-----\"}")]
    [InlineData("{\"blob\":\"aB3dE5fG7hJ9kL2mN4pQ6rS8tV0wX1yZcD3eF5gH7jK9mP2qR4sT6uV8wY0zA1bC\"}")]
    public void RejectsLikelyProductionSecrets(string json)
    {
        var errors = FixtureSecretScanner.ScanJson(Encoding.UTF8.GetBytes(json), "rejected.json");

        Assert.NotEmpty(errors);
    }
}
