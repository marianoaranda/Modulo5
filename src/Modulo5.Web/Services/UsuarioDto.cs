namespace Modulo5.Web.Services;

/// <summary>Refleja <c>Modulo5.Api.Dtos.UsuarioResponse</c> — nunca incluye Hash/Salt porque la Api
/// tampoco los expone (mitigación del riesgo #5 del threat model).</summary>
public record UsuarioDto(int UsuarioId, string Usuario, string NombreCompleto, int PerfilId);
