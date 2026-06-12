using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using AscensoresIruna.Api.Data;
using AscensoresIruna.Api.Services;
using AscensoresIruna.Api.Middleware;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1024; // 1 KB
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=data/ascensores.db"));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IpHashService>();
builder.Services.AddScoped<ElevatorStatusService>();
builder.Services.AddScoped<TrustScoreService>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.Configure<Microsoft.AspNetCore.HostFiltering.HostFilteringOptions>(options =>
{
    var allowedHosts = builder.Configuration["AllowedHosts"] ?? "*";
    options.AllowedHosts = allowedHosts.Split(';', StringSplitOptions.RemoveEmptyEntries);
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(builder =>
    {
        builder.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "Ha ocurrido un error interno." });
        });
    });
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseHostFiltering();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline' https://unpkg.com; img-src 'self' data: https://*.tile.openstreetmap.org https://unpkg.com; connect-src 'self'";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("AllowFrontend");
}

app.UseMiddleware<RateLimitMiddleware>();

app.UseAuthorization();

app.MapControllers();

var dataDir = Path.Combine(builder.Environment.ContentRootPath, "data");
if (!Directory.Exists(dataDir))
    Directory.CreateDirectory(dataDir);

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
    SeedData.Initialize(scope.ServiceProvider);
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();