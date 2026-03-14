using System.Globalization;

namespace GlyphTone.Internal;

internal static class ToonValueFormatter
{
    public static string FormatKey(string key)
    {
        return IsSafeUnquotedKey(key) ? key : Quote(key);
    }

    public static string FormatScalar(ToonScalarValue value, char activeDelimiter)
    {
        return value switch
        {
            ToonNullValue => "null",
            ToonBooleanValue booleanValue => booleanValue.Value ? "true" : "false",
            ToonNumberValue numberValue => numberValue.Text,
            ToonStringValue stringValue => FormatString(stringValue.Value, activeDelimiter),
            _ => throw new InvalidOperationException($"Unsupported scalar type '{value.GetType().Name}'."),
        };
    }

    private static string FormatString(string value, char activeDelimiter)
    {
        return NeedsQuotedString(value, activeDelimiter) ? Quote(value) : value;
    }

    private static bool NeedsQuotedString(string value, char activeDelimiter)
    {
        if (value.Length == 0)
        {
            return true;
        }

        if (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1]))
        {
            return true;
        }

        if (value is "true" or "false" or "null" or "NaN" or "Infinity" or "+Infinity")
        {
            return true;
        }

        if (value[0] == '-')
        {
            return true;
        }

        if (LooksNumeric(value))
        {
            return true;
        }

        foreach (var character in value)
        {
            if (character == activeDelimiter || character is ':' or '"' or '\\' or '[' or ']' or '{' or '}')
            {
                return true;
            }

            if (character < 0x20)
            {
                return true;
            }
        }

        return false;
    }

    private static bool LooksNumeric(string value)
    {
        if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _))
        {
            return true;
        }

        if (value.Length > 1 && value[0] == '0' && char.IsDigit(value[1]))
        {
            return true;
        }

        return value.Length > 2 && value[0] == '-' && value[1] == '0' && char.IsDigit(value[2]);
    }

    private static bool IsSafeUnquotedKey(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        if (!(char.IsLetter(value[0]) || value[0] == '_'))
        {
            return false;
        }

        foreach (var character in value)
        {
            if (!(char.IsLetterOrDigit(character) || character is '_' or '.'))
            {
                return false;
            }
        }

        return true;
    }

    private static string Quote(string value)
    {
        var builder = new System.Text.StringBuilder(value.Length + 2);
        builder.Append('"');

        foreach (var character in value)
        {
            builder.Append(character switch
            {
                '\\' => "\\\\",
                '"' => "\\\"",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                _ => character.ToString(),
            });
        }

        builder.Append('"');
        return builder.ToString();
    }
}