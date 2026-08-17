namespace Modulo5.Api.Dtos;

/// <summary>
/// Request compartido de <c>POST /api/usuarios</c> y <c>PUT /api/usuarios/{id}</c> (Block 4, "API
/// contract"). Deliberadamente SIN Data Annotations (`[Required]`/`[MaxLength]`): el contrato del
/// bloque exige que <see cref="Usuario"/> NO se envíe en el PUT (username inmutable) y que
/// <see cref="Password"/> sea opcional en el PUT pero obligatorio en el POST — dos reglas que no
/// pueden expresarse con un único juego de atributos sobre una clase compartida sin que el binding
/// automático de <c>[ApiController]</c> rechace uno de los dos casos antes de llegar a la acción. La
/// validación real se hace en <c>UsuariosController</c>, lanzando <c>ValidationException</c> (mismo
/// patrón que <c>PasswordPolicy.Validate</c>, capturada por el middleware del Block 3).
/// </summary>
public class UsuarioRequest
{
    /// <summary>Requerido en el alta (POST). Ignorado en la modificación (PUT): el username no es
    /// editable por contrato del bloque.</summary>
    public string? Usuario { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Requerido en el alta (POST). Opcional en la modificación (PUT): si no viene (null o
    /// vacío), se conserva el Hash/Salt existente.</summary>
    public string? Password { get; set; }

    public int PerfilId { get; set; }
}
