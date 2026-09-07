using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text;
using Xunit;

namespace Lipis.Flags.Avalonia.Test;

/// <summary>
/// Protects the embedded flag archive: that it holds what the generated table advertises in both
/// directions, that the caching and stream ownership the API promises are real, and that the lock
/// guarding the non-thread-safe <see cref="ZipArchive"/> works.
/// </summary>
/// <remarks>
/// <para>
/// The archive is read here through <see cref="System.Reflection.Assembly.GetManifestResourceStream(string)"/>
/// rather than through the library, deliberately: a test that asked the library what the archive
/// contains could not catch the library and the archive disagreeing.
/// </para>
/// <para>
/// One constraint shapes every assertion in the suite that touches an image. <c>SvgImage</c> is an
/// <c>AvaloniaObject</c>, so reading any of its properties goes through <c>VerifyAccess</c> and
/// requires the Avalonia dispatcher thread; with no <c>Application</c> constructed that is the
/// process entry thread, which an xUnit test body never runs on. Producing the image is
/// unaffected, so tests compare images by reference and make every claim about content against
/// the markup from <see cref="FlagAssets.OpenSvgStream"/>.
/// </para>
/// </remarks>
public class FlagArchiveTests
{
    /// <summary>
    /// The manifest resource name the library's project file writes and its loader reads.
    /// </summary>
    /// <remarks>
    /// Restated rather than imported, for the same reason the folder names are: a constant shared
    /// with the code under test cannot catch that constant changing.
    /// </remarks>
    private const string ResourceName = "flags.zip";

    /// <summary>
    /// Enough of a file to see its root element, however much preamble it carries.
    /// </summary>
    private const int PreambleLength = 512;

    public static TheoryData<string> FlagCodes => FlagTestData.FlagCodes;

    [Fact]
    public void EmbeddedArchive_IsPresentInTheLibraryAssembly()
    {
        var names = typeof(FlagAssets).Assembly.GetManifestResourceNames();

        Assert.Contains(ResourceName, names);
    }

    /// <summary>
    /// Every advertised code opens, in both aspect ratios, onto something that is actually an SVG.
    /// </summary>
    /// <remarks>
    /// A zip entry of the right name containing the wrong bytes decompresses perfectly happily and
    /// fails only when a renderer tries to parse it, which is why the root element is checked and
    /// not merely the length. The check tolerates a byte-order mark, an XML declaration, a doctype
    /// and leading whitespace: none of the current 542 entries carries any of them, but none of
    /// them would be a defect either.
    /// </remarks>
    [Theory]
    [MemberData(nameof(FlagCodes))]
    public void OpenSvgStream_ForEveryAdvertisedCode_YieldsSvgMarkupInBothAspectRatios(string code)
    {
        foreach (var aspectRatio in FlagTestData.AspectRatios)
        {
            using var stream = FlagAssets.OpenSvgStream(code, aspectRatio);

            Assert.NotNull(stream);
            Assert.Equal(0L, stream.Position);

            var markup = FlagTestData.Drain(stream);

            Assert.NotEmpty(markup);
            AssertSvgRoot(markup, $"{FlagTestData.FolderFor(aspectRatio)}/{code}.svg");
        }
    }

    /// <summary>
    /// The other direction: nothing is in the archive that the table does not know about.
    /// </summary>
    /// <remarks>
    /// This is the test that catches an asset sync which dropped new artwork into <c>Assets</c>
    /// without regenerating <c>CountryData.Generated.cs</c>. The flag ships, the archive grows, and
    /// no caller can ever reach it, because every entry point validates against the generated
    /// table first. Nothing else in the suite notices, since every other sweep is driven by that
    /// same table.
    /// </remarks>
    [Fact]
    public void EmbeddedArchive_ContainsExactlyTheAdvertisedCodesInBothAspectRatios()
    {
        var entries = ReadArchiveEntryNames();

        Assert.NotEmpty(entries);

        var advertised = CountryData.AllFlagCodes.ToHashSet(StringComparer.Ordinal);
        var byFolder = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var malformed = new List<string>();

        foreach (var name in entries)
        {
            // Directory markers are structure, not content. The MSBuild ZipDirectory task does not
            // currently emit any, but one appearing would not be a defect.
            if (name.EndsWith('/'))
            {
                continue;
            }

            var separator = name.IndexOf('/');

            if (separator <= 0 || !name.EndsWith(".svg", StringComparison.Ordinal) || name.IndexOf('/', separator + 1) >= 0)
            {
                malformed.Add(name);

                continue;
            }

            var folder = name[..separator];
            var code = name[(separator + 1)..^".svg".Length];

            if (!byFolder.TryGetValue(folder, out var codes))
            {
                byFolder[folder] = codes = new HashSet<string>(StringComparer.Ordinal);
            }

            Assert.True(codes.Add(code), $"The archive holds '{name}' twice.");
        }

        Assert.True(
            malformed.Count == 0,
            $"The archive holds entries that are not <aspectRatio>/<code>.svg: {string.Join(", ", malformed.Take(20))}.");

        Assert.Equal(
            new[] { "1x1", "4x3" },
            byFolder.Keys.Order(StringComparer.Ordinal));

        foreach (var (folder, codes) in byFolder)
        {
            var orphans = codes.Where(code => !advertised.Contains(code)).Order(StringComparer.Ordinal).ToArray();

            Assert.True(
                orphans.Length == 0,
                $"The embedded archive holds artwork under {folder} that no code reaches: {string.Join(", ", orphans)}. " +
                "Regenerate CountryData.Generated.cs after syncing the assets.");

            var missing = advertised.Where(code => !codes.Contains(code)).Order(StringComparer.Ordinal).ToArray();

            Assert.True(
                missing.Length == 0,
                $"The generated table advertises codes the embedded archive has no artwork for under {folder}: {string.Join(", ", missing)}.");
        }
    }

