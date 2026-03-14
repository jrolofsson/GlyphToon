using System.Collections;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GlyphTone.Internal;

internal sealed class ToonValueNormalizer
{
    private readonly EncoderOptions _options;
    private readonly ToonObjectInspector _objectInspector;
    private readonly HashSet<object> _activeReferences = new(ReferenceEqualityComparer.Instance);

    public ToonValueNormalizer(EncoderOptions options, ToonObjectInspector objectInspector)
    {
        _options = options;
        _objectInspector = objectInspector;
    }

    public IToonValue Normalize(object? value) => NormalizeCore(value, "$", 0);

    private IToonValue NormalizeCore(object? value, string path, int depth)
    {
        EnsureDepth(depth, path);

        if (value is null)
        {
            return ToonNullValue.Instance;
        }

        if (TryNormalizeIntrinsic(value, path, depth, out var intrinsicValue))
        {
            return intrinsicValue;
        }

        if (value is Delegate)
        {
            throw new ToonEncodingException($"Unsupported type at path '{path}': delegates cannot be encoded as TOON.");
        }

        if (value is Type)
        {
            throw new ToonEncodingException($"Unsupported type at path '{path}': System.Type instances cannot be encoded as TOON.");
        }

        if (TryNormalizeDictionary(value, path, depth + 1, out var dictionaryValue))
        {
            return dictionaryValue;
        }

        if (value is IEnumerable enumerable)
        {
            return NormalizeEnumerable(enumerable, value, path, depth + 1);
        }

        if (!_options.AllowReflectionObjectSerialization)
        {
            throw new ToonEncodingException($"Reflection-based object serialization is disabled at path '{path}'. Use dictionaries/arrays/primitives or enable AllowReflectionObjectSerialization.");
        }

        return NormalizeObject(value, path, depth + 1);
    }

    private bool TryNormalizeIntrinsic(object value, string path, int depth, out IToonValue normalizedValue)
    {
        switch (value)
        {
            case string stringValue:
                EnsureTextLength(stringValue, path, isKey: false);
                EnsureEncodableText(stringValue, path, isKey: false);
                normalizedValue = new ToonStringValue(stringValue);
                return true;
            case char character:
                var singleCharacter = character.ToString();
                EnsureTextLength(singleCharacter, path, isKey: false);
                EnsureEncodableText(singleCharacter, path, isKey: false);
                normalizedValue = new ToonStringValue(singleCharacter);
                return true;
            case bool booleanValue:
                normalizedValue = new ToonBooleanValue(booleanValue);
                return true;
            case DateTime dateTime:
                normalizedValue = NormalizeCore(dateTime.ToString("O", CultureInfo.InvariantCulture), path, depth);
                return true;
            case DateTimeOffset dateTimeOffset:
                normalizedValue = NormalizeCore(dateTimeOffset.ToString("O", CultureInfo.InvariantCulture), path, depth);
                return true;
            case Guid guid:
                normalizedValue = NormalizeCore(guid.ToString("D", CultureInfo.InvariantCulture), path, depth);
                return true;
            case Uri uri:
                normalizedValue = NormalizeCore(uri.OriginalString, path, depth);
                return true;
            case Enum enumValue:
                var enumText = enumValue.ToString();
                EnsureTextLength(enumText, path, isKey: false);
                EnsureEncodableText(enumText, path, isKey: false);
                normalizedValue = new ToonStringValue(enumText);
                return true;
            default:
                return TryNormalizeNumber(value, out normalizedValue);
        }
    }

