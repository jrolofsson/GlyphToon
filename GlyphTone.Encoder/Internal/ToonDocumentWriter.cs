using System.Collections.ObjectModel;

namespace GlyphTone.Internal;

internal sealed class ToonDocumentWriter
{
    private readonly TextWriter _writer;
    private readonly EncoderOptions _options;
    private int _charactersWritten;
    private bool _wroteLine;

    public ToonDocumentWriter(TextWriter writer, EncoderOptions options)
    {
        _writer = writer;
        _options = options;
    }

    public void Write(IToonValue value)
    {
        switch (value)
        {
            case ToonObjectValue objectValue:
                WriteObject(objectValue, depth: 0);
                break;
            case ToonArrayValue arrayValue:
                WriteArray(depth: 0, prefix: string.Empty, key: string.Empty, arrayValue);
                break;
            case ToonScalarValue scalarValue:
                WriteText(ToonValueFormatter.FormatScalar(scalarValue, ','));
                break;
            default:
                throw new InvalidOperationException($"Unsupported TOON value type '{value.GetType().Name}'.");
        }
    }

    private void WriteObject(ToonObjectValue objectValue, int depth)
    {
        foreach (var property in objectValue.Properties)
        {
            WriteProperty(depth, prefix: string.Empty, property);
        }
    }

    private void WriteProperty(int depth, string prefix, ToonProperty property)
    {
        var keyText = ToonValueFormatter.FormatKey(property.Name);
        switch (property.Value)
        {
            case ToonScalarValue scalarValue:
                EmitLine(depth, prefix + keyText + ": " + ToonValueFormatter.FormatScalar(scalarValue, ','));
                break;
            case ToonObjectValue objectValue:
                EmitLine(depth, prefix + keyText + ":");
                if (objectValue.Properties.Count > 0)
                {
                    WriteObject(objectValue, depth + 1);
                }

                break;
            case ToonArrayValue arrayValue:
                WriteArray(depth, prefix, keyText, arrayValue);
                break;
            default:
                throw new InvalidOperationException($"Unsupported property value type '{property.Value.GetType().Name}'.");
        }
    }

    private void WriteArray(int depth, string prefix, string key, ToonArrayValue arrayValue)
    {
        if (TryCreateInlineArray(arrayValue, out var inlineItems))
        {
            var header = BuildArrayHeader(key, arrayValue.Items.Count);
            if (inlineItems.Count == 0)
            {
                EmitLine(depth, prefix + header + ":");
                return;
            }

            EmitLine(depth, prefix + header + ": " + string.Join(',', inlineItems));
            return;
        }

        if (TryCreateTabularShape(arrayValue, out var shape))
        {
            WriteTabularArray(depth, prefix, key, arrayValue, shape);
            return;
        }

        EmitLine(depth, prefix + BuildArrayHeader(key, arrayValue.Items.Count) + ":");
        foreach (var item in arrayValue.Items)
        {
            WriteListItem(depth + 1, item);
        }
    }

    private void WriteListItem(int depth, IToonValue item)
    {
        switch (item)
        {
            case ToonScalarValue scalarValue:
                EmitLine(depth, "- " + ToonValueFormatter.FormatScalar(scalarValue, ','));
                break;
            case ToonArrayValue arrayValue:
                WriteArray(depth, "- ", string.Empty, arrayValue);
                break;
            case ToonObjectValue objectValue:
                WriteObjectListItem(depth, objectValue);
                break;
            default:
                throw new InvalidOperationException($"Unsupported list item type '{item.GetType().Name}'.");
        }
    }

    private void WriteObjectListItem(int depth, ToonObjectValue objectValue)
    {
        if (objectValue.Properties.Count == 0)
        {
            EmitLine(depth, "-");
            return;
        }

        var firstProperty = objectValue.Properties[0];
        if (firstProperty.Value is ToonArrayValue firstArray && TryCreateTabularShape(firstArray, out var shape))
        {
            WriteTabularArray(depth, "- ", ToonValueFormatter.FormatKey(firstProperty.Name), firstArray, shape);
            for (var index = 1; index < objectValue.Properties.Count; index++)
            {
                WriteProperty(depth + 1, string.Empty, objectValue.Properties[index]);
            }

            return;
        }

        WriteProperty(depth, "- ", firstProperty);
        for (var index = 1; index < objectValue.Properties.Count; index++)
        {
            WriteProperty(depth + 1, string.Empty, objectValue.Properties[index]);
        }
    }

