namespace Modulo5.Web.Services;

/// <summary>Refleja <c>Modulo5.Api.Dtos.UsuarioRequest</c> — mismo motivo por el que ese DTO no usa
/// Data Annotations: <see cref="Usuario"/> se envía solo en el alta (ignorado en la modificación,
/// username inmutable) y <see cref="Password"/> es obligatorio en el alta pero opcional en la
/// modificación.</summary>
public record UsuarioRequestDto(string? Usuario, string NombreCompleto, string? Password, int PerfilId);
