using Microsoft.EntityFrameworkCore;
using Modulo5.Domain.Entities;
using Modulo5.Domain.Exceptions;
using Modulo5.Domain.Repositories;

namespace Modulo5.Data.Repositories;

public class ArticuloRepository : IArticuloRepository
{
    private readonly Modulo5DbContext _context;

    public ArticuloRepository(Modulo5DbContext context)
    {
        _context = context;
    }

    public Task<Articulo?> GetByCodigoAsync(string codigo) =>
        _context.Articulos.FirstOrDefaultAsync(a => a.Codigo == codigo);

    public async Task<Articulo> AddAsync(Articulo articulo)
    {
        _context.Articulos.Add(articulo);
        await SaveChangesTranslatingConstraintViolationsAsync();
        return articulo;
    }

    public async Task UpdateAsync(Articulo articulo)
    {
        _context.Articulos.Update(articulo);
        await SaveChangesTranslatingConstraintViolationsAsync();
    }

    public async Task DeleteAsync(Articulo articulo)
    {
        _context.Articulos.Remove(articulo);
        await _context.SaveChangesAsync();
    }

    private async Task SaveChangesTranslatingConstraintViolationsAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Violación de PK (Código duplicado) → excepción de dominio tipada, capturada por el
            // middleware existente del Block 3 de FEAT-001a (ver spec Block 1, Error handling).
            throw new ValidationException("Ya existe un artículo con el Código ingresado.", ex);
        }
    }
}
