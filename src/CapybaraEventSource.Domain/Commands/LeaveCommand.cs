namespace CapybaraEventSource.Domain.Commands;

public record LeaveCommand
{
    public required string Name { get; init; }
};