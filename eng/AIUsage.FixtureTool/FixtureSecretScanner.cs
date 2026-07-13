using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AIUsage.FixtureTool;

public static partial class FixtureSecretScanner
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "authorization",
        "cookie",
        "setcookie",
        "apikey",
        "accesstoken",
        "refreshtoken",
        "credential",
        "secret",
        "clientsecret",
        "password",
        "token",
    };

    public static IReadOnlyList<string> ScanJson(ReadOnlyMemory<byte> json, string sourceName)
    {
        using var document = JsonDocument.Parse(json);
        var errors = new List<string>();
        ScanElement(document.RootElement, "$", sourceName, errors);
        return errors;
    }

    public static IReadOnlyList<string> ScanText(string text, string sourceName)
    {
        var errors = new List<string>();
        ScanString(text, "$", sourceName, errors);
        return errors;
    }

    private static void ScanElement(JsonElement element, string path, string sourceName, List<string> errors)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var propertyPath = $"{path}.{property.Name}";
                    if (property.Value.ValueKind == JsonValueKind.String && IsSensitiveKey(property.Name))
                    {
                        var value = property.Value.GetString() ?? string.Empty;
                        if (!ContainsApprovedPlaceholder(value))
                        {
                            errors.Add($"{sourceName}:{propertyPath}: sensitive field is not a fixture placeholder");
                        }
                    }

                    ScanElement(property.Value, propertyPath, sourceName, errors);
                }

                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    ScanElement(item, $"{path}[{index}]", sourceName, errors);
                    index += 1;
                }

                break;

            case JsonValueKind.String:
                ScanString(element.GetString() ?? string.Empty, path, sourceName, errors);
                break;
        }
    }

    private static void ScanString(string value, string path, string sourceName, List<string> errors)
    {
        if (PrivateKeyRegex().IsMatch(value))
        {
            errors.Add($"{sourceName}:{path}: private key material is forbidden");
        }

        if (JwtRegex().IsMatch(value))
        {
            errors.Add($"{sourceName}:{path}: JWT-like value is forbidden");
        }

        if (CommonSecretPrefixRegex().IsMatch(value))
        {
            errors.Add($"{sourceName}:{path}: credential prefix is forbidden");
        }

        if (BearerRegex().IsMatch(value) && !ContainsApprovedPlaceholder(value))
        {
            errors.Add($"{sourceName}:{path}: bearer credential is not a fixture placeholder");
        }

        if (UnixUserPathRegex().IsMatch(value) || WindowsUserPathRegex().IsMatch(value))
        {
            errors.Add($"{sourceName}:{path}: real user path is forbidden");
        }

        foreach (Match match in EmailRegex().Matches(value))
        {
            if (!match.Value.EndsWith("@example.test", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{sourceName}:{path}: real email address is forbidden");
            }
        }

        if (!ContainsApprovedPlaceholder(value) && LooksHighEntropy(value))
        {
            errors.Add($"{sourceName}:{path}: high-entropy value is forbidden");
        }
    }

    private static bool IsSensitiveKey(string key)
    {
        var normalized = key.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);
        return SensitiveKeys.Contains(normalized);
    }

    private static bool ContainsApprovedPlaceholder(string value) =>
        FixturePlaceholderRegex().IsMatch(value);

    private static bool LooksHighEntropy(string value)
    {
        if (value.Length < 48 || value.Any(char.IsWhiteSpace))
        {
            return false;
        }

        return value.All(static character =>
            char.IsAsciiLetterOrDigit(character) || character is '+' or '/' or '=' or '_' or '-');
    }

    [GeneratedRegex("-----BEGIN [A-Z ]*PRIVATE KEY-----", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PrivateKeyRegex();

    [GeneratedRegex(@"\beyJ[A-Za-z0-9_-]+\.eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\b", RegexOptions.CultureInvariant)]
    private static partial Regex JwtRegex();

    [GeneratedRegex(@"\b(?:sk-ant-|sk-|ghp_|github_pat_|AKIA[0-9A-Z]{12,}|xox[baprs]-)[A-Za-z0-9_-]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CommonSecretPrefixRegex();

    [GeneratedRegex(@"\bBearer\s+\S+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BearerRegex();

    [GeneratedRegex(@"/Users/(?!fixture(?:/|$))[^/\s]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UnixUserPathRegex();

    [GeneratedRegex(@"[A-Za-z]:\\Users\\(?!fixture(?:\\|$))[^\\\s]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WindowsUserPathRegex();

    [GeneratedRegex(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"<fixture-[a-z0-9][a-z0-9-]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FixturePlaceholderRegex();
}
