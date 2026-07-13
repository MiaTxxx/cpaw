using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AIUsage.FixtureTool;

public static class FixtureSetValidator
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
    private static readonly StringComparison PathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    public static IReadOnlyList<string> Validate(string fixtureRoot)
    {
        var errors = new List<string>();
        var root = Path.GetFullPath(fixtureRoot);
        var versionRoot = Path.Combine(root, "v1");
        var manifestPath = Path.Combine(versionRoot, "manifest.json");

        if (!File.Exists(manifestPath))
        {
            return [$"Missing fixture manifest: {manifestPath}"];
        }

        FixtureManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<FixtureManifest>(File.ReadAllBytes(manifestPath));
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            return [$"Invalid fixture manifest: {exception.Message}"];
        }

        if (manifest is null)
        {
            return ["Fixture manifest decoded to null"];
        }

        if (manifest.SchemaVersion != 1)
        {
            errors.Add($"Unsupported fixture schemaVersion: {manifest.SchemaVersion}");
        }

        var sortedIds = manifest.Cases.Select(static entry => entry.Id)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
        if (!manifest.Cases.Select(static entry => entry.Id).SequenceEqual(sortedIds, StringComparer.Ordinal))
        {
            errors.Add("Fixture manifest cases must be sorted by id");
        }

        var duplicateIds = manifest.Cases.GroupBy(static entry => entry.Id, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key);
        foreach (var duplicateId in duplicateIds)
        {
            errors.Add($"Duplicate fixture id: {duplicateId}");
        }

        var versionPrefix = Path.GetFullPath(versionRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var listedPaths = new HashSet<string>(PathComparer);
        foreach (var entry in manifest.Cases)
        {
            var fullPath = ResolveEntryPath(entry, versionPrefix, errors);
            if (fullPath is null)
            {
                continue;
            }

            if (!listedPaths.Add(fullPath))
            {
                errors.Add($"Duplicate fixture path: {entry.Path}");
                continue;
            }

            ValidateEntry(entry, fullPath, versionPrefix, errors);
        }

        foreach (var file in Directory.EnumerateFiles(versionRoot, "*", SearchOption.AllDirectories))
        {
            var fullPath = Path.GetFullPath(file);
            if (fullPath.Equals(Path.GetFullPath(manifestPath), PathComparison))
            {
                continue;
            }

            if (!listedPaths.Contains(fullPath))
            {
                errors.Add($"Unlisted fixture file: {Path.GetRelativePath(versionRoot, fullPath).Replace('\\', '/')}");
            }
        }

        return errors;
    }

    private static string? ResolveEntryPath(FixtureManifestEntry entry, string versionPrefix, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(entry.Id) || string.IsNullOrWhiteSpace(entry.Path) || string.IsNullOrWhiteSpace(entry.Kind))
        {
            errors.Add("Fixture entries require non-empty id, path, and kind");
            return null;
        }

        var relativePath = entry.Path.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(versionPrefix, relativePath));
        if (!fullPath.StartsWith(versionPrefix, PathComparison))
        {
            errors.Add($"Fixture path escapes v1 root: {entry.Path}");
            return null;
        }

        return fullPath;
    }

    private static void ValidateEntry(
        FixtureManifestEntry entry,
        string fullPath,
        string versionPrefix,
        List<string> errors)
    {

        if (!File.Exists(fullPath))
        {
            errors.Add($"Fixture file is missing: {entry.Path}");
            return;
        }

        if (ContainsReparsePoint(fullPath, versionPrefix))
        {
            errors.Add($"Fixture paths may not contain symbolic links or reparse points: {entry.Path}");
            return;
        }

        var bytes = File.ReadAllBytes(fullPath);
        var actualHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (!string.Equals(actualHash, entry.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Fixture SHA-256 mismatch: {entry.Path}");
        }

        try
        {
            var extension = Path.GetExtension(fullPath);
            var scanErrors = extension.Equals(".json", StringComparison.OrdinalIgnoreCase)
                ? FixtureSecretScanner.ScanJson(bytes, entry.Path)
                : extension.Equals(".bin", StringComparison.OrdinalIgnoreCase)
                    ? FixtureSecretScanner.ScanText(Encoding.Latin1.GetString(bytes), entry.Path)
                    : FixtureSecretScanner.ScanText(StrictUtf8.GetString(bytes), entry.Path);
            errors.AddRange(scanErrors);
        }
        catch (Exception exception) when (exception is JsonException or DecoderFallbackException)
        {
            errors.Add($"Fixture is not valid UTF-8/JSON for its extension: {entry.Path}: {exception.Message}");
        }
    }

    private static bool ContainsReparsePoint(string fullPath, string versionPrefix)
    {
        var currentPath = fullPath;
        while (currentPath.StartsWith(versionPrefix, PathComparison))
        {
            if ((File.GetAttributes(currentPath) & FileAttributes.ReparsePoint) != 0)
            {
                return true;
            }

            var parent = Path.GetDirectoryName(currentPath);
            if (parent is null || parent.Equals(currentPath, PathComparison))
            {
                break;
            }

            currentPath = parent;
        }

        return false;
    }
}
