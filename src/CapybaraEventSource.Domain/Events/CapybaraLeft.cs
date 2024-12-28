namespace CapybaraEventSource.Domain.Events;

public record CapybaraLeft
{
    public required string Name { get; init; }
};