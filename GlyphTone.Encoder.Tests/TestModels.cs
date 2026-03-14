namespace GlyphTone.Tests;

internal sealed class SampleEnvelope
{
    public required string Title { get; init; }

    public required SamplePerson User { get; init; }

    public required string[] Tags { get; init; }
}

internal sealed class SamplePerson
{
    public bool Active { get; init; }

    public int Id { get; init; }

    public required string Name { get; init; }
}

internal sealed class PublicFieldContainer
{
    public int Id;

    public string Name = string.Empty;
}

internal sealed class LinkedNode
{
    public required string Name { get; init; }

    public LinkedNode? Next { get; set; }
}

internal sealed class OrderedModel
{
    public int Zeta { get; init; }

    public int Alpha { get; init; }

    public int Beta { get; init; }
}