using System.Collections.Immutable;

using Application.V4;

using Marten;
using Marten.Events;
using Marten.Events.Aggregation;
using Marten.Events.Daemon.Resiliency;
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

        options.Projections.Add<CapybaraVilleProjection>(ProjectionLifecycle.Async);
    })
    .AddAsyncDaemon(DaemonMode.HotCold) 
    .UseNpgsqlDataSource(); // Aspire specific configuration.

builder.Services.AddTransient<Decider>();

var app = builder.Build();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.MapPost("village", Endpoints.CapybaraVilleCreated); 
app.MapGet("village/{villageId:guid}", Endpoints.GetVillage); 
app.MapPatch("/village/{villageId:guid}:reprocess", Endpoints.ReprocessVillage);
app.MapPost("village/{villageId:guid}/capybaras", Endpoints.CapybaraArrived);
app.MapDelete("village/{villageId:guid}/capybaras/{capybaraName}", Endpoints.CapybaraLeft);
app.MapPost("village/{villageId:guid}/food", Endpoints.CapybaraAte);
app.MapGet("village/{villageId:guid}/food-consumption", Endpoints.GetFoodEvents);

await app.RunAsync();

/*
    1. Histórico Completo de Alterações
    Cléo anota tudo em um diário: quem chegou, quem saiu e quem trouxe comida. 
    Cada anotação é um evento registrado com data e hora. 
    Se alguém perguntar como foi a festa de 3 meses atrás, Cléo consegue contar tudo nos mínimos detalhes.
*/

/*
    2. Auditoria e Compliance
    Certo dia, a capivara chefe quer saber quem comeu todas as cenouras.
    Cléo revisa os registros e identifica os responsáveis (foi o Capivara Carlos!).
    Sem brigas, apenas fatos.
*/

/*
    3. Escalabilidade e Performance
    CapivaraVille cresce e Cléo começa a registrar eventos em vários cadernos organizados por setores.
    Outras capivaras ajudam no processo, anotando eventos em paralelo e sincronizando tudo no fim do dia.
*/
   
/*
    4. Reprodução e Depuração
    Durante a feira de frutas, um problema acontece: o estoque some!
    Cléo abre os registros e refaz os eventos até descobrir o que deu errado.
    Foi um erro de contagem no evento "Entrega de Melancias".
*/

/*
    5. Flexibilidade para Novos Requisitos
    A vila decide começar a registrar a quantidade das frutas, algo que não era feito antes.
    Cléo não altera os registros antigos—ela só adiciona eventos futuros com essa nova informação.
*/
 
namespace Application.V4
{
    #region Events

    public record CapybaraVilleCreated(Guid Id);

    public record CapybaraArrived(string CapybaraName);
    
    public record CapybaraLeft(string CapybaraName);
    
    public record CapybaraBroughtFood(string CapybaraName, string Food, uint Quantity);
    
    public record CapybaraAte(string CapybaraName, string Food);

    #endregion

    #region Commands

    public record CapybaraVilleCreateCommand(Guid Id);
    
    public record CapybaraArriveCommand(string CapybaraName, string? Food, uint Quantity = 1);
    
    public record CapybaraEatCommand(string CapybaraName, string Food);

    #endregion
    
    public class Decider(IQuerySession querySession)
    {
        public async Task<IEnumerable<object>> On(Guid villageId, CapybaraEatCommand command)
        {
            var village = await querySession.LoadAsync<CapybaraVille>(villageId);
            if (village?.Food.ContainsKey(command.Food) == false)
                throw new InvalidOperationException("Food not available");
            return [new CapybaraAte(command.CapybaraName, command.Food)];
        }
    }
    
    public record CapybaraVille
    {
        public required Guid Id { get; init; }
        public IImmutableList<string> Capybaras { get; init; } = ImmutableList<string>.Empty;
        public IImmutableDictionary<string, uint> Food { get; init; } = ImmutableDictionary<string, uint>.Empty;

        public static CapybaraVille Apply(CapybaraVilleCreated @event) => new() { Id = @event.Id };

        public CapybaraVille Apply(CapybaraArrived @event) =>
            this with { Capybaras = [..Capybaras, @event.CapybaraName] };

        public CapybaraVille Apply(CapybaraLeft @event) =>
            this with { Capybaras = [..Capybaras.Where(w => w != @event.CapybaraName)] };

        public CapybaraVille Apply(CapybaraBroughtFood @event) =>
            this with
            {
                Food = Food.SetItem(
                    @event.Food,
                    Food.GetValueOrDefault<string, uint>(@event.Food, 0) + @event.Quantity)
            };

        public CapybaraVille Apply(CapybaraAte @event) =>
            this with
            {
                Food = Food.ContainsKey(@event.Food) && Food[@event.Food] > 0
                    ? Food.SetItem(@event.Food, Food[@event.Food] - 1).Where(kv => kv.Value > 0).ToImmutableDictionary()
                    : Food
            };
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
                events.Add(new CapybaraBroughtFood(command.CapybaraName, command.Food, command.Quantity));
         
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
        
        public static async Task<IResult> CapybaraAte(
            [FromServices] IDocumentSession session,
            [FromServices] Decider decide,
            [FromRoute] Guid villageId,
            [FromBody] CapybaraEatCommand command)
        {
            // session.Events.Append(villageId, new CapybaraAte(command.CapybaraName, command.Food));
            var events = await decide.On(villageId, command);
            session.Events.Append(villageId, events);
            await session.SaveChangesAsync();
          
            return Results.Ok(await session.LoadAsync<CapybaraVille>(villageId));
        }
      
        // Quem comeu toda a comida?
        // Lista os eventos de comida consumida.
        public static async Task<IEnumerable<CapybaraAte>> GetFoodEvents(
            [FromServices] IQuerySession session,
            [FromRoute] Guid villageId)
        {
            var rawEvents = await session.Events.QueryAllRawEvents()
                .Where(x => x.StreamId == villageId && x.EventTypesAre(typeof(CapybaraAte)))
                .ToListAsync();
            return rawEvents.Select(s => (CapybaraAte)s.Data);
        }
        
        public static async Task ReprocessVillage(
            [FromServices] IDocumentStore store, 
            [FromRoute] Guid villageId)
        {
            await store.Advanced.RebuildSingleStreamAsync<CapybaraVille>(villageId);
        }
    }
}
