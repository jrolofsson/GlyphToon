using System.Globalization;
using System.Numerics;

namespace GlyphTone.Internal;

internal static class ToonNumberFormatter
{
    private static readonly BigInteger MaxInt128 = (BigInteger)Int128.MaxValue;
    private static readonly BigInteger MinInt128 = (BigInteger)Int128.MinValue;

    public static ToonValue FormatNumber(object value)
    {
        return value switch
        {
            byte byteValue => new ToonNumberValue(byteValue.ToString(CultureInfo.InvariantCulture)),
            sbyte sbyteValue => new ToonNumberValue(sbyteValue.ToString(CultureInfo.InvariantCulture)),
            short shortValue => new ToonNumberValue(shortValue.ToString(CultureInfo.InvariantCulture)),
            ushort ushortValue => new ToonNumberValue(ushortValue.ToString(CultureInfo.InvariantCulture)),
            int intValue => new ToonNumberValue(intValue.ToString(CultureInfo.InvariantCulture)),
            uint uintValue => new ToonNumberValue(uintValue.ToString(CultureInfo.InvariantCulture)),
            long longValue => new ToonNumberValue(longValue.ToString(CultureInfo.InvariantCulture)),
            ulong ulongValue => new ToonNumberValue(ulongValue.ToString(CultureInfo.InvariantCulture)),
            Int128 int128Value => new ToonNumberValue(int128Value.ToString(CultureInfo.InvariantCulture)),
            UInt128 uint128Value => new ToonNumberValue(uint128Value.ToString(CultureInfo.InvariantCulture)),
            decimal decimalValue => new ToonNumberValue(CanonicalizeNumericText(decimalValue.ToString(CultureInfo.InvariantCulture))),
            float floatValue => FormatFloat(floatValue),
            double doubleValue => FormatDouble(doubleValue),
            Half halfValue => FormatDouble((double)halfValue),
            BigInteger bigIntegerValue => FormatBigInteger(bigIntegerValue),
            _ => throw new InvalidOperationException($"Unsupported number type '{value.GetType().FullName}'."),
        };
    }

    private static ToonValue FormatFloat(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            return ToonNullValue.Instance;
        }

        return new ToonNumberValue(CanonicalizeNumericText(value.ToString("R", CultureInfo.InvariantCulture)));
    }

    private static ToonValue FormatDouble(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return ToonNullValue.Instance;
        }

        return new ToonNumberValue(CanonicalizeNumericText(value.ToString("R", CultureInfo.InvariantCulture)));
    }

    private static ToonValue FormatBigInteger(BigInteger value)
    {
        if (value >= MinInt128 && value <= MaxInt128)
        {
            return new ToonNumberValue(value.ToString(CultureInfo.InvariantCulture));
        }

        return new ToonStringValue(value.ToString(CultureInfo.InvariantCulture));
    }

    private static string CanonicalizeNumericText(string text)
    {
        var (sign, significand, exponent) = ParseComponents(text);
        if (significand.All(static character => character == '0'))
        {
            return "0";
        }

        var expanded = ExpandScientificNotation(significand, exponent);
        return ApplyCanonicalTrimming(sign, expanded);
    }

    private static (string Sign, string Significand, int Exponent) ParseComponents(string text)
    {
        if (string.Equals(text, "-0", StringComparison.Ordinal) || string.Equals(text, "+0", StringComparison.Ordinal))
        {
            return (string.Empty, "0", 0);
        }

        var sign = string.Empty;
        if (text[0] is '+' or '-')
        {
            sign = text[0] == '-' ? "-" : string.Empty;
            text = text[1..];
        }

        var exponentIndex = text.IndexOfAny(['e', 'E']);
        var exponent = 0;
        if (exponentIndex >= 0)
        {
            exponent = int.Parse(text[(exponentIndex + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            text = text[..exponentIndex];
        }

        return (sign, text, exponent);
    }

    private static string ExpandScientificNotation(string significand, int exponent)
    {
        var separatorIndex = significand.IndexOf('.');
        var digitsBeforeSeparator = separatorIndex >= 0 ? separatorIndex : significand.Length;
        var digits = separatorIndex >= 0 ? string.Concat(significand.AsSpan(0, separatorIndex), significand.AsSpan(separatorIndex + 1)) : significand;
        var decimalPosition = digitsBeforeSeparator + exponent;

        if (decimalPosition <= 0)
        {
            return "0." + new string('0', -decimalPosition) + digits;
        }

        if (decimalPosition >= digits.Length)
        {
            return digits + new string('0', decimalPosition - digits.Length);
        }

        return digits[..decimalPosition] + "." + digits[decimalPosition..];
    }

    private static string ApplyCanonicalTrimming(string sign, string expanded)
    {
        string integerPart;
        string fractionalPart;

        var separatorIndex = expanded.IndexOf('.');
        if (separatorIndex >= 0)
        {
            integerPart = expanded[..separatorIndex];
            fractionalPart = expanded[(separatorIndex + 1)..];
        }
        else
        {
            integerPart = expanded;
            fractionalPart = string.Empty;
        }

        integerPart = integerPart.TrimStart('0');
        if (integerPart.Length == 0)
        {
            integerPart = "0";
        }

        fractionalPart = fractionalPart.TrimEnd('0');

        var result = fractionalPart.Length == 0
            ? integerPart
            : integerPart + "." + fractionalPart;

        return result == "0" ? "0" : sign + result;
    }
}