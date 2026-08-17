using System.ComponentModel.DataAnnotations;

namespace Modulo5.Web.Models;

/// <summary>ViewModel de <c>Views/Account/Login.cshtml</c> (spec Block 5). Replica el límite de
/// <c>Usuario</c> del contrato de la Api (Block 3, <c>LoginRequest</c>: máx. 50 caracteres) como
/// defensa en profundidad — la validación real sigue viviendo en la Api.</summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "El usuario es requerido.")]
    [StringLength(50, ErrorMessage = "El usuario debe tener máximo 50 caracteres.")]
    [Display(Name = "Usuario")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;
}
