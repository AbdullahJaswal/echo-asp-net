using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Echo.Data;
using Echo.Options;

var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    options.UseSqlServer(dbOptions.ConnectionString);
});

// OpenAPI
builder.Services.Configure<OpenApiSettings>(builder.Configuration.GetSection("OpenApi"));
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((doc, _, _) =>
    {
        var settings = builder.Configuration
            .GetSection("OpenApi")
            .Get<OpenApiSettings>() ?? new OpenApiSettings();
        doc.Info = new OpenApiInfo
        {
            Title = settings.Title,
            Version = settings.Version,
            Description = settings.Description
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Development Environment
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        var settings = app.Services.GetRequiredService<IOptions<OpenApiSettings>>().Value;
        options.SwaggerEndpoint($"/openapi/{settings.Version}.json", settings.Title);
        options.DocumentTitle = settings.Title;
    });
}

// DB Migration
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.MapGet("/ping", () => "pong").WithOpenApi();

app.Run();