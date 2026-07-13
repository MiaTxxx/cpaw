using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIUsage.FixtureTool;
using Xunit;

namespace AIUsage.FixtureTool.Tests;

public sealed class FixtureSetValidatorTests
{
    [Fact]
    public void AcceptsSortedManifestWithMatchingHash()
    {
        using var fixture = new TemporaryFixtureSet();
        fixture.AddCase("contracts/dashboard/empty", "contracts/dashboard/empty.json", "json-transform", "{}\n");
        fixture.WriteManifest();

        var errors = FixtureSetValidator.Validate(fixture.Root);

        Assert.Empty(errors);
    }

    [Fact]
    public void RejectsUnsortedManifestCases()
    {
        using var fixture = new TemporaryFixtureSet();
        fixture.AddCase("z-case", "z.json", "json-transform", "{}\n");
        fixture.AddCase("a-case", "a.json", "json-transform", "{}\n");
        fixture.WriteManifest(sort: false);

        var errors = FixtureSetValidator.Validate(fixture.Root);

        Assert.Contains(errors, static error => error.Contains("sorted by id", StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsPathTraversal()
    {
        using var fixture = new TemporaryFixtureSet();
        fixture.AddManifestOnlyCase("escape", "../outside.json", "json-transform", new string('0', 64));
        fixture.WriteManifest();

        var errors = FixtureSetValidator.Validate(fixture.Root);

        Assert.Contains(errors, static error => error.Contains("escapes v1 root", StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsHashMismatch()
    {
        using var fixture = new TemporaryFixtureSet();
        fixture.AddCase("case", "case.json", "json-transform", "{}\n", hashOverride: new string('0', 64));
        fixture.WriteManifest();

        var errors = FixtureSetValidator.Validate(fixture.Root);

        Assert.Contains(errors, static error => error.Contains("SHA-256 mismatch", StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsUnlistedFiles()
    {
        using var fixture = new TemporaryFixtureSet();
        fixture.AddUnlistedFile("unlisted.json", "{}\n");
        fixture.WriteManifest();

        var errors = FixtureSetValidator.Validate(fixture.Root);

        Assert.Contains(errors, static error => error.Contains("Unlisted fixture file", StringComparison.Ordinal));
    }

    [Fact]
    public void AcceptsBinaryFixtures()
    {
        using var fixture = new TemporaryFixtureSet();
        fixture.AddBinaryCase("exact/http/binary", "exact/http/binary.bin", "exact-bytes", [0x00, 0xFF, 0x10, 0x20]);
        fixture.WriteManifest();

        var errors = FixtureSetValidator.Validate(fixture.Root);

        Assert.Empty(errors);
    }

    private sealed class TemporaryFixtureSet : IDisposable
    {
        private readonly List<FixtureManifestEntry> entries = [];

        public TemporaryFixtureSet()
        {
            Root = Path.Combine(Path.GetTempPath(), $"AIUsage-FixtureTests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path.Combine(Root, "v1"));
        }

        public string Root { get; }

        public void AddCase(string id, string path, string kind, string content, string? hashOverride = null)
        {
            var fullPath = Path.Combine(Root, "v1", path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            var bytes = Encoding.UTF8.GetBytes(content);
            File.WriteAllBytes(fullPath, bytes);
            AddManifestOnlyCase(id, path, kind, hashOverride ?? Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
        }

        public void AddManifestOnlyCase(string id, string path, string kind, string hash)
        {
            entries.Add(new FixtureManifestEntry
            {
                Id = id,
                Path = path,
                Kind = kind,
                Sha256 = hash,
            });
        }

        public void AddBinaryCase(string id, string path, string kind, byte[] bytes)
        {
            var fullPath = Path.Combine(Root, "v1", path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllBytes(fullPath, bytes);
            AddManifestOnlyCase(id, path, kind, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
        }

        public void AddUnlistedFile(string path, string content)
        {
            var fullPath = Path.Combine(Root, "v1", path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, content, new UTF8Encoding(false));
        }

        public void WriteManifest(bool sort = true)
        {
            var cases = sort ? entries.OrderBy(static entry => entry.Id, StringComparer.Ordinal).ToList() : entries;
            var manifest = new FixtureManifest
            {
                SchemaVersion = 1,
                Cases = [.. cases],
            };
            var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(Root, "v1", "manifest.json"), json, new UTF8Encoding(false));
        }

        public void Dispose()
        {
            var fullRoot = Path.GetFullPath(Root);
            var tempPrefix = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (fullRoot.StartsWith(tempPrefix, StringComparison.OrdinalIgnoreCase))
            {
                Directory.Delete(fullRoot, recursive: true);
            }
        }
    }
}
