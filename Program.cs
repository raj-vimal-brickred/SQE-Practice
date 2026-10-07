using Microsoft.EntityFrameworkCore;
using SQE_Practice.Services;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SQE_Practice.Observability;
using SQE_Practice.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<EventStore>();
builder.Services.AddSingleton<EventIngestionService>();
builder.Services.AddSingleton<QueryLoader>();
builder.Services.AddSingleton<AggregrationEngine>();
builder.Services.AddSingleton<MetricStore>();
builder.Services.AddSingleton<StandingQueryEngine>();
builder.Services.AddSingleton<AlertStore>();
builder.Services.AddSingleton<AlertEngine>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var connectionString =
builder.Configuration.GetConnectionString("SqeDatabase")
?? throw new InvalidOperationException(
"Connection string 'SqeDatabase' was not found.");

builder.Services.AddDbContextFactory<SqeDbContext>(
options =>
{
    options.UseSqlServer(
    connectionString,
    sqlServerOptions =>
    {
        sqlServerOptions.EnableRetryOnFailure(
    maxRetryCount: 5,
    maxRetryDelay:
    TimeSpan.FromSeconds(10),
    errorNumbersToAdd: null);
    });
});

builder.Services
    .AddOpenTelemetry()

    .ConfigureResource(resource =>
    {
        resource.AddService(
            serviceName: SqeTelemetry.ServiceName);
    })

    .WithTracing(tracing =>
    {
        tracing
            .AddSource(SqeTelemetry.ServiceName)

            .AddAspNetCoreInstrumentation()

            .AddConsoleExporter();
    })

    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(SqeTelemetry.ServiceName)

            .AddAspNetCoreInstrumentation()

            .AddConsoleExporter();
    });


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();