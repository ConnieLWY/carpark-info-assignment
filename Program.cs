using HandshakesByDC_BEAssignment;
using HandshakesByDC_BEAssignment.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Trace);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    // Configure V1
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Carpark API",
        Version = "v1",
        Description = "Original version of the Carpark API"
    });

    // Configure V2
    c.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "Carpark API",
        Version = "v2",
        Description = "Authenticated endpoints of the Carpark API"
    });

    // Configure document filters
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (!apiDesc.TryGetMethodInfo(out var methodInfo)) return false;

        if (docName == "v1")
            return !methodInfo.DeclaringType.FullName.Contains(".v2.");

        if (docName == "v2")
            return methodInfo.DeclaringType.FullName.Contains(".v2.");

        return false;
    });

    // Add JWT Authentication support in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme.\r\n\r\nEnter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\""
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
});

builder.Services.AddSingleton<SampleData>();
builder.Services.AddSingleton<IFileReaderStrategy>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var fileFormat = configuration["FileFormat"];

    return fileFormat.ToLower() switch
    {
        "csv" => new CsvFileReaderStrategy(),
        "json" => new JsonFileReaderStrategy(),
        _ => throw new ArgumentException($"Unsupported file format: {fileFormat}")
    };
});
builder.Services.AddHostedService<BackgroundRefresh>();

// Add DbContext configuration
var connection = builder.Configuration["ConnectionSqlite:SqliteConnectionString"];
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(connection));

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false,  // Changed to false since there's no aud claim
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],  // Should match "youtCompanyIssuer.com"
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Carpark API V1 (Public)");
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "Carpark API V2 (Authenticated)");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();