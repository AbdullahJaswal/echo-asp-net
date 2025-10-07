using System.Text;

using Echo.Data;
using Echo.Options;
using Echo.Services;
using Echo.Services.Abstractions;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Options
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// DB
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var dbOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>().Value;
    if (string.IsNullOrWhiteSpace(dbOptions.ConnectionString))
        throw new InvalidOperationException("Database:ConnectionString is missing.");
    options.UseSqlServer(dbOptions.ConnectionString);
});

// Auth services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// JWT bearer
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ??
          throw new InvalidOperationException("Jwt options missing");
var keyBytes = Encoding.UTF8.GetBytes(jwt.Key);
if (keyBytes.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 bytes (256 bits).");
var key = new SymmetricSecurityKey(keyBytes);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = key,
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("read:users", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.FindFirst("scope")?.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Contains("read:users") == true
            || ctx.User.FindFirst("scp")?.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Contains("read:users") == true));

// Controllers and Configs
builder.Services.AddControllers();

builder.Services.Configure<OpenApiSettings>(builder.Configuration.GetSection("OpenApi"));
builder.Services.AddOpenApi(options =>
{
    // OpenAPI
    options.AddDocumentTransformer((doc, _, _) =>
    {
        var settings = builder.Configuration.GetSection("OpenApi").Get<OpenApiSettings>() ?? new OpenApiSettings();
        doc.Info = new OpenApiInfo
        {
            Title = settings.Title,
            Version = settings.Version,
            Description = settings.Description
        };
        return Task.CompletedTask;
    });

    // Swagger JWT
    options.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Components ??= new OpenApiComponents();
        doc.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Input your Bearer token in the format: Bearer {token}"
        };
        doc.SecurityRequirements.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Dev Configs
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        var settings = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenApiSettings>>().Value;
        options.SwaggerEndpoint($"/openapi/{settings.Version}.json", settings.Title);
        options.DocumentTitle = settings.Title;
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// DB Migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var pending = await db.Database.GetPendingMigrationsAsync();
    var enumerable = pending as string[] ?? pending.ToArray();

    if (enumerable.Length != 0)
    {
        Console.WriteLine($"Applying {enumerable.Length} pending migration(s):");
        foreach (var m in enumerable) Console.WriteLine($" - {m}");
    }
    else
    {
        Console.WriteLine("No pending migrations.");
    }

    await db.Database.MigrateAsync();
    Console.WriteLine("Migrations complete.");
}

app.Run();
