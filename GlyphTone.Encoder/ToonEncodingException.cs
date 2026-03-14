namespace GlyphTone;

/// <summary>
/// Represents an error encountered while preparing or emitting TOON text.
/// </summary>
public sealed class ToonEncodingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToonEncodingException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ToonEncodingException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToonEncodingException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The underlying exception.</param>
    public ToonEncodingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}