    /// <summary>
    /// Parsing an SVG is far more expensive than decompressing it, so the parsed result is cached.
    /// </summary>
    /// <remarks>
    /// A converter runs once per container realisation, so a scrolling list asks for the same flag
    /// over and over; without the cache each of those would re-parse the markup. Reference equality
    /// is the only way to state that from outside, and it also pins the cache key: the two shapes
    /// must not collapse onto one entry, which would silently serve square artwork to a 4:3 caller.
    /// </remarks>
    [Theory]
    [InlineData("de")]
    [InlineData("us")]
    [InlineData("xk")]
    [InlineData("gb-eng")]
    public void GetImage_ForTheSameCodeAndAspectRatio_ReturnsTheCachedInstance(string code)
    {
        var first = FlagAssets.GetImage(code);
        var second = FlagAssets.GetImage(code, FlagAspectRatio.FourByThree);

        Assert.NotNull(first);
        Assert.Same(first, second);

        var square = FlagAssets.GetImage(code, FlagAspectRatio.OneByOne);
        var squareAgain = FlagAssets.GetImage(code, FlagAspectRatio.OneByOne);

        Assert.NotNull(square);
        Assert.Same(square, squareAgain);

        Assert.NotSame(first, square);
    }

    /// <summary>
    /// The stream, by contrast, is a fresh private copy every time.
    /// </summary>
    /// <remarks>
    /// The caller owns and disposes it, so a shared or lazily-decompressed stream would hand the
    /// second caller a disposed object or one already read to its end. Draining the first and then
    /// disposing it before the second is opened exercises both failure modes.
    /// </remarks>
    [Fact]
    public void OpenSvgStream_ReturnsAnIndependentStreamOnEveryCall()
    {
        var first = FlagAssets.OpenSvgStream("de");

        Assert.NotNull(first);

        var drained = FlagTestData.Drain(first);

        Assert.NotEmpty(drained);
        Assert.Equal(first.Length, first.Position);

        first.Dispose();

        using var second = FlagAssets.OpenSvgStream("de");

        Assert.NotNull(second);
        Assert.NotSame(first, second);
        Assert.Equal(0L, second.Position);
        Assert.Equal(drained, FlagTestData.Drain(second));
    }

    /// <summary>
    /// The archive guard, under every thread this machine has, all starting at once.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ZipArchive"/> keeps a position in one underlying stream and is explicitly not
    /// thread-safe; concurrent entry reads corrupt each other's output or throw. The library holds
    /// a lock across the decompression of a single entry, and this test is what proves the lock is
    /// there. <see cref="FlagAssets.OpenSvgStream"/> is the sharp end, because it never caches and
    /// so reaches the archive on every single call.
    /// </para>
    /// <para>
    /// Deterministic by construction rather than by timing. The expected bytes are read
    /// single-threaded first, a barrier holds every worker until all of them have been created so
    /// the calls genuinely overlap, and each worker walks the identical work list in the identical
    /// order so that they contend on the same entries. No sleeps, no time budget, and the
    /// assertion is on content rather than on speed: torn output fails just as loudly as an
    /// exception, which matters because unsynchronised zip reads usually corrupt before they throw.
    /// </para>
    /// </remarks>
    [Fact]
    public void ArchiveAccess_FromEveryWorkerAtOnce_NeverThrowsAndAlwaysReturnsIntactArtwork()
    {
        var work = FlagTestData.EveryFlag;

        Assert.NotEmpty(work);

        var expected = work.ToDictionary(item => item, item => FlagTestData.ReadFlag(item.Code, item.AspectRatio));

        var workerCount = Math.Max(Environment.ProcessorCount, 8);
        var failures = new ConcurrentBag<string>();

        using var start = new Barrier(workerCount);

        var workers = Enumerable
            .Range(0, workerCount)
            .Select(index => new Thread(() =>
            {
                start.SignalAndWait();

                foreach (var item in work)
                {
                    try
                    {
                        if (FlagAssets.GetImage(item.Code, item.AspectRatio) is null)
                        {
                            failures.Add($"worker {index}: GetImage returned null for '{item.Code}' in {item.AspectRatio}.");
                        }

                        var markup = FlagTestData.TryReadFlag(item.Code, item.AspectRatio);

                        if (markup is null)
                        {
                            failures.Add($"worker {index}: OpenSvgStream returned null for '{item.Code}' in {item.AspectRatio}.");
                        }
                        else if (!markup.AsSpan().SequenceEqual(expected[item]))
                        {
                            failures.Add(
                                $"worker {index}: '{item.Code}' in {item.AspectRatio} came back as {markup.Length} bytes " +
                                $"that differ from the {expected[item].Length} read single-threaded.");
                        }
                    }
                    catch (Exception exception)
                    {
                        failures.Add(
                            $"worker {index}: '{item.Code}' in {item.AspectRatio} threw " +
                            $"{exception.GetType().Name}: {exception.Message}");
                    }
                }
            })
            {
                IsBackground = true,
                Name = $"flag-archive-{index}",
            })
            .ToArray();

        foreach (var worker in workers)
        {
            worker.Start();
        }

        foreach (var worker in workers)
        {
            worker.Join();
        }

        AssertNoFailures(failures);
    }

