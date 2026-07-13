namespace AIUsage.FixtureTool;

internal static class Program
{
    public static int Main(string[] args)
    {
        if (args is ["validate", var fixtureRoot])
        {
            var errors = FixtureSetValidator.Validate(fixtureRoot);
            if (errors.Count == 0)
            {
                Console.WriteLine($"Golden fixtures are valid: {Path.GetFullPath(fixtureRoot)}");
                return 0;
            }

            foreach (var error in errors)
            {
                Console.Error.WriteLine(error);
            }

            return 1;
        }

        if (args is ["canonicalize", var inputPath, var outputPath])
        {
            var canonical = CanonicalJson.Canonicalize(File.ReadAllBytes(inputPath));
            File.WriteAllBytes(outputPath, canonical);
            return 0;
        }

        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  AIUsage.FixtureTool validate <fixture-root>");
        Console.Error.WriteLine("  AIUsage.FixtureTool canonicalize <input-json> <output-json>");
        return 2;
    }
}
