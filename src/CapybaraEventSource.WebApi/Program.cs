using CapybaraEventSource.Domain;
using CapybaraEventSource.Domain.Commands;
using CapybaraEventSource.Domain.Events;

using Marten;
using Marten.Events;

using Microsoft.AspNetCore.Mvc;
using Microsoft.IO;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddMarten(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("CapybaraDb");
    options.Connection(connectionString!);
});
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapOpenApi();
app.UseHttpsRedirection();
app.UseSwagger();
app.UseSwaggerUI();

app
    .MapPost("/villa/{villaId:guid}/capybaras",
        async ([FromServices] IDocumentStore store, Guid villaId, ArriveCommand command) =>
        {
            var villaExists = false; // going talk about deciders later

            List<object> events = [new CapybaraArrived { Name = command.Name }];
            if (command.Food is not null)
            {
                events.Add(new BroughtFood
                {
                    CapybaraName = command.Name, Food = command.Food, Quantity = command.Quantity ?? 1
                });
            }

            await using var session = store.LightweightSession();
            if (villaExists) { session.Events.Append(villaId, events); }
            else { session.Events.StartStream<CapybaraVille>(villaId, events); }

            await session.SaveChangesAsync();

            return Results.Created($"/villa/{villaId}", villaId);
        })
    .WithTags("CapybaraVille");
app.MapDelete("/villa/{villaId:guid}/capybaras/{capybaraName}",
        async ([FromServices] IDocumentStore store, Guid villaId, string capybaraName) =>
        {
            // what about business rules? deciders
            await using var session = store.LightweightSession();
            session.Events.Append(villaId, new CapybaraLeft { Name = capybaraName });
            await session.SaveChangesAsync();
        })
    .WithTags("CapybaraVille");
app.MapGet("/villa/{villaId:guid}", 
        async ([FromServices] IDocumentStore store, Guid villaId) =>
        {
            await using var session = store.QuerySession();
            return await session.LoadAsync<CapybaraVille>(villaId);
        })
    .WithTags("CapybaraVille");

await app.RunAsync();