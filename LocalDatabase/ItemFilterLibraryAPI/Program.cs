using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ItemFilterLibraryAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JWT authentication
var jwtSection = builder.Configuration.GetSection("JWT");
var secretKey = jwtSection["SecretKey"] ?? "your-super-secret-key-change-in-production";
var issuer = jwtSection["Issuer"] ?? "ItemFilterLibraryAPI";
var audience = jwtSection["Audience"] ?? "ItemFilterLibraryAPI";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Configure database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? 
                      "Data Source=itemfilterlibrary.db";

// Register services
builder.Services.AddSingleton<DatabaseService>(provider => new DatabaseService(connectionString));
builder.Services.AddScoped<AuthenticationService>();

// Add CORS for local development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Copy database schema to output directory
var schemaSource = Path.Combine(Directory.GetCurrentDirectory(), "..", "Database.sql");
var schemaDestination = Path.Combine(Directory.GetCurrentDirectory(), "Database.sql");
if (File.Exists(schemaSource) && !File.Exists(schemaDestination))
{
    File.Copy(schemaSource, schemaDestination);
}

// Clean up expired sessions on startup
using (var scope = app.Services.CreateScope())
{
    var databaseService = scope.ServiceProvider.GetRequiredService<DatabaseService>();
    await databaseService.CleanupExpiredSessionsAsync();
}

// Welcome message
app.MapGet("/", () => 
{
    return Results.Json(new 
    { 
        message = "ItemFilterLibrary Local API Server", 
        version = "1.0.0",
        endpoints = new
        {
            login = "/auth/discord/login",
            test_auth = "/auth/test",
            template_types = "/templates/types/list",
            swagger = "/swagger"
        }
    });
});

app.Run(); 