using System.ComponentModel.DataAnnotations;

namespace Modulo5.Web.Models;

/// <summary>
/// ViewModel de <c>Views/Usuarios/Create.cshtml</c> (spec Block 5, "Input validation": Data
/// Annotations que replican las mismas reglas del Block 4 — <c>usuario</c> máx. 50, <c>nombreCompleto</c>
/// máx. 150, y la política de contraseña de <c>PasswordPolicy</c> del Block 2 — defensa en profundidad
/// client+server; la validación real sigue viviendo en la Api).
/// </summary>
public class UsuarioCreateViewModel
{
    [Required(ErrorMessage = "El nombre de usuario es requerido.")]
    [StringLength(50, ErrorMessage = "El nombre de usuario debe tener máximo 50 caracteres.")]
    [Display(Name = "Usuario")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre completo es requerido.")]
    [StringLength(150, ErrorMessage = "El nombre completo debe tener máximo 150 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres alfanuméricos.")]
    [RegularExpression("^[a-zA-Z0-9]+$", ErrorMessage = "La contraseña debe tener al menos 8 caracteres alfanuméricos.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "El perfil es requerido.")]
    [Range(1, int.MaxValue, ErrorMessage = "El PerfilId debe ser un número positivo.")]
    [Display(Name = "PerfilId")]
    public int PerfilId { get; set; }
}
