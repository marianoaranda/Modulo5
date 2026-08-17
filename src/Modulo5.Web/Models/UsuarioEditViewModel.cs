using System.ComponentModel.DataAnnotations;

namespace Modulo5.Web.Models;

/// <summary>
/// ViewModel de <c>Views/Usuarios/Edit.cshtml</c> (spec Block 5). Igual que <c>UsuarioRequest</c> del
/// lado de la Api (Block 4): <c>Usuario</c> (username) NO es editable por contrato del bloque y
/// <c>Password</c> es opcional (si se deja vacío, la Api conserva el Hash/Salt existente).
///
/// ASSUMPTION documentada en el reporte del bloque: el contrato de la Api (Block 4) no expone ningún
/// endpoint <c>GET</c> para recuperar un usuario existente por Id, así que esta vista NO puede
/// pre-cargar <c>NombreCompleto</c>/<c>PerfilId</c> actuales — el administrador debe reingresar los
/// valores completos (igual que exige el propio <c>PUT</c> de la Api, que no admite un patch parcial).
/// </summary>
public class UsuarioEditViewModel
{
    public int UsuarioId { get; set; }

    [Required(ErrorMessage = "El nombre completo es requerido.")]
    [StringLength(150, ErrorMessage = "El nombre completo debe tener máximo 150 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Opcional: si se deja vacío, la Api conserva la contraseña actual.</summary>
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres alfanuméricos.")]
    [RegularExpression(
        "^[a-zA-Z0-9]*$",
        ErrorMessage = "La contraseña debe tener al menos 8 caracteres alfanuméricos.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña (opcional)")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "El perfil es requerido.")]
    [Range(1, int.MaxValue, ErrorMessage = "El PerfilId debe ser un número positivo.")]
    [Display(Name = "PerfilId")]
    public int PerfilId { get; set; }
}
