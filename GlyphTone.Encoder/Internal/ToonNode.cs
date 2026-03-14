namespace GlyphTone.Internal;

internal abstract class ToonValue
{
}

internal abstract class ToonScalarValue : ToonValue;

internal sealed class ToonNullValue : ToonScalarValue
{
    private ToonNullValue()
    {
    }

    public static ToonNullValue Instance { get; } = new();
}

internal sealed class ToonBooleanValue : ToonScalarValue
{
    public ToonBooleanValue(bool value)
    {
        Value = value;
    }

    public bool Value { get; }
}

internal sealed class ToonNumberValue : ToonScalarValue
{
    public ToonNumberValue(string text)
    {
        Text = text;
    }

    public string Text { get; }
}

internal sealed class ToonStringValue : ToonScalarValue
{
    public ToonStringValue(string value)
    {
        Value = value;
    }

    public string Value { get; }
}

internal sealed class ToonArrayValue : ToonValue
{
    public ToonArrayValue(IReadOnlyList<ToonValue> items)
    {
        Items = items;
    }

    public IReadOnlyList<ToonValue> Items { get; }
}

internal sealed class ToonObjectValue : ToonValue
{
    public ToonObjectValue(IReadOnlyList<ToonProperty> properties)
    {
        Properties = properties;
    }

    public IReadOnlyList<ToonProperty> Properties { get; }
}

internal sealed class ToonProperty
{
    public ToonProperty(string name, ToonValue value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public ToonValue Value { get; }
}