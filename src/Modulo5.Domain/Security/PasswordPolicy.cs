using System.Text.RegularExpressions;
using Modulo5.Domain.Exceptions;

namespace Modulo5.Domain.Security;

/// <summary>
/// Política de contraseñas: longitud mínima de 8 caracteres, solo alfanuméricos (FR-06/AC-06/AC-07
/// del PRD FEAT-001a).
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const string ErrorMessage = "La contraseña debe tener al menos 8 caracteres alfanuméricos.";

    private static readonly Regex AlphanumericRegex = new("^[a-zA-Z0-9]+$", RegexOptions.Compiled);

    /// <summary>
    /// Indica si <paramref name="password"/> cumple la política (longitud ≥ <see cref="MinLength"/>
    /// y solo caracteres alfanuméricos).
    /// </summary>
    public static bool IsValid(string password) =>
        !string.IsNullOrEmpty(password)
        && password.Length >= MinLength
        && AlphanumericRegex.IsMatch(password);

    /// <summary>
    /// Valida <paramref name="password"/> contra la política y lanza <see cref="ValidationException"/>
    /// con el mensaje exacto de AC-06 si no la cumple. Los bloques que reciben una contraseña como
    /// input externo (alta/modificación de usuario, Block 3/4) llaman a este método en vez de
    /// reimplementar el mensaje de error.
    /// </summary>
    public static void Validate(string password)
    {
        if (!IsValid(password))
        {
            throw new ValidationException(ErrorMessage);
        }
    }
}
