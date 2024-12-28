namespace CapybaraEventSource.Domain.Events;

public record CapybaraArrived
{
    public required string Name { get; init; }
}