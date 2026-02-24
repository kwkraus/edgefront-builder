using Azure.Identity;
using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Features.Metrics;
using EdgeFront.Builder.Features.Series;
using EdgeFront.Builder.Features.Sessions;
using EdgeFront.Builder.Features.Webhook;
using EdgeFront.Builder.Infrastructure.Background;
using EdgeFront.Builder.Infrastructure.Data;
using EdgeFront.Builder.Infrastructure.Graph;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication & Authorization (Entra ID / Azure AD)
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();
builder.Services.AddAuthorization();

// Explicitly set valid audiences to handle both v1.0 (api://ClientId) and v2.0 (ClientId) token formats.
// Microsoft.IdentityModel 8.x changed audience validation; RegisterValidAudience may not
// reliably inject audiences at runtime. This ensures they are always present.
var apiClientId = builder.Configuration["AzureAd:ClientId"]!;
builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters.ValidAudiences = new[]
    {
        apiClientId,
        $"api://{apiClientId}"
    };
});

if (builder.Environment.IsDevelopment())
{
    // Show actual audience values in token validation errors instead of scrubbed "[PII hidden]"
    AppContext.SetSwitch("Switch.Microsoft.IdentityModel.DoNotScrubExceptions", true);
}

// Domain services
builder.Services.AddSingleton(sp =>
    new InternalDomainFilter(
        builder.Configuration.GetSection("InternalDomains").Get<string[]>() ?? Array.Empty<string>()
    ));

// Feature services
builder.Services.AddScoped<SeriesService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<MetricsService>();
builder.Services.AddScoped<WebhookService>();

// Graph services
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var tenantId = config["AzureAd:TenantId"]!;
    var clientId = config["AzureAd:ClientId"]!;
    var clientSecret = config["AzureAd:ClientSecret"] ?? string.Empty;
    var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
    return new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });
});
builder.Services.AddScoped<ITeamsGraphClient, TeamsGraphClient>();
builder.Services.AddScoped<IOboTokenService, OboTokenService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<DriftDetectionService>();
builder.Services.AddScoped<WarmRuleEvaluator>();
builder.Services.AddScoped<MetricsRecomputeService>();
builder.Services.AddScoped<WebhookIngestionService>();
builder.Services.AddHostedService<SubscriptionRenewalService>();

// CORS: allow configured frontend origins (falls back to localhost:3000 in development)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if ((allowedOrigins == null || allowedOrigins.Length == 0) && builder.Environment.IsDevelopment())
{
    allowedOrigins = new[] { "http://localhost:3000" };
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins != null && allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Auto-create and migrate the database in development
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }

    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/time", () =>
{
    var now = DateTimeOffset.Now;
    var formattedTime = $"{now:yyyy-MM-dd HH:mm:ss} GMT{now:zzz}";
    return Results.Ok(new { time = formattedTime });
})
.WithName("Time");

// Feature endpoints
app.MapSeriesEndpoints();
app.MapSessionEndpoints();
app.MapMetricsEndpoints();
app.MapWebhookEndpoints();

app.Run();

// Required for WebApplicationFactory in tests
public partial class Program { }
