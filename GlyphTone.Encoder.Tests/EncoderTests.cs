using GlyphTone;

namespace GlyphTone.Tests;

public sealed class EncoderTests
{
    private static readonly int[] ExcessiveCollectionPayload = [1, 2, 3];

    [Fact]
    public void Serialize_Primitives_UsesCanonicalForms()
    {
        Assert.Equal("null", GlyphTone.Encoder.Serialize<object?>(null));
        Assert.Equal("true", GlyphTone.Encoder.Serialize(true));
        Assert.Equal("42", GlyphTone.Encoder.Serialize(42));
        Assert.Equal("1.5", GlyphTone.Encoder.Serialize(1.5000m));
        Assert.Equal("1000000", GlyphTone.Encoder.Serialize(1e6d));
        Assert.Equal("0.000001", GlyphTone.Encoder.Serialize(1e-6d));
        Assert.Equal("0", GlyphTone.Encoder.Serialize(-0.0d));
        Assert.Equal("null", GlyphTone.Encoder.Serialize(double.PositiveInfinity));
        Assert.Equal("\"340282366920938463463374607431768211456\"", GlyphTone.Encoder.Serialize(System.Numerics.BigInteger.Parse("340282366920938463463374607431768211456")));
    }

    [Theory]
    [InlineData("safe text", "safe text")]
    [InlineData("", "\"\"")]
    [InlineData("true", "\"true\"")]
    [InlineData("05", "\"05\"")]
    [InlineData("-dash", "\"-dash\"")]
    [InlineData("hello:world", "\"hello:world\"")]
    [InlineData("hello,world", "\"hello,world\"")]
    [InlineData("line\nfeed", "\"line\\nfeed\"")]
    [InlineData("tab\tvalue", "\"tab\\tvalue\"")]
    [InlineData("quote\"value", "\"quote\\\"value\"")]
    public void Serialize_Strings_QuotesConservatively(string input, string expected)
    {
        Assert.Equal(expected, GlyphTone.Encoder.Serialize(input));
    }

