using Modulo5.Domain.Entities;

namespace Modulo5.Domain.Repositories;

/// <summary>
/// Puerto de persistencia para <see cref="Usuario"/>. Implementado por <c>Modulo5.Data</c>
/// (UsuarioRepository), respetando la separación de capas: Domain define, Data implementa.
/// </summary>
public interface IUsuarioRepository
{
    Task<Usuario?> GetByIdAsync(int usuarioId);

    Task<Usuario?> GetByUsuarioAsync(string usuario);

    Task<Usuario> AddAsync(Usuario usuario);

    Task UpdateAsync(Usuario usuario);

    Task DeleteAsync(Usuario usuario);
}
