namespace GlyphTone;

/// <summary>
/// Configures TOON serialization behavior.
/// </summary>
public sealed class EncoderOptions
{
    /// <summary>
    /// Gets or sets the maximum number of characters allowed in any serialized key or string value.
    /// </summary>
    public int MaxStringLength { get; set; } = 1_000_000;

    /// <summary>
    /// Gets or sets the maximum number of characters the encoder may write for a single TOON document.
    /// </summary>
    public int MaxOutputLength { get; set; } = 10_000_000;

    /// <summary>
    /// Gets or sets the maximum number of items allowed in any serialized array or enumerable.
    /// </summary>
    public int MaxCollectionItemCount { get; set; } = 100_000;

    /// <summary>
    /// Gets or sets the maximum nesting depth allowed while normalizing the input object graph.
    /// </summary>
    public int MaxDepth { get; set; } = 128;

    /// <summary>
    /// Gets or sets the maximum number of members allowed in any serialized object.
    /// </summary>
    public int MaxObjectMemberCount { get; set; } = 4_096;

    /// <summary>
    /// Gets or sets the indentation unit used for nested objects and arrays.
    /// </summary>
    public string Indent { get; set; } = "  ";

    /// <summary>
    /// Gets or sets the newline sequence written between TOON lines.
    /// </summary>
    public string NewLine { get; set; } = "\n";

    /// <summary>
    /// Gets or sets an optional property naming policy applied to reflected properties and fields.
    /// </summary>
    public Func<string, string>? PropertyNamingPolicy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether null-valued object members should be omitted.
    /// </summary>
    public bool IgnoreNullValues { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether reflected object members should be sorted by effective name.
    /// </summary>
    public bool SortProperties { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether dictionary keys should be sorted ordinally before encoding.
    /// </summary>
    public bool SortDictionaryKeys { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether homogeneous arrays of objects should prefer tabular TOON output.
    /// </summary>
    public bool PreferTabularArrays { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether strict TOON validation rules should be enforced for emitted text.
    /// </summary>
    public bool StrictMode { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether public fields should be serialized in addition to public readable properties.
    /// </summary>
    public bool IncludeFields { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether reflection-based POCO serialization is allowed.
    /// </summary>
    public bool AllowReflectionObjectSerialization { get; set; } = true;

    /// <summary>
    /// Creates a conservative option set for hostile or multi-tenant environments.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This preset is intended for callers that serialize data influenced by users, plugins, or external systems.
    /// It disables reflection-based POCO serialization and applies conservative limits for nesting depth, collection
    /// size, object width, string size, and total output size.
    /// </para>
    /// <para>
    /// Use this as a starting point, then tune the limits for your workload and deployment budget.
    /// </para>
    /// </remarks>
    /// <returns>A new <see cref="EncoderOptions"/> instance configured with hardened defaults.</returns>
    public static EncoderOptions CreateHardenedDefaults() => new()
    {
        AllowReflectionObjectSerialization = false,
        MaxCollectionItemCount = 10_000,
        MaxDepth = 64,
        MaxObjectMemberCount = 1_024,
        MaxOutputLength = 1_000_000,
        MaxStringLength = 65_536,
        StrictMode = true,
    };

    internal EncoderOptions Clone() => new()
    {
        AllowReflectionObjectSerialization = AllowReflectionObjectSerialization,
        IgnoreNullValues = IgnoreNullValues,
        IncludeFields = IncludeFields,
        Indent = Indent,
        MaxCollectionItemCount = MaxCollectionItemCount,
        MaxDepth = MaxDepth,
        MaxObjectMemberCount = MaxObjectMemberCount,
        MaxOutputLength = MaxOutputLength,
        MaxStringLength = MaxStringLength,
        NewLine = NewLine,
        PreferTabularArrays = PreferTabularArrays,
        PropertyNamingPolicy = PropertyNamingPolicy,
        SortDictionaryKeys = SortDictionaryKeys,
        SortProperties = SortProperties,
        StrictMode = StrictMode,
    };

    internal void Validate()
    {
        ValidateNumericLimits();
        ValidateBaseFormatting();

        if (!StrictMode)
        {
            return;
        }

        if (NewLine != "\n")
        {
            throw new InvalidOperationException("Strict mode requires LF (\\n) line endings.");
        }

        if (Indent.Any(static character => character != ' '))
        {
            throw new InvalidOperationException("Strict mode requires indentation to consist of spaces only.");
        }
    }

    private void ValidateNumericLimits()
    {
        if (MaxOutputLength < 1)
        {
            throw new InvalidOperationException("EncoderOptions.MaxOutputLength must be greater than zero.");
        }

        if (MaxStringLength < 1)
        {
            throw new InvalidOperationException("EncoderOptions.MaxStringLength must be greater than zero.");
        }

        if (MaxCollectionItemCount < 1)
        {
            throw new InvalidOperationException("EncoderOptions.MaxCollectionItemCount must be greater than zero.");
        }

        if (MaxDepth < 1)
        {
            throw new InvalidOperationException("EncoderOptions.MaxDepth must be greater than zero.");
        }

        if (MaxObjectMemberCount < 1)
        {
            throw new InvalidOperationException("EncoderOptions.MaxObjectMemberCount must be greater than zero.");
        }
    }

    private void ValidateBaseFormatting()
    {
        if (Indent is null)
        {
            throw new InvalidOperationException("EncoderOptions.Indent cannot be null.");
        }

        if (NewLine is null)
        {
            throw new InvalidOperationException("EncoderOptions.NewLine cannot be null.");
        }

        if (NewLine.Length == 0)
        {
            throw new InvalidOperationException("EncoderOptions.NewLine cannot be empty.");
        }

        if (Indent.Length == 0)
        {
            throw new InvalidOperationException("EncoderOptions.Indent cannot be empty.");
        }

        if (NewLine is not "\n" and not "\r\n")
        {
            throw new InvalidOperationException("EncoderOptions.NewLine must be either LF (\\n) or CRLF (\\r\\n).");
        }

        if (Indent.Any(static character => character is '\r' or '\n'))
        {
            throw new InvalidOperationException("EncoderOptions.Indent cannot contain newline characters.");
        }

        if (Indent.Any(static character => character != ' ' && character != '\t'))
        {
            throw new InvalidOperationException("EncoderOptions.Indent must consist only of spaces or tabs.");
        }
    }
}