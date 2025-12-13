using Consumer.API.EventHandlers;
using EventBus.Abstractions;
using EventBus.RabbitMQ;
using Serilog;
using Shared.IntegrationEvents;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Consumer API", Version = "v1" });
});

// Add RabbitMQ EventBus
builder.Services.AddRabbitMQEventBus(builder.Configuration);

// Register Event Handlers
builder.Services.AddIntegrationEventHandler<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Consumer API v1");
    c.RoutePrefix = string.Empty;
});

app.UseSerilogRequestLogging();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Consumer API" }));

// Subscribe to events
var eventBus = app.Services.GetRequiredService<IEventBus>();
eventBus.Subscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();

Log.Information("Consumer API iniciada e inscrita nos eventos");
Log.Information("Aguardando eventos de OrderCreatedIntegrationEvent...");

app.Run();
