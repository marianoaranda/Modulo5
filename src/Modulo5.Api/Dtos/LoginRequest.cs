using System.ComponentModel.DataAnnotations;

namespace Modulo5.Api.Dtos;

/// <summary>
/// Request de <c>POST /api/auth/login</c> (Block 3, "API contract"). `Password` no tiene límite
/// superior de longitud: se hashea, nunca se persiste tal cual (Input validation del bloque).
/// </summary>
public class LoginRequest
{
    [Required]
    [MaxLength(50)]
    public string Usuario { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
