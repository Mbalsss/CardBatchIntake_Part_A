using CardBatchIntake.Api.Repositories;
using CardBatchIntake.Api.Services;

// Connect incoming API requests to the controllers, services, and in-memory store.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<IBatchRepository, InMemoryBatchRepository>();
builder.Services.AddSingleton<IBatchValidator, BatchValidator>();
builder.Services.AddScoped<IBatchService, BatchService>();

var app = builder.Build();
app.MapControllers();
app.Run();

public partial class Program { }
