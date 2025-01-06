using System.Collections.Immutable;

using JasperFx.Core.Reflection;

using Marten;
using Marten.Events;
using Marten.Events.Aggregation;
using Marten.Events.Daemon.Resiliency;
using Marten.Events.Projections;

using Microsoft.AspNetCore.Mvc;

using Wolverine;
using Wolverine.Marten;
using Wolverine.RabbitMQ;
using Wolverine.RabbitMQ.Internal;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Host.UseWolverine(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("brokerServer");
    var connectionUri = new Uri(connectionString!);
    options
        .UseRabbitMq(connectionUri)
        .DeclareExchangeWithDefaults<CapybaraVilleCreated>()
        .DeclareExchangeWithDefaults<CapybaraArrived>()
        .DeclareExchangeWithDefaults<CapybaraBroughtFood>()
        .DeclareExchangeWithDefaults<CapybaraAte>()
        .DeclareExchangeWithDefaults<CapybaraLeft>()
        .AutoProvision()
        .UseSenderConnectionOnly();
    options.PublishMessageToRabbitExchangeWithDefaults<CapybaraVilleCreated>();
    options.PublishMessageToRabbitExchangeWithDefaults<CapybaraArrived>();
    options.PublishMessageToRabbitExchangeWithDefaults<CapybaraBroughtFood>();
    options.PublishMessageToRabbitExchangeWithDefaults<CapybaraAte>();
    options.PublishMessageToRabbitExchangeWithDefaults<CapybaraLeft>();
    
    options.Policies.UseDurableOutboxOnAllSendingEndpoints();
}); // for v6

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

        // options.Projections.Add<V1CapybaraVilleProjection>(ProjectionLifecycle.Async);
        // options.Projections.Add<V4CapybaraVilleProjection>(ProjectionLifecycle.Async); //v4
        options.Projections.Add<V5CapybaraVilleProjection>(ProjectionLifecycle.Async); //v5
    })
    .AddAsyncDaemon(DaemonMode.HotCold) // for v3
    .IntegrateWithWolverine() // for v6
    .UseNpgsqlDataSource(); // Aspire specific configuration.

var app = builder.Build();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

// app.MapGet("village/{villageId:guid}", V1Endpoints.GetVillage); // for v1
// app.MapGet("village/{villageId:guid}", V4Endpoints.GetVillage); // for v4
app.MapGet("village/{villageId:guid}", V5Endpoints.GetVillage); // for v5
app.MapPatch("/village/{villageId:guid}:reprocess", V4Endpoints.ReprocessVillage);
// app.MapPost("village", V1Endpoints.CapybaraVilleCreated); // for v1
// app.MapPost("village/{villageId:guid}/capybaras", V1Endpoints.CapybaraArrived);
// app.MapDelete("village/{villageId:guid}/capybaras/{capybaraName}", V1Endpoints.CapybaraLeft);
// app.MapPost("village/{villageId:guid}/food", V2Endpoints.CapybaraAte); // for v2
app.MapGet("village/{villageId:guid}/food-consumption", V2Endpoints.GetFoodEvents);
app.MapPost("village/{villageId:guid}/capybaras", V5Endpoints.CapybaraArrived); // for v5
app.MapPost("village", V6Endpoints.CapybaraVilleCreated); // for v6
// app.MapPost("village/{villageId:guid}/capybaras", V6Endpoints.CapybaraArrived);
app.MapDelete("village/{villageId:guid}/capybaras/{capybaraName}", V6Endpoints.CapybaraLeft);
app.MapPost("village/{villageId:guid}/food", V6Endpoints.CapybaraAte);

await app.RunAsync();

public static class StringExtensions
{
    public static string ToExchangeName(this string value) => $"{value}Exchange";
    public static string ToQueueName(this string value) => $"{value}Queue";
}

