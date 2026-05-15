using ASB.Entitlements.Application;
using ASB.Entitlements.Infrastructure;
using ASB.Entitlements.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() {
        Title = "ASB Entitlements API",
        Version = "v1",
        Description = "Graph-backed entitlement service with Clean Architecture and BIAN compliance"
    });
});

// Add application layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Seed demo data on startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Starting demo data seeding...");
        var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        await seeder.SeedAsync();
        logger.LogInformation("Demo data seeded successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error seeding demo data. Application will continue.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
