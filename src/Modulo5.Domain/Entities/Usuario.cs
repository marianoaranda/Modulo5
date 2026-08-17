namespace Modulo5.Domain.Entities;

/// <summary>
/// Usuario del sistema. La contraseña nunca se guarda en texto plano: solo <see cref="Hash"/> y
/// <see cref="Salt"/> (ver Block 2 del spec para el algoritmo de hashing).
/// </summary>
public class Usuario
{
    public int UsuarioId { get; set; }

    // NOTA: el spec (tabla "Data model" del Block 1) nombra este campo "Usuario" — igual al nombre
    // de la entidad, lo cual C# no permite (CS0542: un miembro no puede llamarse igual que su tipo
    // contenedor). Se resuelve renombrando la propiedad a NombreUsuario y mapeándola a la columna
    // "Usuario" (ver UsuarioConfiguration), preservando el esquema de datos exacto del spec.
    public string NombreUsuario { get; set; } = string.Empty;

    public string NombreCompleto { get; set; } = string.Empty;

    public byte[] Hash { get; set; } = Array.Empty<byte>();

    public byte[] Salt { get; set; } = Array.Empty<byte>();

    public int PerfilId { get; set; }

    public Perfil? Perfil { get; set; }
}
