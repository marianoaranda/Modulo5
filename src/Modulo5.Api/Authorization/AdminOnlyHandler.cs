using Microsoft.AspNetCore.Authorization;
using Modulo5.Domain.Repositories;

namespace Modulo5.Api.Authorization;

/// <summary>
/// Autorización REAL de la política "AdminOnly" (Block 4 del spec FEAT-001a — mitigación del riesgo
/// #4 del threat model): un JWT válido no alcanza. Lee el claim <c>PerfilId</c> del JWT (emitido por
/// <c>JwtTokenGenerator</c>, Block 3) y lo compara contra el <c>PerfilId</c> del perfil
/// "administrador" resuelto por CONSULTA a <see cref="IPerfilRepository"/> (no un valor hardcodeado
/// — el seed de la migración inicial pone <c>PerfilId=1</c>, pero esta clase no lo asume: consulta la
/// fila real por <c>Descripcion == "administrador"</c>). Si el requirement no se cumple, ASP.NET Core
/// responde 403 (no 401, porque la autenticación del JWT ya fue válida — el rechazo es de
/// autorización, no de autenticación).
/// </summary>
public class AdminOnlyHandler : AuthorizationHandler<AdminOnlyRequirement>
{
    private const string PerfilIdClaimType = "PerfilId";
    private const string AdministradorDescripcion = "administrador";

    private readonly IPerfilRepository _perfilRepository;

    public AdminOnlyHandler(IPerfilRepository perfilRepository)
    {
        _perfilRepository = perfilRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, AdminOnlyRequirement requirement)
    {
        var perfilIdClaim = context.User.FindFirst(PerfilIdClaimType)?.Value;
        if (perfilIdClaim is null || !int.TryParse(perfilIdClaim, out var perfilId))
        {
            return; // Requirement no cumplido -> el pipeline de autorización deniega (403).
        }

        var administrador = await _perfilRepository.GetByDescripcionAsync(AdministradorDescripcion);
        if (administrador is not null && administrador.PerfilId == perfilId)
        {
            context.Succeed(requirement);
        }
    }
}