    private void WriteTabularArray(int depth, string prefix, string key, ToonArrayValue arrayValue, TabularShape shape)
    {
        var header = BuildArrayHeader(key, arrayValue.Items.Count) + "{" + string.Join(',', shape.Columns.Select(ToonValueFormatter.FormatKey)) + "}:";
        EmitLine(depth, prefix + header);

        foreach (var row in shape.Rows)
        {
            EmitLine(depth + 1, string.Join(',', row.Select(static value => ToonValueFormatter.FormatScalar(value, ','))));
        }
    }

    private static bool TryCreateInlineArray(ToonArrayValue arrayValue, out IReadOnlyList<string> items)
    {
        var values = new List<string>(arrayValue.Items.Count);
        foreach (var item in arrayValue.Items)
        {
            if (item is not ToonScalarValue scalarValue)
            {
                items = Array.Empty<string>();
                return false;
            }

            values.Add(ToonValueFormatter.FormatScalar(scalarValue, ','));
        }

        items = values;
        return true;
    }

    private bool TryCreateTabularShape(ToonArrayValue arrayValue, out TabularShape shape)
    {
        if (!_options.PreferTabularArrays || arrayValue.Items.Count == 0)
        {
            shape = TabularShape.Empty;
            return false;
        }

        if (arrayValue.Items[0] is not ToonObjectValue firstRow)
        {
            shape = TabularShape.Empty;
            return false;
        }

        return TryCreateTabularShape(firstRow, arrayValue, out shape);
    }

    private static bool TryCreateTabularShape(ToonObjectValue firstRow, ToonArrayValue arrayValue, out TabularShape shape)
    {
        if (firstRow.Properties.Count == 0)
        {
            shape = TabularShape.Empty;
            return false;
        }

        var columns = firstRow.Properties.Select(static property => property.Name).ToArray();
        var rows = new List<IReadOnlyList<ToonScalarValue>>(arrayValue.Items.Count);

        foreach (var item in arrayValue.Items)
        {
            if (!TryMapRow(columns, item, out var orderedRow))
            {
                shape = TabularShape.Empty;
                return false;
            }

            rows.Add(orderedRow);
        }

        shape = new TabularShape(columns, rows);
        return true;
    }

    private static bool TryMapRow(string[] columns, IToonValue item, out ToonScalarValue[] orderedRow)
    {
        orderedRow = [];
        if (item is not ToonObjectValue objectValue || objectValue.Properties.Count != columns.Length)
        {
            return false;
        }

        var rowMap = new Dictionary<string, ToonScalarValue>(StringComparer.Ordinal);
        foreach (var property in objectValue.Properties)
        {
            if (property.Value is not ToonScalarValue scalarValue)
            {
                return false;
            }

            rowMap[property.Name] = scalarValue;
        }

        orderedRow = new ToonScalarValue[columns.Length];
        for (var index = 0; index < columns.Length; index++)
        {
            if (!rowMap.TryGetValue(columns[index], out var scalarValue))
            {
                return false;
            }

            orderedRow[index] = scalarValue;
        }

        return true;
    }

    private void EmitLine(int depth, string content)
    {
        if (_wroteLine)
        {
            WriteText(_options.NewLine);
        }

        for (var index = 0; index < depth; index++)
        {
            WriteText(_options.Indent);
        }

        WriteText(content);
        _wroteLine = true;
    }

    private void WriteText(string text)
    {
        EnsureOutputLength(text.Length);
        _writer.Write(text);
        _charactersWritten += text.Length;
    }

    private void EnsureOutputLength(int additionalLength)
    {
        if (additionalLength < 0)
        {
            throw new InvalidOperationException("Additional output length cannot be negative.");
        }

        if (_charactersWritten > _options.MaxOutputLength - additionalLength)
        {
            throw new ToonEncodingException($"Maximum output length {_options.MaxOutputLength} exceeded while writing TOON text.");
        }
    }

    private static string BuildArrayHeader(string key, int count)
    {
        return key.Length == 0 ? $"[{count}]" : $"{key}[{count}]";
    }

    private sealed class TabularShape
    {
        public static TabularShape Empty { get; } = new([], []);

        public TabularShape(IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<ToonScalarValue>> rows)
        {
            Columns = columns;
            Rows = rows;
        }

        public IReadOnlyList<string> Columns { get; }

        public IReadOnlyList<IReadOnlyList<ToonScalarValue>> Rows { get; }
    }
}