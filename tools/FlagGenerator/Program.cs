using System.Text;
using System.Text.Json;

namespace FlagGenerator;

// Regenerates CountryData.Generated.cs from the vendored country.json. Generating the table rather
// than hand-maintaining it is what makes an unattended asset sync safe: the names and the artwork
// come from the same upstream release and cannot drift apart.
internal static class Program
{
    // Names where the upstream value is not the one we want to ship. Kept deliberately tiny and
    // reviewed by hand. Upstream drops the ring diacritic from "Åland Islands"; that is a mojibake
    // regression rather than an editorial choice, so it is corrected here instead of being
    // inherited on every regeneration.
    private static readonly Dictionary<string, string> NameOverrides = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AX"] = "Åland Islands",
    };

    private static int Main(string[] args)
    {
        var repoRoot = args.Length > 0 ? args[0] : FindRepoRoot();

        if (repoRoot is null)
        {
            Console.Error.WriteLine("Could not locate the repository root. Pass it as the first argument.");
            return 1;
        }

        var countryJsonPath = Path.Combine(repoRoot, "country.json");
        var outputPath = args.Length > 1
            ? args[1]
            : Path.Combine(repoRoot, "Lipis.Flags.Avalonia", "obj", "generated", "CountryData.g.cs");
        var assetsRoot = Path.Combine(repoRoot, "Lipis.Flags.Avalonia", "Assets");

        if (!File.Exists(countryJsonPath))
        {
            Console.Error.WriteLine($"Not found: {countryJsonPath}");
            return 1;
        }

        var countries = JsonSerializer.Deserialize<List<CountryEntry>>(
            File.ReadAllText(countryJsonPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        // ISO 3166-1 alpha-2 proper: the named countries the public table exposes.
        var namedCountries = countries
            .Where(c => c.Iso && c.Code.Length == 2)
            .OrderBy(c => c.Code, StringComparer.Ordinal)
            .Select(c => (Code: c.Code.ToUpperInvariant(), Name: Resolve(c)))
            .ToList();

        // Every code with an asset, including the non-ISO entities flag-icons ships. Membership
        // tests resolve against this set, not the named set, or those flags would be unreachable.
        var flagCodes = countries
            .Select(c => c.Code.ToLowerInvariant())
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();

        VerifyAssetsExist(assetsRoot, flagCodes);

        // AppendLine uses Environment.NewLine, so normalise: the file is compiled on whatever
        // OS built it, and a stable byte sequence keeps the build reproducible.
        Write(outputPath, Render(namedCountries, flagCodes).Replace("\r\n", "\n"));

        Console.WriteLine($"Wrote {outputPath}");
        Console.WriteLine($"  {namedCountries.Count} named ISO countries");
        Console.WriteLine($"  {flagCodes.Count} flag codes");
        return 0;
    }

    // Written through a temporary file and moved into place, because both target frameworks build
    // in parallel and each one runs this. The identical-content check keeps the timestamp stable so
    // an otherwise up-to-date build is not forced to recompile.
    private static void Write(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        if (File.Exists(path) && File.ReadAllText(path) == content)
        {
            return;
        }

        var temporary = $"{path}.{Environment.ProcessId}.tmp";

        File.WriteAllText(temporary, content, new UTF8Encoding(false));
        File.Move(temporary, path, overwrite: true);
    }


    private static string Resolve(CountryEntry entry)
        => NameOverrides.TryGetValue(entry.Code, out var name) ? name : entry.Name;

    // Fails generation when the table would advertise a flag that is not on disk. The table is the
    // only membership authority at runtime, so such a code passes Exists and then resolves to
    // nothing at all.
    private static void VerifyAssetsExist(string assetsRoot, IEnumerable<string> flagCodes)
    {
        if (!Directory.Exists(assetsRoot))
        {
            Console.Error.WriteLine($"Warning: assets directory not found at {assetsRoot}; skipping asset verification.");
            return;
        }

        var missing = new List<string>();

        foreach (var code in flagCodes)
        {
            foreach (var folder in (string[])["4x3", "1x1"])
            {
                if (!File.Exists(Path.Combine(assetsRoot, folder, $"{code}.svg")))
                {
                    missing.Add($"{folder}/{code}.svg");
                }
            }
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"country.json lists {missing.Count} flag(s) with no asset on disk: {string.Join(", ", missing.Take(10))}" +
                (missing.Count > 10 ? ", ..." : string.Empty));
        }
    }

    private static string Render(List<(string Code, string Name)> namedCountries, List<string> flagCodes)
    {
        var builder = new StringBuilder();

        builder.AppendLine("// <auto-generated>");
        builder.AppendLine("//     Generated from country.json, which is vendored verbatim from lipis/flag-icons.");
        builder.AppendLine("//     Do not edit by hand. Regenerate with:");
        builder.AppendLine("//         dotnet run --project tools/FlagGenerator");
        builder.AppendLine("// </auto-generated>");
        builder.AppendLine();
        builder.AppendLine("using System.Collections.Frozen;");
        builder.AppendLine();
        builder.AppendLine("namespace Lipis.Flags.Avalonia;");
        builder.AppendLine();
        builder.AppendLine("public partial class CountryData");
        builder.AppendLine("{");
        builder.AppendLine("    private static readonly Dictionary<string, string> _englishNameByIso2 =");
        builder.AppendLine("        new(StringComparer.OrdinalIgnoreCase)");
        builder.AppendLine("        {");

        foreach (var (code, name) in namedCountries)
        {
            builder.AppendLine($"            {{ \"{code}\", {Literal(name)} }},");
        }

        builder.AppendLine("        };");
        builder.AppendLine();
        builder.AppendLine("    // Every code with an embedded flag, including the non-ISO entities flag-icons ships.");
        builder.AppendLine("    private static readonly FrozenSet<string> _flagCodes =");
        builder.AppendLine("        new[]");
        builder.AppendLine("        {");

        foreach (var code in flagCodes)
        {
            builder.AppendLine($"            \"{code}\",");
        }

        builder.AppendLine("        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);");
        builder.AppendLine("}");

        return builder.ToString();
    }

    // Emits a C# string literal, escaping only what C# requires. Non-ASCII characters are written
    // out verbatim rather than escaped, so names such as "Åland Islands" and "Türkiye" stay
    // readable in the generated source. The file is written as UTF-8 without a BOM and the compiler
    // reads it as UTF-8.
    private static string Literal(string value)
        => $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";

    private static string? FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "country.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private sealed record CountryEntry(string Code, string Name, bool Iso);
}