    [Fact]
    public void Serialize_NestedObject_UsesIndentationBasedLayout()
    {
        var payload = new SampleEnvelope
        {
            Tags = ["ops", "admin"],
            Title = "example",
            User = new SamplePerson
            {
                Active = true,
                Id = 7,
                Name = "Ada",
            },
        };

        var options = CreateCamelCaseOptions();
        var result = GlyphTone.Encoder.Serialize(payload, options);

        var expected = string.Join(
            "\n",
            "tags[2]: ops,admin",
            "title: example",
            "user:",
            "  active: true",
            "  id: 7",
            "  name: Ada");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Serialize_IgnoreNullValues_OmitsNullObjectMembers()
    {
        var payload = new Dictionary<string, object?>
        {
            ["alpha"] = 1,
            ["beta"] = null,
        };

        var options = new EncoderOptions
        {
            IgnoreNullValues = true,
        };

        var result = GlyphTone.Encoder.Serialize(payload, options);
        Assert.Equal("alpha: 1", result);
    }

    [Fact]
    public void Serialize_Dictionary_CanPreserveInsertionOrder_WhenSortingDisabled()
    {
        var payload = new Dictionary<string, object?>
        {
            ["zeta"] = 1,
            ["alpha"] = 2,
            ["beta"] = 3,
        };

        var options = new EncoderOptions
        {
            SortDictionaryKeys = false,
        };

        var result = GlyphTone.Encoder.Serialize(payload, options);

        var expected = string.Join(
            "\n",
            "zeta: 1",
            "alpha: 2",
            "beta: 3");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Serialize_Properties_AreSortedOrdinallyByDefault()
    {
        var payload = new OrderedModel
        {
            Alpha = 1,
            Beta = 2,
            Zeta = 3,
        };

        var options = CreateCamelCaseOptions();
        var result = GlyphTone.Encoder.Serialize(payload, options);

        var expected = string.Join(
            "\n",
            "alpha: 1",
            "beta: 2",
            "zeta: 3");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Serialize_CanIncludePublicFields_WhenEnabled()
    {
        var payload = new PublicFieldContainer
        {
            Id = 42,
            Name = "Ada",
        };

        var options = CreateCamelCaseOptions();
        options.IncludeFields = true;

        var result = GlyphTone.Encoder.Serialize(payload, options);
        Assert.Equal("id: 42\nname: Ada", result);
    }

    [Fact]
    public void Serialize_TabularArrays_WhenEligible()
    {
        var payload = new Dictionary<string, object?>
        {
            ["users"] = new[]
            {
                new SamplePerson { Active = true, Id = 1, Name = "Alice" },
                new SamplePerson { Active = false, Id = 2, Name = "Bob" },
            },
        };

        var options = CreateCamelCaseOptions();
        var result = GlyphTone.Encoder.Serialize(payload, options);

        var expected = string.Join(
            "\n",
            "users[2]{active,id,name}:",
            "  true,1,Alice",
            "  false,2,Bob");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Serialize_FallsBackToExpandedArray_WhenObjectsAreNotTabular()
    {
        var payload = new Dictionary<string, object?>
        {
            ["items"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["id"] = 1,
                    ["name"] = "First",
                },
                new Dictionary<string, object?>
                {
                    ["id"] = 2,
                    ["meta"] = new Dictionary<string, object?> { ["flag"] = true },
                },
            },
        };

        var result = GlyphTone.Encoder.Serialize(payload);

        var expected = string.Join(
            "\n",
            "items[2]:",
            "  - id: 1",
            "    name: First",
            "  - id: 2",
            "    meta:",
            "      flag: true");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Serialize_EmptyStructures_UseCanonicalRepresentations()
    {
        Assert.Equal(string.Empty, GlyphTone.Encoder.Serialize(new Dictionary<string, object?>()));

        var nested = new Dictionary<string, object?>
        {
            ["emptyArray"] = Array.Empty<int>(),
            ["emptyObject"] = new Dictionary<string, object?>(),
        };

        var result = GlyphTone.Encoder.Serialize(nested);
        Assert.Equal("emptyArray[0]:\nemptyObject:", result);
    }

    [Fact]
    public void Serialize_TextWriterOverload_WritesToExistingWriter()
    {
        using var writer = new StringWriter();
        GlyphTone.Encoder.Serialize(new Dictionary<string, object?> { ["id"] = 1 }, writer);
        Assert.Equal("id: 1", writer.ToString());
    }

    [Fact]
    public void Serialize_UnsupportedTypes_ThrowUsefulErrors()
    {
        var exception = Assert.Throws<ToonEncodingException>(() => GlyphTone.Encoder.Serialize(new Dictionary<string, object?>
        {
            ["action"] = new Action(static () => { }),
        }));

        Assert.Contains("$.action", exception.Message, StringComparison.Ordinal);
        Assert.Contains("delegates", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Serialize_CircularReferences_AreRejected()
    {
        var node = new LinkedNode { Name = "root" };
        node.Next = node;

        var exception = Assert.Throws<ToonEncodingException>(() => GlyphTone.Encoder.Serialize(node, CreateCamelCaseOptions()));
        Assert.Contains("Circular reference", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_StrictMode_RejectsNonLfLineEndings()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => GlyphTone.Encoder.Serialize(
            new Dictionary<string, object?> { ["id"] = 1 },
            new EncoderOptions { NewLine = "\r\n" }));

        Assert.Contains("LF", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_NonStrictMode_AllowsCustomLineEndings()
    {
        var options = new EncoderOptions
        {
            NewLine = "\r\n",
            StrictMode = false,
        };

        var payload = new Dictionary<string, object?>
        {
            ["alpha"] = 1,
            ["beta"] = 2,
        };

        var result = GlyphTone.Encoder.Serialize(payload, options);
        Assert.Equal("alpha: 1\r\nbeta: 2", result);
    }

    [Fact]
    public void Serialize_DisabledReflectionObjectSerialization_RejectsPocos()
    {
        var options = CreateCamelCaseOptions();
        options.AllowReflectionObjectSerialization = false;

        var exception = Assert.Throws<ToonEncodingException>(() => GlyphTone.Encoder.Serialize(
            new SamplePerson { Active = true, Id = 1, Name = "Ada" },
            options));

        Assert.Contains("Reflection-based object serialization is disabled", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_DisabledReflectionObjectSerialization_StillAllowsDictionaries()
    {
        var options = new EncoderOptions
        {
            AllowReflectionObjectSerialization = false,
        };

        var result = GlyphTone.Encoder.Serialize(new Dictionary<string, object?> { ["id"] = 1 }, options);
        Assert.Equal("id: 1", result);
    }

    [Fact]
    public void Serialize_ExcessiveDepth_IsRejectedBeforeStackOverflow()
    {
        var payload = new Dictionary<string, object?>
        {
            ["level1"] = new Dictionary<string, object?>
            {
                ["level2"] = new Dictionary<string, object?>
                {
                    ["level3"] = new Dictionary<string, object?>
                    {
                        ["level4"] = 1,
                    },
                },
            },
        };

        var options = new EncoderOptions
        {
            MaxDepth = 2,
        };

        var exception = Assert.Throws<ToonEncodingException>(() => GlyphTone.Encoder.Serialize(payload, options));
        Assert.Contains("Maximum object graph depth 2 exceeded", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_ExcessiveCollectionSize_IsRejected()
    {
        var options = new EncoderOptions
        {
            MaxCollectionItemCount = 2,
        };

        var exception = Assert.Throws<ToonEncodingException>(() => GlyphTone.Encoder.Serialize(ExcessiveCollectionPayload, options));
        Assert.Contains("Maximum collection item count 2 exceeded", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_ExcessiveObjectMemberCount_IsRejected()
    {
        var options = new EncoderOptions
        {
            MaxObjectMemberCount = 2,
        };

        var exception = Assert.Throws<ToonEncodingException>(() => GlyphTone.Encoder.Serialize(
            new Dictionary<string, object?>
            {
                ["alpha"] = 1,
                ["beta"] = 2,
                ["gamma"] = 3,
            },
            options));

        Assert.Contains("Maximum object member count 2 exceeded", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_InvalidCollectionLimit_IsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => GlyphTone.Encoder.Serialize(
            1,
            new EncoderOptions
            {
                MaxCollectionItemCount = 0,
            }));

        Assert.Contains("MaxCollectionItemCount", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_InvalidObjectMemberLimit_IsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => GlyphTone.Encoder.Serialize(
            1,
            new EncoderOptions
            {
                MaxObjectMemberCount = 0,
            }));

        Assert.Contains("MaxObjectMemberCount", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_ExcessiveStringLength_IsRejected()
    {
        var options = new EncoderOptions
        {
            MaxStringLength = 3,
        };

        var exception = Assert.Throws<ToonEncodingException>(() => GlyphTone.Encoder.Serialize("abcd", options));
        Assert.Contains("maximum string length 3", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_ExcessiveOutputLength_IsRejected()
    {
        var options = new EncoderOptions
        {
            MaxOutputLength = 4,
        };

        var exception = Assert.Throws<ToonEncodingException>(() => GlyphTone.Encoder.Serialize(new Dictionary<string, object?> { ["id"] = 1 }, options));
        Assert.Contains("Maximum output length 4 exceeded", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_InvalidStringLimit_IsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => GlyphTone.Encoder.Serialize(
            1,
            new EncoderOptions
            {
                MaxStringLength = 0,
            }));

        Assert.Contains("MaxStringLength", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_InvalidOutputLimit_IsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => GlyphTone.Encoder.Serialize(
            1,
            new EncoderOptions
            {
                MaxOutputLength = 0,
            }));

        Assert.Contains("MaxOutputLength", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateHardenedDefaults_ReturnsExpectedSecurityBaseline()
    {
        var options = EncoderOptions.CreateHardenedDefaults();

        Assert.False(options.AllowReflectionObjectSerialization);
        Assert.True(options.StrictMode);
        Assert.Equal(10_000, options.MaxCollectionItemCount);
        Assert.Equal(64, options.MaxDepth);
        Assert.Equal(1_024, options.MaxObjectMemberCount);
        Assert.Equal(1_000_000, options.MaxOutputLength);
        Assert.Equal(65_536, options.MaxStringLength);
    }

    [Fact]
    public void Serialize_NonStrictMode_RejectsArbitraryNewLineStrings()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => GlyphTone.Encoder.Serialize(
            new Dictionary<string, object?> { ["id"] = 1 },
            new EncoderOptions
            {
                StrictMode = false,
                NewLine = "--",
            }));

        Assert.Contains("either LF", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_NonStrictMode_RejectsIndentWithNewLines()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => GlyphTone.Encoder.Serialize(
            new Dictionary<string, object?> { ["id"] = new Dictionary<string, object?> { ["name"] = "Ada" } },
            new EncoderOptions
            {
                StrictMode = false,
                Indent = " \n ",
            }));

        Assert.Contains("cannot contain newline", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_RootMixedArray_UsesListSyntax()
    {
        var payload = new object[]
        {
            1,
            new Dictionary<string, object?> { ["id"] = 2, ["name"] = "Ada" },
            new[] { 3, 4 },
        };

        var result = GlyphTone.Encoder.Serialize(payload);

        var expected = string.Join(
            "\n",
            "[3]:",
            "  - 1",
            "  - id: 2",
            "    name: Ada",
            "  - [2]: 3,4");

        Assert.Equal(expected, result);
    }

    private static EncoderOptions CreateCamelCaseOptions()
    {
        return new EncoderOptions
        {
            AllowReflectionObjectSerialization = true,
            PropertyNamingPolicy = static name => char.ToLowerInvariant(name[0]) + name[1..],
        };
    }
}