using Projects;

var builder = DistributedApplication.CreateBuilder(args);

const string databaseName = "capybaraVilleDb";
var capybaraVilleDb = builder.AddAzurePostgresFlexibleServer("dbServer")
    .RunAsContainer(configureContainer => configureContainer
        .WithEnvironment("POSTGRES_DB", databaseName)
        .WithPgWeb())
    .AddDatabase(databaseName);

// v6 only
var username = builder.AddParameter("username", secret: true);
var password = builder.AddParameter("password", secret: true);
var capybaraVilleBroker = builder.AddRabbitMQ("brokerServer", username, password)
    .WithManagementPlugin();

builder.AddProject<CapybaraVile_WebApi>("capybaraVilleApi")
    .WithReplicas(3) //v3 only
    .WithReference(capybaraVilleBroker)
    .WaitFor(capybaraVilleBroker)// v6 only
    .WithReference(capybaraVilleDb)
    .WaitFor(capybaraVilleDb); 

builder.Build().Run();