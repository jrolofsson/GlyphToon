namespace GlyphTone;

/// <summary>
/// Configures TOON serialization behavior.
/// </summary>
public sealed class EncoderOptions
{
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

    internal EncoderOptions Clone() => new()
    {
        IgnoreNullValues = IgnoreNullValues,
        IncludeFields = IncludeFields,
        Indent = Indent,
        NewLine = NewLine,
        PreferTabularArrays = PreferTabularArrays,
        PropertyNamingPolicy = PropertyNamingPolicy,
        SortDictionaryKeys = SortDictionaryKeys,
        SortProperties = SortProperties,
        StrictMode = StrictMode,
    };

    internal void Validate()
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

        if (!StrictMode)
        {
            return;
        }

        if (NewLine != "\n")
        {
            throw new InvalidOperationException("Strict mode requires LF (\\n) line endings.");
        }

        if (Indent.Length == 0)
        {
            throw new InvalidOperationException("Strict mode requires a non-empty indentation string.");
        }

        if (Indent.Any(static character => character != ' '))
        {
            throw new InvalidOperationException("Strict mode requires indentation to consist of spaces only.");
        }
    }
}