    /// <summary>
    /// The same guard again, driven by the thread pool rather than by dedicated threads.
    /// </summary>
    /// <remarks>
    /// A converter is invoked from whatever pool thread the UI framework happens to be on, so the
    /// pool is the shape this actually takes in production. Every code is visited twice, once in
    /// each direction, so that a worker warming the image cache for a code is racing another
    /// worker still cold on it.
    /// </remarks>
    [Fact]
    public void ArchiveAccess_InParallelAcrossTheWholeCodeSet_NeverThrowsAndAlwaysReturnsArtwork()
    {
        var work = FlagTestData.EveryFlag.Concat(FlagTestData.EveryFlag.Reverse()).ToArray();

        Assert.NotEmpty(work);

        var failures = new ConcurrentBag<string>();
        var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount, 8) };

        Parallel.For(0, work.Length, options, index =>
        {
            var (code, aspectRatio) = work[index];

            try
            {
                if (FlagAssets.GetImage(code, aspectRatio) is null)
                {
                    failures.Add($"GetImage returned null for '{code}' in {aspectRatio}.");
                }

                if (FlagTestData.TryReadFlag(code, aspectRatio) is not { Length: > 0 })
                {
                    failures.Add($"OpenSvgStream returned nothing for '{code}' in {aspectRatio}.");
                }
            }
            catch (Exception exception)
            {
                failures.Add($"'{code}' in {aspectRatio} threw {exception.GetType().Name}: {exception.Message}");
            }
        });

        AssertNoFailures(failures);
    }

    private static void AssertNoFailures(ConcurrentBag<string> failures)
        => Assert.True(
            failures.IsEmpty,
            $"{failures.Count} concurrent lookups misbehaved:{Environment.NewLine}" +
            string.Join(Environment.NewLine, failures.Order(StringComparer.Ordinal).Take(20)));

    /// <summary>
    /// Reads the names of everything in the embedded archive.
    /// </summary>
    private static IReadOnlyList<string> ReadArchiveEntryNames()
    {
        using var stream = typeof(FlagAssets).Assembly.GetManifestResourceStream(ResourceName);

        Assert.NotNull(stream);

        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        return archive.Entries.Select(entry => entry.FullName).ToArray();
    }

    /// <summary>
    /// Asserts that some markup opens an SVG document, past any preamble it is entitled to carry.
    /// </summary>
    private static void AssertSvgRoot(byte[] markup, string what)
    {
        var text = Encoding.UTF8.GetString(markup, 0, Math.Min(markup.Length, PreambleLength))
            .TrimStart('\uFEFF')
            .TrimStart();

        if (text.StartsWith("<?xml", StringComparison.Ordinal))
        {
            var close = text.IndexOf("?>", StringComparison.Ordinal);

            Assert.True(close >= 0, $"{what} opens an XML declaration that never closes: '{Excerpt(text)}'.");

            text = text[(close + 2)..].TrimStart();
        }

        if (text.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
        {
            var close = text.IndexOf('>');

            Assert.True(close >= 0, $"{what} opens a doctype that never closes: '{Excerpt(text)}'.");

            text = text[(close + 1)..].TrimStart();
        }

        // The name has to end where an element name may end, so that a hypothetical <svgfoo> root
        // is not mistaken for an SVG.
        var isSvgRoot = text.StartsWith("<svg", StringComparison.Ordinal)
                        && (text.Length == 4 || char.IsWhiteSpace(text[4]) || text[4] is '>' or '/');

        Assert.True(isSvgRoot, $"{what} does not begin with an <svg> root element: '{Excerpt(text)}'.");
    }

    private static string Excerpt(string text) => text.Length <= 60 ? text : text[..60] + "...";
}
