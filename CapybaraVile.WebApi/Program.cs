using System.Collections.Immutable;

using Application.V1;

using Marten;
using Marten.Events.Aggregation;
using Marten.Events.Projections;

using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.AddNpgsqlDataSource("capybaraVilleDb"); // for Aspire works correctly.
builder.Services.AddMarten(options =>
    {
        // var connectionString = builder.Configuration.GetConnectionString("capybaraVilleDb");
        // options.Connection(connectionString!);
        // REMARK: Normal configuration. Commented for Aspire specific configuration.

        // This adds additional metadata tracking to the
        // event store tables
        options.Events.MetadataConfig.HeadersEnabled = true;
        options.Events.MetadataConfig.CausationIdEnabled = true;
        options.Events.MetadataConfig.CorrelationIdEnabled = true;

        options.Projections.Add<CapybaraVilleProjection>(ProjectionLifecycle.Inline);
    })
    .UseNpgsqlDataSource(); // Aspire specific configuration.

var app = builder.Build();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.MapPost("village", Endpoints.CapybaraVilleCreated); 
app.MapGet("village/{villageId:guid}", Endpoints.GetVillage); 
app.MapPost("village/{villageId:guid}/capybaras", Endpoints.CapybaraArrived);
app.MapDelete("village/{villageId:guid}/capybaras/{capybaraName}", Endpoints.CapybaraLeft);

await app.RunAsync();

/*
    1. Histórico Completo de Alterações
    Cléo anota tudo em um diário: quem chegou, quem saiu e quem trouxe comida. 
    Cada anotação é um evento registrado com data e hora. 
    Se alguém perguntar como foi a festa de 3 meses atrás, Cléo consegue contar tudo nos mínimos detalhes.
*/

namespace Application.V1
{
    #region Events

    public record CapybaraVilleCreated(Guid Id);

    public record CapybaraArrived(string CapybaraName);
 
    public record CapybaraLeft(string CapybaraName);
 
    public record CapybaraBroughtFood(string CapybaraName, string Food);

    #endregion

    #region Commands

    public record CapybaraVilleCreateCommand(Guid Id);

    public record CapybaraArriveCommand(string CapybaraName, string? Food);
    
    #endregion
    
    public record CapybaraVille
    {
        public required Guid Id { get; init; }
        public IImmutableList<string> Capybaras { get; init; } = ImmutableList<string>.Empty;
        public IImmutableList<string> Food { get; init; } = ImmutableList<string>.Empty;

        public static CapybaraVille Apply(CapybaraVilleCreated @event) => new() { Id = @event.Id };
     
        public CapybaraVille Apply(CapybaraArrived @event) => 
            this with { Capybaras = [..Capybaras, @event.CapybaraName] };

        public CapybaraVille Apply(CapybaraLeft @event) =>
            this with { Capybaras = [..Capybaras.Where(w => w != @event.CapybaraName)] };

        public CapybaraVille Apply(CapybaraBroughtFood @event) =>
            this with { Food = [..Food, @event.Food] };
    }
    
    public class CapybaraVilleProjection : SingleStreamProjection<CapybaraVille>
    {
        public CapybaraVilleProjection()
        {
            ProjectEvent<CapybaraVilleCreated>((_, @event) => CapybaraVille.Apply(@event));
            ProjectEvent<CapybaraArrived>((ville, @event) => ville.Apply(@event));
            ProjectEvent<CapybaraLeft>((ville, @event) => ville.Apply(@event));
            ProjectEvent<CapybaraBroughtFood>((ville, @event) => ville.Apply(@event));
        }
    }
    
    public static class Endpoints
    {
        public static async Task<IResult> CapybaraVilleCreated(
            [FromServices] IDocumentSession session,
            [FromBody] CapybaraVilleCreateCommand command)
        {
            session.Events.StartStream(command.Id, new CapybaraVilleCreated(command.Id));
            await session.SaveChangesAsync();
         
            return Results.Created(
                $"village/{command.Id}", 
                await session.LoadAsync<CapybaraVille>(command.Id));
        }
     
        public static async Task<IResult> CapybaraArrived(
            [FromServices] IDocumentSession session,
            [FromRoute] Guid villageId,
            [FromBody] CapybaraArriveCommand command)
        {
            List<object> events = [new CapybaraArrived(command.CapybaraName)];
            if (!string.IsNullOrWhiteSpace(command.Food))
                events.Add(new CapybaraBroughtFood(command.CapybaraName, command.Food));
         
            session.Events.Append(villageId, events);
            await session.SaveChangesAsync();

            return Results.Ok(await session.LoadAsync<CapybaraVille>(villageId));
        }
     
        public static async Task<IResult> CapybaraLeft(
            [FromServices] IDocumentSession session,
            [FromRoute] Guid villageId,
            string capybaraName)
        {
            session.Events.Append(villageId, new CapybaraLeft(capybaraName));
            await session.SaveChangesAsync();

            return Results.Ok(await session.LoadAsync<CapybaraVille>(villageId));
        }
     
        public static async Task<CapybaraVille?> GetVillage(
            [FromServices] IDocumentSession session, 
            [FromRoute] Guid villageId)
        {
            return await session.LoadAsync<CapybaraVille>(villageId);
        }
    }
}