public static class RabbitMqTransportExpressionExtensions
{
    public static RabbitMqTransportExpression DeclareExchangeWithDefaults<T>(this RabbitMqTransportExpression expression)
    {
        var typeName = typeof(T).ShortNameInCode();
        var exchangeName = typeName.ToExchangeName();
        var queueName = typeName.ToQueueName();
        
        return expression.DeclareExchange(
            exchangeName, 
            configure => configure.BindQueue(queueName, exchangeName));
    }
}

public static class WolverineOptionsExtensions
{
    public static  RabbitMqSubscriberConfiguration PublishMessageToRabbitExchangeWithDefaults<T>(this WolverineOptions options)
    {
        var typeName = typeof(T).ShortNameInCode();
        var exchangeName = typeName.ToExchangeName();
        return options.PublishMessage<T>().ToRabbitExchange(exchangeName);
    }
}

/*
    1. Histórico Completo de Alterações
    Cléo anota tudo em um diário: quem chegou, quem saiu e quem trouxe comida. 
    Cada anotação é um evento registrado com data e hora. 
    Se alguém perguntar como foi a festa de 3 meses atrás, Cléo consegue contar tudo nos mínimos detalhes.
*/
 
 public record CapybaraVilleCreated(Guid Id);

 public record CapybaraVilleCreateCommand(Guid Id);

 public record CapybaraArrived(string CapybaraName);
 
 public record CapybaraLeft(string CapybaraName);
 
 public record CapybaraBroughtFood(string CapybaraName, string Food);

 public record CapybaraArriveCommand(string CapybaraName, string? Food);

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

 public class V1CapybaraVilleProjection : SingleStreamProjection<CapybaraVille>
 {
     public V1CapybaraVilleProjection()
     {
         ProjectEvent<CapybaraVilleCreated>((_, @event) => CapybaraVille.Apply(@event));
         ProjectEvent<CapybaraArrived>((ville, @event) => ville.Apply(@event));
         ProjectEvent<CapybaraLeft>((ville, @event) => ville.Apply(@event));
         ProjectEvent<CapybaraBroughtFood>((ville, @event) => ville.Apply(@event));
     }
 }

 public static class V1Endpoints
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

