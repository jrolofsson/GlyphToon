using System.Collections.Concurrent;
using System.Reflection;

namespace GlyphTone.Internal;

internal sealed class ToonObjectInspector
{
    private readonly ConcurrentDictionary<CacheKey, IReadOnlyList<MemberAccessor>> _memberCache = new();

    public static ToonObjectInspector Shared { get; } = new();

    public IReadOnlyList<MemberAccessor> GetMembers(Type type, bool includeFields) =>
        _memberCache.GetOrAdd(new CacheKey(type, includeFields), static key => CreateMembers(key.Type, key.IncludeFields));

    private static List<MemberAccessor> CreateMembers(Type type, bool includeFields)
    {
        var members = new List<MemberAccessor>();

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || property.GetMethod is null || property.GetMethod.IsStatic || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            members.Add(new MemberAccessor(property.Name, property));
        }

        if (includeFields)
        {
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (field.IsStatic)
                {
                    continue;
                }

                members.Add(new MemberAccessor(field.Name, field));
            }
        }

        return members;
    }

    private readonly record struct CacheKey(Type Type, bool IncludeFields);

    internal sealed class MemberAccessor
    {
        public MemberAccessor(string name, MemberInfo memberInfo)
        {
            Name = name;
            MemberInfo = memberInfo;
        }

        public string Name { get; }

        public MemberInfo MemberInfo { get; }

        public object? GetValue(object instance) => MemberInfo switch
        {
            PropertyInfo property => property.GetValue(instance),
            FieldInfo field => field.GetValue(instance),
            _ => throw new InvalidOperationException($"Unsupported member type '{MemberInfo.GetType().Name}'."),
        };
    }
}