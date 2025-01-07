using Projects;

var builder = DistributedApplication.CreateBuilder(args);

const string databaseName = "capybaraVilleDb";
var capybaraVilleDb = builder.AddAzurePostgresFlexibleServer("dbServer")
    .RunAsContainer(configureContainer => configureContainer
        .WithEnvironment("POSTGRES_DB", databaseName)
        .WithPgWeb())
    .AddDatabase(databaseName);

// v6 only
var capybaraVilleBroker = builder.AddRabbitMQ("brokerServer")
    .WithManagementPlugin();

builder.AddProject<CapybaraVile_WebApi>("capybaraVilleApi")
    .WithReplicas(3) //v3 only
    .WithReference(capybaraVilleBroker)
    .WaitFor(capybaraVilleBroker)// v6 only
    .WithReference(capybaraVilleDb)
    .WaitFor(capybaraVilleDb); 

await builder.Build().RunAsync();