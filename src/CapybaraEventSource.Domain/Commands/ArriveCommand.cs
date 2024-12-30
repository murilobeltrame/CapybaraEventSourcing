namespace CapybaraEventSource.Domain.Commands;

public record ArriveCommand
{
    public required string Name { get; init; }
    public string? Food { get; init; }
    public int? Quantity { get; init; }
};