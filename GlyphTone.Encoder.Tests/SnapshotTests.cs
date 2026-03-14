using GlyphTone;

namespace GlyphTone.Tests;

public sealed class SnapshotTests
{
    [Fact]
    public void Serialize_ComplexDocument_MatchesSnapshot()
    {
        var payload = new
        {
            Context = new
            {
                Task = "Our favorite hikes together",
                Location = "Boulder",
                Season = "spring_2025",
            },
            Friends = new[] { "ana", "luis", "sam" },
            Hikes = new[]
            {
                new { Id = 1, Name = "Blue Lake Trail", DistanceKm = 7.5, ElevationGain = 320, Companion = "ana", WasSunny = true },
                new { Id = 2, Name = "Ridge Overlook", DistanceKm = 9.2, ElevationGain = 540, Companion = "luis", WasSunny = false },
                new { Id = 3, Name = "Wildflower Loop", DistanceKm = 5.1, ElevationGain = 180, Companion = "sam", WasSunny = true },
            },
        };

        var actual = GlyphTone.Encoder.Serialize(payload, CreateCamelCaseOptions());
        Assert.Equal(ReadSnapshot("complex-document.toon"), actual);
    }

    [Fact]
    public void Serialize_RootArrayDocument_MatchesSnapshot()
    {
        var payload = new object[]
        {
            new Dictionary<string, object?>
            {
                ["id"] = 1,
                ["meta"] = new Dictionary<string, object?>
                {
                    ["score"] = 9.5,
                },
                ["name"] = "Ada",
            },
            new Dictionary<string, object?>
            {
                ["id"] = 2,
                ["name"] = "Bob",
                ["tags"] = new[] { "admin", "ops" },
            },
        };

        var actual = GlyphTone.Encoder.Serialize(payload);
        Assert.Equal(ReadSnapshot("root-array.toon"), actual);
    }

    private static string ReadSnapshot(string fileName)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, "Snapshots", fileName);
        return File.ReadAllText(fullPath).Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static EncoderOptions CreateCamelCaseOptions()
    {
        return new EncoderOptions
        {
            PropertyNamingPolicy = static name => char.ToLowerInvariant(name[0]) + name[1..],
        };
    }
}