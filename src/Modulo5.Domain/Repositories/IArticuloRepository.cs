using Modulo5.Domain.Entities;

namespace Modulo5.Domain.Repositories;

/// <summary>
/// Puerto de persistencia para <see cref="Articulo"/>. Implementado por <c>Modulo5.Data</c>
/// (ArticuloRepository), respetando la separación de capas: Domain define, Data implementa.
/// </summary>
public interface IArticuloRepository
{
    Task<Articulo?> GetByCodigoAsync(string codigo);

    Task<Articulo> AddAsync(Articulo articulo);

    Task UpdateAsync(Articulo articulo);

    Task DeleteAsync(Articulo articulo);
}
