namespace Modulo5.Api.Dtos;

/// <summary>
/// Response de <c>POST</c>/<c>PUT /api/usuarios</c> (Block 4, "API contract"). DTO explícito que
/// NUNCA incluye <c>Hash</c> ni <c>Salt</c> — mitigación del riesgo #5 del threat model
/// (docs/daw/security/threat-FEAT-001a.md): un endpoint jamás devuelve la entidad <c>Usuario</c>
/// completa.
/// </summary>
public class UsuarioResponse
{
    public UsuarioResponse(int usuarioId, string usuario, string nombreCompleto, int perfilId)
    {
        UsuarioId = usuarioId;
        Usuario = usuario;
        NombreCompleto = nombreCompleto;
        PerfilId = perfilId;
    }

    public int UsuarioId { get; }

    public string Usuario { get; }

    public string NombreCompleto { get; }

    public int PerfilId { get; }
}
