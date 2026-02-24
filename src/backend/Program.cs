using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Features.Metrics;
using EdgeFront.Builder.Features.Series;
using EdgeFront.Builder.Features.Sessions;
using EdgeFront.Builder.Features.Webhook;
using EdgeFront.Builder.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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
