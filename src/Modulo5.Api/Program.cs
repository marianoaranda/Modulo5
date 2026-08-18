using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Modulo5.Api.Authorization;
using Modulo5.Api.Middleware;
using Modulo5.Api.Security;
using Modulo5.Data;
using Modulo5.Data.Repositories;
using Modulo5.Domain.Repositories;
using Modulo5.Domain.Security;

var builder = WebApplication.CreateBuilder(args);

// --- Secretos: SOLO desde configuración (user-secrets en desarrollo, variables de entorno en
// producción) — NUNCA hardcodeados ni con un valor por defecto inseguro en el código (mitigación
// del riesgo #1 del threat model). Falla rápido si faltan, tanto en runtime real como en tests.
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtSigningKey))
{
    throw new InvalidOperationException(
        "La clave de firma JWT no está configurada. En desarrollo: 'dotnet user-secrets set " +
        "\"Jwt:SigningKey\" \"<clave>\"' dentro de src/Modulo5.Api. En producción: variable de " +
        "entorno Jwt__SigningKey.");
}

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "La connection string no está configurada. En desarrollo: 'dotnet user-secrets set " +
        "\"ConnectionStrings:Default\" \"<cadena>\"' dentro de src/Modulo5.Api. En producción: " +
        "variable de entorno ConnectionStrings__Default.");
}

// --- Servicios ---
builder.Services.AddDbContext<Modulo5DbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IPerfilRepository, PerfilRepository>();
builder.Services.AddScoped<IArticuloRepository, ArticuloRepository>();
builder.Services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<JwtTokenGenerator>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

// Política "AdminOnly" (Block 4 del spec FEAT-001a — mitigación del riesgo #4 del threat model):
// autorización explícita por PerfilId, no solo "tiene JWT válido". Evaluación real en
// AdminOnlyHandler, resuelta contra el perfil "administrador" en la base (no un id hardcodeado).
builder.Services.AddScoped<IAuthorizationHandler, AdminOnlyHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.Requirements.Add(new AdminOnlyRequirement()));
});

// Rate limiting nativo de .NET 8 (Microsoft.AspNetCore.RateLimiting, sin paquete nuevo) — ventana
// fija de 5 requests/minuto por IP sobre POST /api/auth/login (mitigación del riesgo #7).
builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { mensaje = "Demasiados intentos, intente nuevamente en unos minutos." },
            cancellationToken);
    };

    options.AddPolicy("login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

builder.Services.AddControllers();

var app = builder.Build();

// --- Pipeline ---
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Necesario para que WebApplicationFactory<Program> (tests/Modulo5.Api.Tests) pueda referenciar
// esta clase — con top-level statements, el compilador la genera `internal` por defecto.
public partial class Program
{
}
