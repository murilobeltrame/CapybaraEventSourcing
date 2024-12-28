using Projects;

var builder = DistributedApplication.CreateBuilder(args);

const string databaseName = "CapybaraDb";

var capybaraDb = builder.AddAzurePostgresFlexibleServer("server")
    .RunAsContainer(configureContainer => configureContainer
        .WithEnvironment("POSTGRES_DB", databaseName)
        .WithPgWeb())
    .AddDatabase(databaseName);

builder.AddProject<CapybaraEventSource_WebApi>("webapi")
    .WithReference(capybaraDb);

builder.Build().Run();
