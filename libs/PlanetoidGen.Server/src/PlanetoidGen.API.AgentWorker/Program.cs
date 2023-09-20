using PlanetoidGen.API.AgentWorker.Workers;
using PlanetoidGen.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .ConfigureServiceOptions(builder.Configuration)
    .ConfigureMessageBrokerOptions(builder.Configuration)
    .ConfigureDocumentDbOptions(builder.Configuration)
    .ConfigureGeoInfoOptions(builder.Configuration)
    .ConfigureAgentWorkerServiceOptions(builder.Configuration)
    .ConfigureDataAccess(builder.Configuration)
    .ConfigureServices()
    .ConfigureLogging(builder.Host);

builder.Services.AddHostedService<AgentWorkerService>();

using (var app = builder.Build())
{
    app.UseConfiguredLogging(app.Services);
    app.Run();
}