    private ToonArrayValue NormalizeEnumerable(IEnumerable enumerable, object originalValue, string path, int depth)
    {
        return WithReferenceTracking(originalValue, path, static (normalizer, state) =>
        {
            var items = new List<IToonValue>();
            var index = 0;
            foreach (var item in state.Enumerable)
            {
                normalizer.EnsureCollectionItemCount(index + 1, state.Path);
                items.Add(normalizer.NormalizeCore(item, $"{state.Path}[{index}]", state.Depth));
                index++;
            }

            return new ToonArrayValue(items);
        }, (Enumerable: enumerable, Path: path, Depth: depth));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Uses serializer instance state through tracked callbacks.")]
    private ToonObjectValue NormalizeObject(object value, string path, int depth)
    {
        return WithReferenceTracking(value, path, static (normalizer, state) =>
        {
            var members = normalizer._objectInspector.GetMembers(state.Value.GetType(), normalizer._options.IncludeFields);
            normalizer.EnsureObjectMemberCount(members.Count, state.Path);
            var properties = new List<ToonProperty>(members.Count);
            var seenNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var member in members)
            {
                var memberValue = ReadMemberValue(state.Value, state.Path, member);
                var propertyPath = BuildObjectPath(state.Path, member.Name);
                var normalizedValue = normalizer.NormalizeCore(memberValue, propertyPath, state.Depth);
                if (normalizer._options.IgnoreNullValues && normalizedValue is ToonNullValue)
                {
                    continue;
                }

                var effectiveName = normalizer.ApplyPropertyName(member.Name, propertyPath);
                if (!seenNames.Add(effectiveName))
                {
                    throw new ToonEncodingException($"Duplicate property name '{effectiveName}' encountered at path '{state.Path}' after applying naming policy.");
                }

                properties.Add(new ToonProperty(effectiveName, normalizedValue));
            }

            if (normalizer._options.SortProperties)
            {
                properties.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));
            }

            return new ToonObjectValue(properties);
        }, (Value: value, Path: path, Depth: depth));
    }

    private static object? ReadMemberValue(object instance, string path, ToonObjectInspector.MemberAccessor member)
    {
        try
        {
            return member.GetValue(instance);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw new ToonEncodingException($"Failed to read member '{member.Name}' at path '{path}'.", exception.InnerException);
        }
        catch (Exception exception)
        {
            throw new ToonEncodingException($"Failed to read member '{member.Name}' at path '{path}'.", exception);
        }
    }

    private static bool TryNormalizeNumber(object value, out IToonValue normalizedValue)
    {
        switch (value)
        {
            case byte or sbyte or short or ushort or int or uint or long or ulong or Int128 or UInt128 or decimal or float or double or Half or BigInteger:
                normalizedValue = ToonNumberFormatter.FormatNumber(value);
                return true;
            default:
                normalizedValue = null!;
                return false;
        }
    }

    private bool TryNormalizeDictionary(object value, string path, int depth, out IToonValue normalizedValue)
    {
        if (value is IDictionary dictionary)
        {
            normalizedValue = WithReferenceTracking(value, path, static (normalizer, state) =>
            {
                var entries = new List<KeyValuePair<string, object?>>();
                foreach (DictionaryEntry entry in state.Dictionary)
                {
                    entries.Add(new KeyValuePair<string, object?>(ConvertDictionaryKey(entry.Key, state.Path), entry.Value));
                }

                return normalizer.NormalizeDictionaryEntries(entries, state.Path, state.Depth);
            }, (Dictionary: dictionary, Path: path, Depth: depth));
            return true;
        }

        if (!ImplementsDictionaryInterface(value.GetType()))
        {
            normalizedValue = null!;
            return false;
        }

        normalizedValue = WithReferenceTracking(value, path, static (normalizer, state) =>
        {
            var entries = new List<KeyValuePair<string, object?>>();
            foreach (var entry in (IEnumerable)state.Value)
            {
                if (entry is null)
                {
                    continue;
                }

                var entryType = entry.GetType();
                var keyProperty = entryType.GetProperty("Key", BindingFlags.Instance | BindingFlags.Public);
                var valueProperty = entryType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
                if (keyProperty is null || valueProperty is null)
                {
                    throw new ToonEncodingException($"Unsupported dictionary entry type '{entryType.FullName}' at path '{state.Path}'.");
                }

                var key = keyProperty.GetValue(entry);
                var dictionaryValue = valueProperty.GetValue(entry);
                entries.Add(new KeyValuePair<string, object?>(ConvertDictionaryKey(key, state.Path), dictionaryValue));
            }

            return normalizer.NormalizeDictionaryEntries(entries, state.Path, state.Depth);
        }, (Value: value, Path: path, Depth: depth));
        return true;
    }

    private ToonObjectValue NormalizeDictionaryEntries(List<KeyValuePair<string, object?>> entries, string path, int depth)
    {
        if (_options.SortDictionaryKeys)
        {
            entries.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Key, right.Key));
        }

        EnsureObjectMemberCount(entries.Count, path);
        var properties = new List<ToonProperty>(entries.Count);
        var seenNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            var keyPath = BuildObjectPath(path, entry.Key);
            EnsureTextLength(entry.Key, keyPath, isKey: true);
            EnsureEncodableText(entry.Key, keyPath, isKey: true);

            if (!seenNames.Add(entry.Key))
            {
                throw new ToonEncodingException($"Duplicate dictionary key '{entry.Key}' encountered at path '{path}'.");
            }

            var normalizedValue = NormalizeCore(entry.Value, keyPath, depth);
            if (_options.IgnoreNullValues && normalizedValue is ToonNullValue)
            {
                continue;
            }

            properties.Add(new ToonProperty(entry.Key, normalizedValue));
        }

        return new ToonObjectValue(properties);
    }

    private string ApplyPropertyName(string name, string path)
    {
        if (_options.PropertyNamingPolicy is null)
        {
            EnsureTextLength(name, path, isKey: true);
            EnsureEncodableText(name, path, isKey: true);
            return name;
        }

        var transformed = _options.PropertyNamingPolicy(name);
        if (transformed is null)
        {
            throw new ToonEncodingException($"Property naming policy returned null for member '{name}' at path '{path}'.");
        }

        EnsureTextLength(transformed, path, isKey: true);
        EnsureEncodableText(transformed, path, isKey: true);
        return transformed;
    }

    private static string ConvertDictionaryKey(object? key, string path)
    {
        if (key is null)
        {
            throw new ToonEncodingException($"Dictionary at path '{path}' contains a null key, which TOON does not support.");
        }

        return key switch
        {
            string stringKey => stringKey,
            char character => character.ToString(),
            Guid guid => guid.ToString("D", CultureInfo.InvariantCulture),
            Uri uri => uri.OriginalString,
            Enum enumValue => enumValue.ToString(),
            byte or sbyte or short or ushort or int or uint or long or ulong or Int128 or UInt128 or decimal or float or double or Half or BigInteger or bool => Convert.ToString(key, CultureInfo.InvariantCulture)!,
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            _ => throw new ToonEncodingException($"Unsupported dictionary key type '{key.GetType().FullName}' at path '{path}'. Keys must be string-compatible.")
        };
    }

    private static bool ImplementsDictionaryInterface(Type type)
    {
        foreach (var interfaceType in type.GetInterfaces())
        {
            if (!interfaceType.IsGenericType)
            {
                continue;
            }

            var genericDefinition = interfaceType.GetGenericTypeDefinition();
            if (genericDefinition == typeof(IDictionary<,>) || genericDefinition == typeof(IReadOnlyDictionary<,>))
            {
                return true;
            }
        }

        return false;
    }

    private static string BuildObjectPath(string parentPath, string childName)
    {
        return parentPath == "$"
            ? "$." + childName
            : parentPath + "." + childName;
    }

    private static void EnsureEncodableText(string value, string path, bool isKey)
    {
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (character < 0x20 && character is not '\n' and not '\r' and not '\t')
            {
                var label = isKey ? "Key" : "String";
                throw new ToonEncodingException($"{label} at path '{path}' contains unsupported control character U+{(int)character:X4}.");
            }
        }
    }

    private void EnsureTextLength(string value, string path, bool isKey)
    {
        if (value.Length <= _options.MaxStringLength)
        {
            return;
        }

        var label = isKey ? "Key" : "String";
        throw new ToonEncodingException($"{label} at path '{path}' exceeds the configured maximum string length {_options.MaxStringLength}.");
    }

    private void EnsureDepth(int depth, string path)
    {
        if (depth > _options.MaxDepth)
        {
            throw new ToonEncodingException($"Maximum object graph depth {_options.MaxDepth} exceeded at path '{path}'.");
        }
    }

    private void EnsureCollectionItemCount(int count, string path)
    {
        if (count > _options.MaxCollectionItemCount)
        {
            throw new ToonEncodingException($"Maximum collection item count {_options.MaxCollectionItemCount} exceeded at path '{path}'.");
        }
    }

    private void EnsureObjectMemberCount(int count, string path)
    {
        if (count > _options.MaxObjectMemberCount)
        {
            throw new ToonEncodingException($"Maximum object member count {_options.MaxObjectMemberCount} exceeded at path '{path}'.");
        }
    }

    private TResult WithReferenceTracking<TState, TResult>(object instance, string path, Func<ToonValueNormalizer, TState, TResult> action, TState state)
    {
        if (instance.GetType().IsValueType)
        {
            return action(this, state);
        }

        if (!_activeReferences.Add(instance))
        {
            throw new ToonEncodingException($"Circular reference detected while encoding path '{path}'.");
        }

        try
        {
            return action(this, state);
        }
        finally
        {
            _activeReferences.Remove(instance);
        }
    }
}