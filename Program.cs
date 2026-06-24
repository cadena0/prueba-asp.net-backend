using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using RentasApi.Data;
using RentasApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar la conexión a PostgreSQL con EF Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Configurar CORS para permitir peticiones desde tu Frontend MVC
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // Añade aquí los puertos exactos que te genere tu proyecto MVC al ejecutarlo
        policy.WithOrigins("http://localhost:5173", "https://localhost:7123", "http://localhost:5247", "http://localhost:5000", "https://localhost:5001") 
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Register auth service
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure JWT authentication
var jwtKey = builder.Configuration["Jwt:Key"];
if (!string.IsNullOrEmpty(jwtKey))
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
}

var app = builder.Build();

// 3. Seed inicial del propietario en la base de datos si no existe
using (var scope = app.Services.CreateScope())
{
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

    var ownerEmail = configuration["InitialOwner:Email"];
    var ownerPassword = configuration["InitialOwner:Password"];
    var ownerFullName = configuration["InitialOwner:FullName"] ?? "Propietario";

    if (!string.IsNullOrEmpty(ownerEmail) && !string.IsNullOrEmpty(ownerPassword))
    {
        var existingOwner = await db.Users.FirstOrDefaultAsync(u => u.Role == "OWNER" || u.Email == ownerEmail);
        if (existingOwner == null)
        {
            var request = new RentasApi.DTOs.RegisterRequest
            {
                FullName = ownerFullName,
                Email = ownerEmail,
                Password = ownerPassword
            };

            var result = await authService.RegisterAsync(request);
            if (!result.Success)
            {
                Console.WriteLine($"Initial owner seed failed: {result.Message}");
            }
            else
            {
                Console.WriteLine($"Initial owner created: {ownerEmail}");
            }
        }
    }
}

// 4. Usar la política CORS antes de los controladores
app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();