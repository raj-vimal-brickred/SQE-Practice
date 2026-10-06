using SQE_Practice.Services;
using SQE_Practice.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSingleton<EventIngestionService>();
builder.Services.AddSingleton<QueryLoader>();
builder.Services.AddSingleton<AggregationEngine>();
builder.Services.AddSingleton<MetricStore>();
builder.Services.AddSingleton<StandingQueryEngine>();
builder.Services.AddSingleton<AlertStore>();
builder.Services.AddSingleton<AlertEngine>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();