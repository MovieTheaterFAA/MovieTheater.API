using MovieTheater.API.Architecture;
using MovieTheater.API.Configuration;
using MovieTheater.Application.Hubs;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Services;
using Stripe;
using SwaggerThemes;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.SetupIocContainer();

builder.Configuration
    .AddJsonFile("appsettings.json", true, true)
    .AddEnvironmentVariables();

// Configure Stripe settings
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

// Validate Stripe configuration
var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];
var stripePublishableKey = builder.Configuration["Stripe:PublishableKey"];
var stripeWebhookSecret = builder.Configuration["Stripe:WebhookSecret"];

// Set Stripe API key
StripeConfiguration.ApiKey = stripeSecretKey;

// Set up Stripe app info
var appInfo = new AppInfo { Name = "MovieTheater", Version = "v1" };
StripeConfiguration.AppInfo = appInfo;

// Register HTTP client for Stripe
builder.Services.AddHttpClient("Stripe");

// Register the StripeClient as a service
builder.Services.AddTransient<IStripeClient, StripeClient>(s =>
{
    var clientFactory = s.GetRequiredService<IHttpClientFactory>();

    var sysHttpClient = new SystemNetHttpClient(
        clientFactory.CreateClient("Stripe"),
        StripeConfiguration.MaxNetworkRetries,
        appInfo,
        StripeConfiguration.EnableTelemetry);

    return new StripeClient(stripeSecretKey, httpClient: sysHttpClient);
});

builder.Services.AddSingleton(serviceProvider => stripeWebhookSecret ?? string.Empty);
if (string.IsNullOrEmpty(stripeSecretKey))
{
    Console.WriteLine("CRITICAL: Stripe Secret Key is missing! Payment processing will fail.");
}

if (string.IsNullOrEmpty(stripeWebhookSecret))
{
    Console.WriteLine("WARNING: Stripe Webhook Secret is missing! Webhook validation will be disabled.");
}
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins(
                "https://movietheaterfe.ae-tao-fullstack-api.site", // Production
                "http://localhost:3000",                             // Local dev
                "http://localhost:3001"                             // Local dev
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Tắt việc map claim mặc định
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.WebHost.UseUrls("http://0.0.0.0:5000");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHostedService<EventAutoCleanupBackgroundService>();

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});
var app = builder.Build();

// Check chắc chắn MinIO bucket đã tồn tại sau khi project build
using (var scope = app.Services.CreateScope())
{
    var blob = scope.ServiceProvider.GetRequiredService<IBlobService>();
    await blob.EnsureBucketExistsAsync();
}

app.UseCors("AllowFrontend");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MovieTheater API v1");
        c.RoutePrefix = string.Empty;
        c.InjectStylesheet("/swagger-ui/custom-theme.css");
        c.HeadContent = $"<style>{SwaggerTheme.GetSwaggerThemeCss(Theme.Dracula)}</style>";
        // Config theme của swagger
    });
}

try
{
    app.ApplyMigrations(app.Logger);
}
catch (Exception e)
{
    app.Logger.LogError(e, "An problem occurred during migration!");
}

// app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseSession();

// Map SignalR hubs
app.MapHub<SeatHub>("/seatHubs");
app.MapHub<ChatbotHub>("/chatbotHubs");

// Health check endpoint
app.MapGet("/health", () => Results.Ok("Healthy!"));

app.Run();