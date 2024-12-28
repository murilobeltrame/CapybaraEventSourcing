using System.Collections.Immutable;

using CapybaraEventSource.Domain;
using CapybaraEventSource.Domain.Events;
using FluentAssertions;

namespace CapybaraEventSource.Unit.Tests;

public class CapybaraVilleTests
{
    [Fact]
    public void ApplyEvent_CapybaraArrived_ShouldAddCapybara()
    {
        var capybaraVille = new CapybaraVille();
        var capybaraArrived = new CapybaraArrived { Name = "Filó" };

        var updatedVille = capybaraVille.ApplyEvent(capybaraArrived);

        updatedVille.Capybaras.Should().Contain("Filó");
    }

    [Fact]
    public void ApplyEvent_CapybaraLeft_ShouldRemoveCapybara()
    {
        var capybaraVille = new CapybaraVille { Capybaras = ["Filó"] };
        var capybaraLeft = new CapybaraLeft { Name = "Filó" };

        var updatedVille = capybaraVille.ApplyEvent(capybaraLeft);

        updatedVille.Capybaras.Should().NotContain("Filó");
    }

    [Fact]
    public void ApplyEvent_BroughtFood_ShouldAddFood()
    {
        var capybaraVille = new CapybaraVille();
        var broughtFood = new BroughtFood { CapybaraName = "Filó", Food = "Carrot", Quantity = 3 };

        var updatedVille = capybaraVille.ApplyEvent(broughtFood);

        updatedVille.Food["Carrot"].Should().Be(3);
    }

    [Fact]
    public void ApplyEvent_AteFood_ShouldReduceFoodQuantity()
    {
        var capybaraVille = new CapybaraVille { Food = ImmutableDictionary<string, int>.Empty.Add("Carrot", 5) };
        var ateFood = new AteFood { CapybaraName = "Filó", Food = "Carrot", Quantity = 2 };

        var updatedVille = capybaraVille.ApplyEvent(ateFood);

        updatedVille.Food["Carrot"].Should().Be(3);
    }

    [Fact]
    public void ApplyEvent_AteFood_ShouldRemoveFoodIfQuantityIsZero()
    {
        var capybaraVille = new CapybaraVille { Food = ImmutableDictionary<string, int>.Empty.Add("Carrot", 2) };
        var ateFood = new AteFood { CapybaraName = "Filó", Food = "Carrot", Quantity = 2 };

        var updatedVille = capybaraVille.ApplyEvent(ateFood);

        updatedVille.Food.ContainsKey("Carrot").Should().BeFalse();
    }
}