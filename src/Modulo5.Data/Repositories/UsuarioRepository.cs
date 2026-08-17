using Microsoft.EntityFrameworkCore;
using Modulo5.Domain.Entities;
using Modulo5.Domain.Exceptions;
using Modulo5.Domain.Repositories;

namespace Modulo5.Data.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly Modulo5DbContext _context;

    public UsuarioRepository(Modulo5DbContext context)
    {
        _context = context;
    }

    public Task<Usuario?> GetByIdAsync(int usuarioId) =>
        _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);

    public Task<Usuario?> GetByUsuarioAsync(string usuario) =>
        _context.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == usuario);

    public async Task<Usuario> AddAsync(Usuario usuario)
    {
        _context.Usuarios.Add(usuario);
        await SaveChangesTranslatingConstraintViolationsAsync();
        return usuario;
    }

    public async Task UpdateAsync(Usuario usuario)
    {
        _context.Usuarios.Update(usuario);
        await SaveChangesTranslatingConstraintViolationsAsync();
    }

    public async Task DeleteAsync(Usuario usuario)
    {
        _context.Usuarios.Remove(usuario);
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
            // Violación de constraint de EF Core (p. ej. IX_Usuario_Usuario duplicado) → excepción
            // de dominio tipada, capturada por el middleware del Block 3 (ver spec Block 1, Error
            // handling).
            throw new ValidationException("El nombre de usuario ya existe.", ex);
        }
    }
}
