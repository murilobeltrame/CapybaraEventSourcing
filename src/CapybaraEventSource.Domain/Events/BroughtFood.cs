namespace CapybaraEventSource.Domain.Events;

public record BroughtFood
{
    public required string CapybaraName { get; init; }
    public required string Food { get; init; }
    public int Quantity { get; init; } = 1;
};