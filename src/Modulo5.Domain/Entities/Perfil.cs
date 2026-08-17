namespace Modulo5.Domain.Entities;

/// <summary>
/// Perfil de seguridad de un Usuario (p. ej. "administrador"). No tiene ABM propio en este ticket
/// (FR-10): se precarga por seed de migración.
/// </summary>
public class Perfil
{
    public int PerfilId { get; set; }

    public string Descripcion { get; set; } = string.Empty;
}
