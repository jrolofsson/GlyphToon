# GlyphTone.Encoder

[![CI](https://github.com/jrolofsson/GlyphToon/actions/workflows/ci.yml/badge.svg)](https://github.com/jrolofsson/GlyphToon/actions/workflows/ci.yml)
[![Publish Package](https://github.com/jrolofsson/GlyphToon/actions/workflows/publish-package.yml/badge.svg)](https://github.com/jrolofsson/GlyphToon/actions/workflows/publish-package.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

`GlyphTone.Encoder` is a focused .NET 10 class library for writing the JSON data model as TOON, the Token-Oriented Object Notation format. TOON keeps JSON's core types while replacing braces with indentation and using compact array headers such as `[N]` and `{field1,field2}` to make structure explicit and economical.

The format is especially effective for uniform arrays of objects. Instead of repeating field names on every item, TOON can emit one tabular header and then stream rows underneath it. This library implements the encoder side only, with conservative, deterministic output that stays close to the published TOON semantics.

Current scope: encoder implemented, decoder intentionally not included yet.

## Solution Structure

```text
GlyphTone.slnx
Directory.Build.props
GlyphTone.Encoder/
GlyphTone.Encoder.Tests/
README.md
LICENSE
```

## Build and Test

```bash
dotnet restore GlyphTone.slnx
dotnet build GlyphTone.slnx -c Release
dotnet test GlyphTone.slnx -c Release
```

## Usage

```csharp
using GlyphTone;

var value = new
{
    Users = new[]
    {
        new { Id = 1, Name = "Alice", Role = "admin" },
        new { Id = 2, Name = "Bob", Role = "user" },
    },
};

var toon = Encoder.Serialize(
    value,
    new EncoderOptions
    {
        PropertyNamingPolicy = static name => char.ToLowerInvariant(name[0]) + name[1..],
    });

Console.WriteLine(toon);
```

Output:

```text
users[2]{id,name,role}:
  1,Alice,admin
  2,Bob,user
```

Writing directly to a `TextWriter`:

```csharp
using var writer = File.CreateText("users.toon");

Encoder.Serialize(
    new Dictionary<string, object?>
    {
        ["id"] = 42,
        ["name"] = "Ada",
    },
    writer);
```

Nested object example:

```csharp
var toon = Encoder.Serialize(
    new
    {
        Context = new
        {
            Task = "Example",
            Region = "eu-west",
        },
        Tags = new[] { "llm", "toon" },
    },
    new EncoderOptions
    {
        PropertyNamingPolicy = static name => char.ToLowerInvariant(name[0]) + name[1..],
    });
```

Output:

```text
context:
  region: eu-west
  task: Example
tags[2]: llm,toon
```

## Encoder Options

`EncoderOptions` exposes the main formatting controls:

- `Indent`: indentation unit for nested structures. Strict mode requires spaces only.
- `NewLine`: line separator between TOON lines. Strict mode requires `\n`.
- `PropertyNamingPolicy`: optional hook for reflected property and field names.
- `IgnoreNullValues`: omit null object members.
- `SortProperties`: sort reflected members by effective name for deterministic output.
- `SortDictionaryKeys`: sort dictionary keys ordinally for deterministic output.
- `PreferTabularArrays`: use tabular arrays when every row is a homogeneous primitive-valued object.
- `StrictMode`: enforce strict encoder-side constraints for indentation and line endings.
- `IncludeFields`: include public fields in addition to public readable properties.

## Supported Inputs

- `null`, `bool`, `string`, and common numeric primitives
- `DateTime`, `DateTimeOffset`, `Guid`, and `Uri` as strings
- arrays, lists, and general `IEnumerable`
- dictionaries and dictionary-like objects with string-compatible keys
- POCOs via public readable properties
- public fields when `IncludeFields` is enabled

## Determinism and Behavior

- Numbers are emitted in canonical decimal form without exponent notation.
- Strings are left unquoted only when they are clearly safe in TOON.
- Empty root objects serialize as an empty document.
- Empty arrays serialize as `[0]:`.
- Tabular arrays are used only when the collection is non-empty, every item is an object, every object has the same field set, and every field value is a primitive.
- Circular references are detected and reported as `ToonEncodingException`.

## Notes on Scope

This repository currently implements only encoding. The internal design separates normalization, inspection, formatting, and emission so a decoder can be added later without forcing a public API break.
