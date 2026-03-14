# Security Policy

## Scope

`GlyphTone.Encoder` is a serialization library. Its main security concerns are denial-of-service risks from hostile object graphs, ambiguous output caused by unsafe formatting options, and side effects triggered by reflection-based property access.

This project does not currently implement a decoder, network service, or sandbox. Callers remain responsible for deciding which objects are safe to serialize and for applying appropriate process-level memory and CPU limits in hostile environments.

## Supported Hardening Controls

The encoder includes the following controls for safer operation:

- `MaxDepth`: stops excessively deep graphs before they can trigger stack overflow.
- `MaxCollectionItemCount`: stops unbounded arrays and enumerables from consuming unbounded CPU and memory.
- `MaxObjectMemberCount`: stops excessively wide objects and dictionaries from consuming unbounded CPU and memory.
- `MaxStringLength`: stops oversized keys and string values from consuming unbounded memory.
- `MaxOutputLength`: stops a single serialization from emitting unbounded output.
- `AllowReflectionObjectSerialization`: disables reflection-based POCO serialization when callers want to accept only primitives, arrays, and dictionaries.
- `StrictMode`: constrains newline and indentation formatting to spec-aligned output.

Recommended hardened baseline:

```csharp
var options = new EncoderOptions
{
    StrictMode = true,
    AllowReflectionObjectSerialization = false,
    MaxDepth = 64,
    MaxCollectionItemCount = 10_000,
    MaxObjectMemberCount = 1_024,
    MaxStringLength = 65_536,
    MaxOutputLength = 1_000_000,
};
```

Tune those limits to match your workload and memory budget.

Equivalent shortcut:

```csharp
var options = EncoderOptions.CreateHardenedDefaults();
```

## Threat Model Notes

### Reflection side effects

When `AllowReflectionObjectSerialization` is enabled, the encoder reads public properties and optionally public fields. Property getters may execute arbitrary user code, trigger database access, lazy-loading, or other side effects. Do not serialize untrusted POCOs unless that is an explicit choice.

### Resource exhaustion

The encoder now rejects graphs that exceed configured depth, collection size, or object member count. These limits are intended to reduce denial-of-service risk, not eliminate it entirely. Extremely large strings and very large individual numeric values can still consume memory proportional to their size.

### Output integrity

In strict mode, the encoder only emits LF line endings and space-based indentation. Outside strict mode, newline values are still constrained to `\n` or `\r\n`, and indentation is constrained to tabs/spaces without embedded newlines.

## Reporting

If you find a vulnerability, open a private security advisory on the GitHub repository if available. If private reporting is not available, avoid publishing a working exploit in a public issue until a fix is ready.

Include:

- affected version or commit
- reproduction steps
- expected impact
- any proposed mitigation

## Current Limitations

- No decoder is implemented yet.
- The library does not enforce a maximum string length.
- The library does not sandbox property getters.

Current hardening covers depth, collection size, object width, string length, and total emitted output length. It does not cap total CPU time or overall process memory usage.

If you need stronger isolation, run serialization in a constrained process boundary.