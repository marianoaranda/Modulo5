using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modulo5.Api.Dtos;
using Modulo5.Domain.Entities;
using Modulo5.Domain.Exceptions;
using Modulo5.Domain.Repositories;
using Modulo5.Domain.Security;

namespace Modulo5.Api.Controllers;

/// <summary>
/// ABM de Usuarios (Block 4 del spec FEAT-001a). Todas las acciones exigen la política "AdminOnly"
/// (mitigación del riesgo #4 del threat model, FR-07/AC-08): un JWT válido no alcanza, se exige
/// además que el <c>PerfilId</c> del actor sea el del perfil "administrador". Cada operación exitosa
/// loguea el <c>UsuarioId</c> del actor + timestamp (mitigación de Repudiation del threat model).
/// </summary>
[ApiController]
[Route("api/usuarios")]
[Authorize(Policy = "AdminOnly")]
public class UsuariosController : ControllerBase
{
    private const string UsuarioIdClaimType = "UsuarioId";

    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPerfilRepository _perfilRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UsuariosController> _logger;

    public UsuariosController(
        IUsuarioRepository usuarioRepository,
        IPerfilRepository perfilRepository,
        IPasswordHasher passwordHasher,
        ILogger<UsuariosController> logger)
    {
        _usuarioRepository = usuarioRepository;
        _perfilRepository = perfilRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// <c>POST /api/usuarios</c> — alta de usuario (FR-01/AC-01). <c>Password</c> es obligatorio acá
    /// (a diferencia del PUT): se valida con <see cref="PasswordPolicy"/> y se hashea antes de
    /// persistir. El <c>Usuario</c> duplicado lo traduce a <see cref="ValidationException"/> el
    /// repositorio (violación del índice único, ver Block 1), no este método.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UsuarioResponse>> Create([FromBody] UsuarioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Usuario) || request.Usuario.Length > 50)
        {
            throw new ValidationException(
                "El nombre de usuario es requerido y debe tener máximo 50 caracteres.");
        }

        ValidateNombreCompleto(request.NombreCompleto);

        PasswordPolicy.Validate(request.Password ?? string.Empty);

        await EnsurePerfilExistsAsync(request.PerfilId);

        var (hash, salt) = _passwordHasher.Hash(request.Password!);

        var usuario = new Usuario
        {
            NombreUsuario = request.Usuario,
            NombreCompleto = request.NombreCompleto,
            Hash = hash,
            Salt = salt,
            PerfilId = request.PerfilId
        };

        var creado = await _usuarioRepository.AddAsync(usuario);

        LogOperacionExitosa("alta", creado.UsuarioId);

        return StatusCode(StatusCodes.Status201Created, MapToResponse(creado));
    }

    /// <summary>
    /// <c>PUT /api/usuarios/{id}</c> — modificación de usuario (FR-03/AC-03). <c>Usuario</c> no
    /// es editable por contrato del bloque (no se lee de <paramref name="request"/>). <c>Password</c>
    /// es opcional: si viene, se revalida y re-hashea; si no, se conserva el Hash/Salt existente.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UsuarioResponse>> Update(int id, [FromBody] UsuarioRequest request)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"No existe el usuario con UsuarioId {id}.");

        ValidateNombreCompleto(request.NombreCompleto);

        await EnsurePerfilExistsAsync(request.PerfilId);

        usuario.NombreCompleto = request.NombreCompleto;
        usuario.PerfilId = request.PerfilId;

        if (!string.IsNullOrEmpty(request.Password))
        {
            PasswordPolicy.Validate(request.Password);
            var (hash, salt) = _passwordHasher.Hash(request.Password);
            usuario.Hash = hash;
            usuario.Salt = salt;
        }

        await _usuarioRepository.UpdateAsync(usuario);

        LogOperacionExitosa("modificación", usuario.UsuarioId);

        return Ok(MapToResponse(usuario));
    }

    /// <summary>
    /// <c>DELETE /api/usuarios/{id}</c> — baja de usuario (FR-02/AC-02).
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"No existe el usuario con UsuarioId {id}.");

        await _usuarioRepository.DeleteAsync(usuario);

        LogOperacionExitosa("baja", usuario.UsuarioId);

        return NoContent();
    }

    private static void ValidateNombreCompleto(string nombreCompleto)
    {
        if (string.IsNullOrWhiteSpace(nombreCompleto) || nombreCompleto.Length > 150)
        {
            throw new ValidationException(
                "El nombre completo es requerido y debe tener máximo 150 caracteres.");
        }
    }

    private async Task EnsurePerfilExistsAsync(int perfilId)
    {
        var perfil = await _perfilRepository.GetByIdAsync(perfilId);
        if (perfil is null)
        {
            throw new ValidationException($"El perfil {perfilId} no existe.");
        }
    }

    /// <summary>
    /// Mitigación de Repudiation (threat model): loguea el <c>UsuarioId</c> del actor —tomado del
    /// claim del JWT, no del body— + timestamp en cada operación exitosa de ABM de Usuarios.
    /// </summary>
    private void LogOperacionExitosa(string operacion, int usuarioAfectadoId)
    {
        var actorId = User.FindFirst(UsuarioIdClaimType)?.Value ?? "desconocido";
        _logger.LogInformation(
            "ABM Usuarios: {Operacion} sobre UsuarioId {UsuarioAfectadoId} por actor UsuarioId " +
            "{ActorId} a las {Timestamp:o}",
            operacion,
            usuarioAfectadoId,
            actorId,
            DateTime.UtcNow);
    }

    private static UsuarioResponse MapToResponse(Usuario usuario) =>
        new(usuario.UsuarioId, usuario.NombreUsuario, usuario.NombreCompleto, usuario.PerfilId);
}
