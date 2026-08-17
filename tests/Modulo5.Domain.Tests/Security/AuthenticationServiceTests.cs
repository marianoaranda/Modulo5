using Modulo5.Domain.Entities;
using Modulo5.Domain.Exceptions;
using Modulo5.Domain.Repositories;
using Modulo5.Domain.Security;

namespace Modulo5.Domain.Tests.Security;

/// <summary>
/// Tests de la corrección de seguridad sobre el hallazgo de la ronda 2 de revisión del Block 3 del
/// spec FEAT-001a: <see cref="AuthenticationService.AuthenticateAsync"/> debía invocar
/// <see cref="IPasswordHasher.Verify"/> exactamente una vez tanto si el usuario existe como si no,
/// para eliminar el canal de timing por short-circuit del operador `||` (riesgo #8 del threat
/// model, docs/daw/security/threat-FEAT-001a.md).
/// </summary>
public class AuthenticationServiceTests
{
    private static readonly Usuario UsuarioExistente = new()
    {
        UsuarioId = 1,
        NombreUsuario = "jperez",
        NombreCompleto = "Juan Perez",
        Hash = new byte[] { 1, 2, 3 },
        Salt = new byte[] { 9, 9, 9 },
        PerfilId = 1
    };

    [Fact]
    public async Task Login_con_usuario_inexistente_invoca_Verify_exactamente_una_vez()
    {
        // Arrange — mismo costo computacional que el caso "usuario existe, password incorrecta":
        // sin el fix, el short-circuit de `||` hace que Verify() nunca se llame acá.
        var repositorio = new UsuarioRepositorioFalso(usuario: null);
        var hasher = new PasswordHasherEspia(resultado: false);
        var servicio = new AuthenticationService(repositorio, hasher);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedDomainException>(
            () => servicio.AuthenticateAsync("no-existe", "cualquiera1"));

        Assert.Equal(1, hasher.LlamadasAVerify);
    }

    [Fact]
    public async Task Login_con_password_incorrecta_invoca_Verify_exactamente_una_vez()
    {
        // Arrange — control: el caso "usuario existe" ya invocaba Verify() una vez antes del fix;
        // este test confirma que el fix no lo duplica.
        var repositorio = new UsuarioRepositorioFalso(UsuarioExistente);
        var hasher = new PasswordHasherEspia(resultado: false);
        var servicio = new AuthenticationService(repositorio, hasher);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedDomainException>(
            () => servicio.AuthenticateAsync("jperez", "incorrecta1"));

        Assert.Equal(1, hasher.LlamadasAVerify);
    }

    [Fact]
    public async Task Login_con_usuario_inexistente_devuelve_el_mismo_mensaje_uniforme_AC09()
    {
        // Arrange — comportamiento funcional (AC-09): el mensaje no debe cambiar por el fix.
        var repositorio = new UsuarioRepositorioFalso(usuario: null);
        var hasher = new PasswordHasherEspia(resultado: false);
        var servicio = new AuthenticationService(repositorio, hasher);

        // Act
        var ex = await Assert.ThrowsAsync<UnauthorizedDomainException>(
            () => servicio.AuthenticateAsync("no-existe", "cualquiera1"));

        // Assert
        Assert.Equal("Usuario o contraseña incorrectos", ex.Message);
    }

    [Fact]
    public async Task Login_con_usuario_inexistente_no_usa_el_hash_ni_el_salt_de_ningun_usuario_real()
    {
        // Arrange — Verify() debe invocarse con un hash/salt fijo, no derivado de ningún usuario.
        var repositorio = new UsuarioRepositorioFalso(usuario: null);
        var hasher = new PasswordHasherEspia(resultado: false);
        var servicio = new AuthenticationService(repositorio, hasher);

        // Act
        await Assert.ThrowsAsync<UnauthorizedDomainException>(
            () => servicio.AuthenticateAsync("no-existe", "cualquiera1"));

        // Assert — no puede ser null/vacío ni coincidir con el hash/salt de UsuarioExistente.
        Assert.NotNull(hasher.UltimoHashRecibido);
        Assert.NotNull(hasher.UltimoSaltRecibido);
        Assert.NotEmpty(hasher.UltimoHashRecibido!);
        Assert.NotEmpty(hasher.UltimoSaltRecibido!);
        Assert.NotEqual(UsuarioExistente.Hash, hasher.UltimoHashRecibido);
        Assert.NotEqual(UsuarioExistente.Salt, hasher.UltimoSaltRecibido);
    }

    [Fact]
    public async Task Login_con_credenciales_validas_sigue_devolviendo_el_usuario()
    {
        // Arrange — regresión: el fix no debe romper el happy path.
        var repositorio = new UsuarioRepositorioFalso(UsuarioExistente);
        var hasher = new PasswordHasherEspia(resultado: true);
        var servicio = new AuthenticationService(repositorio, hasher);

        // Act
        var resultado = await servicio.AuthenticateAsync("jperez", "Passw0rd1");

        // Assert
        Assert.Same(UsuarioExistente, resultado);
        Assert.Equal(1, hasher.LlamadasAVerify);
    }

    /// <summary>Doble de prueba: no hay librería de mocking en este proyecto de tests.</summary>
    private sealed class UsuarioRepositorioFalso : IUsuarioRepository
    {
        private readonly Usuario? _usuario;

        public UsuarioRepositorioFalso(Usuario? usuario) => _usuario = usuario;

        public Task<Usuario?> GetByIdAsync(int usuarioId) => Task.FromResult(_usuario);

        public Task<Usuario?> GetByUsuarioAsync(string usuario) => Task.FromResult(_usuario);

        public Task<Usuario> AddAsync(Usuario usuario) => throw new NotSupportedException();

        public Task UpdateAsync(Usuario usuario) => throw new NotSupportedException();

        public Task DeleteAsync(Usuario usuario) => throw new NotSupportedException();
    }

    /// <summary>Espía de <see cref="IPasswordHasher"/>: cuenta llamadas y registra los argumentos.</summary>
    private sealed class PasswordHasherEspia : IPasswordHasher
    {
        private readonly bool _resultado;

        public PasswordHasherEspia(bool resultado) => _resultado = resultado;

        public int LlamadasAVerify { get; private set; }

        public byte[]? UltimoHashRecibido { get; private set; }

        public byte[]? UltimoSaltRecibido { get; private set; }

        public (byte[] Hash, byte[] Salt) Hash(string password) => throw new NotSupportedException();

        public bool Verify(string password, byte[] hash, byte[] salt)
        {
            LlamadasAVerify++;
            UltimoHashRecibido = hash;
            UltimoSaltRecibido = salt;
            return _resultado;
        }
    }
}
