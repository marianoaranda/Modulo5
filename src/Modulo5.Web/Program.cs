using Modulo5.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Configuración: URL base de Modulo5.Api. No es un secreto (no es connection string ni clave de
// firma), así que vive en appsettings.json/appsettings.Development.json como cualquier otra
// configuración no sensible — a diferencia de Jwt:SigningKey/ConnectionStrings:Default en
// Modulo5.Api, que SOLO vienen de user-secrets/variables de entorno (mitigación del riesgo #1 del
// threat model, ver Program.cs de Modulo5.Api).
var apiBaseUrl = builder.Configuration["ApiClient:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException(
        "La URL base de Modulo5.Api (ApiClient:BaseUrl) no está configurada en appsettings.json.");
}

// --- Servicios ---
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

var app = builder.Build();

// --- Pipeline ---
// HTTPS enforcement + página de error genérica fuera de Development (spec Block 5, "Files" —
// Program.cs; mismo patrón que Modulo5.Api/Program.cs, Block 3).
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();

app.UseRouting();

// Deliberadamente SIN UseAuthentication()/UseAuthorization() ni [Authorize]: Modulo5.Web no decide
// autorización por sí mismo, solo refleja lo que Modulo5.Api resuelve (spec Block 5, "Logic") — ver
// UsuariosController, que reacciona a 401/403 de la respuesta HTTP real de la Api.

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();

// Necesario para que un futuro WebApplicationFactory<Program> pueda referenciar esta clase — con
// top-level statements, el compilador la genera `internal` por defecto (mismo patrón que
// Modulo5.Api/Program.cs).
public partial class Program
{
}