/*
    2. Auditoria e Compliance
    Certo dia, a capivara chefe quer saber quem comeu todas as cenouras. 
    Cléo revisa os registros e identifica os responsáveis (foi o Capivara Carlos!). 
    Sem brigas, apenas fatos.
*/
  
  public record CapybaraAte(string CapybaraName, string Food);
  
  public record CapybaraEatCommand(string CapybaraName, string Food);

  public static class V2Endpoints
  {
      public static async Task<IResult> CapybaraAte(
          [FromServices] IDocumentSession session,
          [FromRoute] Guid villageId,
          [FromBody] CapybaraEatCommand command)
      {
          session.Events.Append(villageId, new CapybaraAte(command.CapybaraName, command.Food));
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
  }
  
/*
    3. Escalabilidade e Performance
    CapivaraVille cresce e Cléo começa a registrar eventos em vários cadernos organizados por setores. 
    Outras capivaras ajudam no processo, anotando eventos em paralelo e sincronizando tudo no fim do dia.
*/
   
   // REVIEW: @29.
   
/*
    4. Reprodução e Depuração
    Durante a feira de frutas, um problema acontece: o estoque some! 
    Cléo abre os registros e refaz os eventos até descobrir o que deu errado.
    Foi um erro de contagem no evento "Entrega de Melancias".
*/

   public record V4CapybaraVille
   {
       public required Guid Id { get; init; }
       public IImmutableList<string> Capybaras { get; init; } = ImmutableList<string>.Empty;
       public IImmutableDictionary<string, uint> Food { get; init; } = ImmutableDictionary<string, uint>.Empty;

       public static V4CapybaraVille Apply(CapybaraVilleCreated @event) => new() { Id = @event.Id };

       public V4CapybaraVille Apply(CapybaraArrived @event) =>
           this with { Capybaras = [..Capybaras, @event.CapybaraName] };

       public V4CapybaraVille Apply(CapybaraLeft @event) =>
           this with { Capybaras = [..Capybaras.Where(w => w != @event.CapybaraName)] };

       public V4CapybaraVille Apply(CapybaraBroughtFood @event) =>
           this with
           {
               Food = Food.SetItem(
                   @event.Food,
                   Food.GetValueOrDefault<string, uint>(@event.Food, 0) + 1)
           };

       public V4CapybaraVille Apply(CapybaraAte @event) =>
           this with
           {
               Food = Food.ContainsKey(@event.Food) && Food[@event.Food] > 0
                   ? Food.SetItem(@event.Food, Food[@event.Food] - 1).Where(kv => kv.Value > 0).ToImmutableDictionary()
                   : Food
           };
   }

   public class V4CapybaraVilleProjection : SingleStreamProjection<V4CapybaraVille>
   {
       public V4CapybaraVilleProjection()
       {
           ProjectEvent<CapybaraVilleCreated>((_, @event) => V4CapybaraVille.Apply(@event));
           ProjectEvent<CapybaraArrived>((ville, @event) => ville.Apply(@event));
           ProjectEvent<CapybaraLeft>((ville, @event) => ville.Apply(@event));
           ProjectEvent<CapybaraBroughtFood>((ville, @event) => ville.Apply(@event));
           ProjectEvent<CapybaraAte>((ville, @event) => ville.Apply(@event));
       }
   }

   public static class V4Endpoints
   {
       public static async Task<V4CapybaraVille?> GetVillage(
           [FromServices] IQuerySession session, 
           [FromRoute] Guid villageId)
       {
           return await session.LoadAsync<V4CapybaraVille>(villageId);
       }

       public static async Task ReprocessVillage(
           [FromServices] IDocumentStore store, 
           [FromRoute] Guid villageId)
       {
           await store.Advanced.RebuildSingleStreamAsync<V4CapybaraVille>(villageId);
       }
   }

    // DEMO: Exploração do DB
    
/*
    5. Flexibilidade para Novos Requisitos
    A vila decide começar a registrar a quantidade das frutas, algo que não era feito antes. 
    Cléo não altera os registros antigos—ela só adiciona eventos futuros com essa nova informação.
*/
     
     public record V5CapybaraBroughtFood(string CapybaraName, string Food, uint Quantity);

     public record V5CapybaraArriveCommand(string CapybaraName, string? Food, uint Quantity = 1);

     public record V5CapybaraVille
     {
         public required Guid Id { get; init; }
         public IImmutableList<string> Capybaras { get; init; } = ImmutableList<string>.Empty;
         public IImmutableDictionary<string, uint> Food { get; init; } = ImmutableDictionary<string, uint>.Empty;

         public static V5CapybaraVille Apply(CapybaraVilleCreated @event) => new() { Id = @event.Id };

         public V5CapybaraVille Apply(CapybaraArrived @event) =>
             this with { Capybaras = [..Capybaras, @event.CapybaraName] };

         public V5CapybaraVille Apply(CapybaraLeft @event) =>
             this with { Capybaras = [..Capybaras.Where(w => w != @event.CapybaraName)] };

         public V5CapybaraVille Apply(V5CapybaraBroughtFood @event) =>
             this with
             {
                 Food = Food.SetItem(
                     @event.Food,
                     Food.GetValueOrDefault<string, uint>(@event.Food, 0) + @event.Quantity)
             };

         public V5CapybaraVille Apply(CapybaraAte @event) =>
             this with
             {
                 Food = Food.ContainsKey(@event.Food) && Food[@event.Food] > 0
                     ? Food.SetItem(@event.Food, Food[@event.Food] - 1).Where(kv => kv.Value > 0).ToImmutableDictionary()
                     : Food
             };
     }

     public class V5CapybaraVilleProjection : SingleStreamProjection<V5CapybaraVille>
     {
         public V5CapybaraVilleProjection()
         {
             ProjectEvent<CapybaraVilleCreated>((_, @event) => V4CapybaraVille.Apply(@event));
             ProjectEvent<CapybaraArrived>((ville, @event) => ville.Apply(@event));
             ProjectEvent<CapybaraLeft>((ville, @event) => ville.Apply(@event));
             ProjectEvent<V5CapybaraBroughtFood>((ville, @event) => ville.Apply(@event));
             ProjectEvent<CapybaraAte>((ville, @event) => ville.Apply(@event));
         }
     }

     public static class V5Endpoints
     {
         public static async Task<IResult> GetVillage(
             [FromServices] IDocumentSession session,
             [FromRoute] Guid villageId)
         {
             return await session.LoadAsync<V5CapybaraVille>(villageId) is { } village ? 
                 Results.Ok(village) : 
                 Results.NotFound();
         }

         public static async Task<IResult> CapybaraArrived(
             [FromServices] IDocumentSession session,
             [FromRoute] Guid villageId,
             [FromBody] V5CapybaraArriveCommand command)
         {
             List<object> events = [new CapybaraArrived(command.CapybaraName)];
             if (!string.IsNullOrWhiteSpace(command.Food))
                 events.Add(new V5CapybaraBroughtFood(command.CapybaraName, command.Food, command.Quantity));
             
             session.Events.Append(villageId, events);
             await session.SaveChangesAsync();

             return Results.Ok(await session.LoadAsync<V5CapybaraVille>(villageId));
         }
     }
    
/*
    6. Integração com Sistemas Reativos
    Agora a vila quer alertas automáticos quando um evento importante acontece—como a chegada de um caminhão de cenouras. 
    Cléo adiciona um sino que toca sempre que anota algo relevante.
*/

     public static class V6Endpoints
     {
         public static async Task<IResult> CapybaraVilleCreated(
             [FromServices] IDocumentSession session,
             [FromServices] IMartenOutbox  outbox,
             [FromBody] CapybaraVilleCreateCommand command)
         {
             var @event = new CapybaraVilleCreated(command.Id);
             session.Events.StartStream(command.Id, @event);
             await outbox.SendAsync(@event);
             await session.SaveChangesAsync();

             return Results.Created(
                 $"village/{command.Id}",
                 await session.LoadAsync<CapybaraVille>(command.Id));
         }

         public static async Task<IResult> CapybaraArrived(
             [FromServices] IDocumentSession session,
             [FromServices] IMartenOutbox outbox,
             [FromRoute] Guid villageId,
             [FromBody] V5CapybaraArriveCommand command)
         {
             List<object> events = [new CapybaraArrived(command.CapybaraName)];
             if (!string.IsNullOrWhiteSpace(command.Food))
                 events.Add(new V5CapybaraBroughtFood(command.CapybaraName, command.Food, command.Quantity));

             session.Events.Append(villageId, events);
             foreach (var @event in events)
             {
                 await outbox.SendAsync(@event);
             }
             await session.SaveChangesAsync();
             
             return Results.Ok(await session.LoadAsync<V5CapybaraVille>(villageId));
         }

         public static async Task<IResult> CapybaraLeft(
             [FromServices] IDocumentSession session,
             [FromServices] IMartenOutbox outbox,
             [FromRoute] Guid villageId,
             string capybaraName)
         {
             var @event = new CapybaraLeft(capybaraName);
             session.Events.Append(villageId, @event);
             await outbox.SendAsync(@event); 
             await session.SaveChangesAsync();
             
             return Results.Ok(await session.LoadAsync<V5CapybaraVille>(villageId));
         }
         
         public static async Task<IResult> CapybaraAte(
             [FromServices] IDocumentSession session,
             [FromServices] IMartenOutbox outbox,
             [FromRoute] Guid villageId,
             [FromBody] CapybaraEatCommand command)
         {
             var @event = new CapybaraAte(command.CapybaraName, command.Food);
             session.Events.Append(villageId, @event);
             await outbox.SendAsync(@event);
             await session.SaveChangesAsync();
             
             return Results.Ok(await session.LoadAsync<V5CapybaraVille>(villageId));
         }
     }