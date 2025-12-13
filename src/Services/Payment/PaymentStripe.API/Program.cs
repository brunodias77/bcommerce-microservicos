using EventBus.RabbitMQ;
using PaymentStripe.API.Configuration;
using PaymentStripe.API.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ===========================================
// LOGGING - Serilog
// ===========================================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// ===========================================
// SERVICES
// ===========================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Payment Stripe API",
        Version = "v1",
        Description = "API de pagamentos integrada com Stripe"
    });
});

// Stripe Settings
builder.Services.Configure<StripeSettings>(
    builder.Configuration.GetSection(StripeSettings.SectionName));

// Stripe Payment Service
builder.Services.AddScoped<IStripePaymentService, StripePaymentService>();

// RabbitMQ EventBus
builder.Services.AddRabbitMQEventBus(builder.Configuration);

// Redis Cache (opcional)
var redisConnection = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "PaymentStripe:";
    });
}

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ===========================================
// MIDDLEWARE PIPELINE
// ===========================================

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Stripe API v1");
    c.RoutePrefix = string.Empty;
});

app.UseCors("AllowAll");
app.UseSerilogRequestLogging();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Service = "Payment Stripe API",
    Timestamp = DateTime.UtcNow
}));

// Endpoint para obter chave pública do Stripe (para frontend)
app.MapGet("/api/stripe/publishable-key", (IConfiguration config) =>
{
    var key = config["Stripe:PublishableKey"];
    return Results.Ok(new { publishableKey = key });
});

Log.Information("Payment Stripe API iniciada");
Log.Information("Swagger disponível em: http://localhost:5003");

app.Run();
