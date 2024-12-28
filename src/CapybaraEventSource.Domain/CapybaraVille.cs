using System.Collections.Immutable;

namespace CapybaraEventSource.Domain;

public record CapybaraVille
{
    public IImmutableList<string> Capybaras { get; init; } = [];
    public IImmutableDictionary<string, int> Food { get; init; } = ImmutableDictionary<string, int>.Empty;
    
    public CapybaraVille ApplyEvent(Events.CapybaraArrived capybaraArrived) => 
        this with
        {
            Capybaras = Capybaras.Add(capybaraArrived.Name)
        };

    public CapybaraVille ApplyEvent(Events.CapybaraLeft capybaraLeft) =>
        this with
        {
            Capybaras = Capybaras.Remove(capybaraLeft.Name)
        };

    public CapybaraVille ApplyEvent(Events.BroughtFood broughtFood) =>
        this with
        {
            Food = Food.SetItem(
                broughtFood.Food,
                Food.TryGetValue(broughtFood.Food, out var quantity)
                    ? quantity + broughtFood.Quantity
                    : broughtFood.Quantity)
        };

    public CapybaraVille ApplyEvent(Events.AteFood ateFood) =>
        this with
        {
            Food = Food.TryGetValue(ateFood.Food, out var quantity) && quantity > ateFood.Quantity
                ? Food.SetItem(ateFood.Food, quantity - ateFood.Quantity)
                : Food.Remove(ateFood.Food)
        };
};