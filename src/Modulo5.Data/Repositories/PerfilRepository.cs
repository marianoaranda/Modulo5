using Microsoft.EntityFrameworkCore;
using Modulo5.Domain.Entities;
using Modulo5.Domain.Repositories;

namespace Modulo5.Data.Repositories;

public class PerfilRepository : IPerfilRepository
{
    private readonly Modulo5DbContext _context;

    public PerfilRepository(Modulo5DbContext context)
    {
        _context = context;
    }

    public Task<Perfil?> GetByIdAsync(int perfilId) =>
        _context.Perfiles.FirstOrDefaultAsync(p => p.PerfilId == perfilId);

    public Task<Perfil?> GetByDescripcionAsync(string descripcion) =>
        _context.Perfiles.FirstOrDefaultAsync(p => p.Descripcion == descripcion);
}
