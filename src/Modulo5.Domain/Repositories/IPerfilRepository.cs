using Modulo5.Domain.Entities;

namespace Modulo5.Domain.Repositories;

/// <summary>
/// Puerto de persistencia para <see cref="Perfil"/>. Solo lectura: este ticket no expone ABM de
/// Perfiles (FR-10, fuera de alcance el alta/baja/modificación).
/// </summary>
public interface IPerfilRepository
{
    Task<Perfil?> GetByIdAsync(int perfilId);

    Task<Perfil?> GetByDescripcionAsync(string descripcion);
}
