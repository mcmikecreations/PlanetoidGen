using PlanetoidGen.API.Controllers;
using PlanetoidGen.API.Helpers;
using PlanetoidGen.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc(opt => opt.EnableDetailedErrors = true);
builder.Services.AddGrpcReflection();
builder.Services
    .ConfigureServiceOptions(builder.Configuration)
    .ConfigureMessageBrokerOptions(builder.Configuration)
    .ConfigureDocumentDbOptions(builder.Configuration)
    .ConfigureGeoInfoOptions(builder.Configuration)
    .ConfigureDataAccess(builder.Configuration)
    .ConfigureServices()
    .ConfigureHelpers()
    .ConfigureLogging(builder.Host)
    .AddControllers();

using (var app = builder.Build())
{
    app.UseConfiguredLogging(app.Services);

    app.UseGrpcWeb();
    app.MapGrpcService<DummyStreamController>().EnableGrpcWeb();
    app.MapGrpcService<AgentController>().EnableGrpcWeb();
    app.MapGrpcService<PlanetoidController>().EnableGrpcWeb();
    app.MapGrpcService<TileGenerationController>().EnableGrpcWeb();
    app.MapGrpcService<GenerationLODController>().EnableGrpcWeb();
    app.MapGrpcService<BinaryContentController>().EnableGrpcWeb();
    app.MapGrpcReflectionService().EnableGrpcWeb();

    app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
    app.MapControllers();

    app.Run();
}
