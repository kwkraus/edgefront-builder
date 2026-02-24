using EdgeFront.Builder.Api.Domain;
using Nager.PublicSuffix;
using Nager.PublicSuffix.RuleProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplicationInsightsTelemetry();

// Domain identity services per SPEC-010
builder.Services.AddSingleton<IDomainParser>(sp =>
{
    var pslPath = Path.Combine(AppContext.BaseDirectory, "Resources", "public_suffix_list.dat");
    var ruleProvider = new LocalFileRuleProvider(pslPath);
    ruleProvider.BuildAsync().GetAwaiter().GetResult();
    return new DomainParser(ruleProvider);
});

var internalDomains = builder.Configuration
    .GetSection("InternalDomains")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddSingleton(sp =>
    new DomainNormalizer(sp.GetRequiredService<IDomainParser>(), internalDomains));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/api/time", () =>
{
    var now = DateTimeOffset.Now;
    var formattedTime = $"{now:yyyy-MM-dd HH:mm:ss} GMT{now:zzz}";
    return Results.Ok(new { time = formattedTime });
})
.WithName("Time");

app.Run();
