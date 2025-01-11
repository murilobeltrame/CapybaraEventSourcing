using Projects;

var builder = DistributedApplication.CreateBuilder(args);

const string databaseName = "capybaraVilleDb";
var capybaraVilleDb = builder.AddAzurePostgresFlexibleServer("dbServer")
    .RunAsContainer(configureContainer => configureContainer
        .WithEnvironment("POSTGRES_DB", databaseName)
        .WithPgWeb())
    .AddDatabase(databaseName);

builder.AddProject<CapybaraVile_WebApi>("capybaraVilleApi")
    .WithReplicas(3)
    .WithReference(capybaraVilleDb)
    .WaitFor(capybaraVilleDb); 

await builder.Build().RunAsync();