using System.Globalization;

namespace GlyphTone;

/// <summary>
/// Serializes .NET values into Token-Oriented Object Notation (TOON).
/// </summary>
public static class Encoder
{
    /// <summary>
    /// Serializes a value into a TOON document.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">Optional serialization settings.</param>
    /// <returns>The serialized TOON text.</returns>
    public static string Serialize<T>(T value, EncoderOptions? options = null)
    {
        var effectiveOptions = PrepareOptions(options);
        var node = new Internal.ToonValueNormalizer(effectiveOptions, Internal.ToonObjectInspector.Shared).Normalize(value);

        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        new Internal.ToonDocumentWriter(writer, effectiveOptions).Write(node);
        return writer.ToString();
    }

    /// <summary>
    /// Serializes a value into TOON and writes the result to a text writer.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="options">Optional serialization settings.</param>
    public static void Serialize<T>(T value, TextWriter writer, EncoderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);

        var effectiveOptions = PrepareOptions(options);
        var node = new Internal.ToonValueNormalizer(effectiveOptions, Internal.ToonObjectInspector.Shared).Normalize(value);
        new Internal.ToonDocumentWriter(writer, effectiveOptions).Write(node);
    }

    private static EncoderOptions PrepareOptions(EncoderOptions? options)
    {
        var effectiveOptions = (options ?? new EncoderOptions()).Clone();
        effectiveOptions.Validate();
        return effectiveOptions;
    }
}