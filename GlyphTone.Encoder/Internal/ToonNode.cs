namespace GlyphTone.Internal;

internal interface IToonValue
{
}

internal abstract class ToonScalarValue : IToonValue;

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

internal sealed class ToonArrayValue : IToonValue
{
    public ToonArrayValue(IReadOnlyList<IToonValue> items)
    {
        Items = items;
    }

    public IReadOnlyList<IToonValue> Items { get; }
}

internal sealed class ToonObjectValue : IToonValue
{
    public ToonObjectValue(IReadOnlyList<ToonProperty> properties)
    {
        Properties = properties;
    }

    public IReadOnlyList<ToonProperty> Properties { get; }
}

internal sealed class ToonProperty
{
    public ToonProperty(string name, IToonValue value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public IToonValue Value { get; }
}