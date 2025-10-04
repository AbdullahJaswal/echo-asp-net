using Echo.Options;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

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

app.MapGet("/ping", () => "pong").WithOpenApi();

app.Run();