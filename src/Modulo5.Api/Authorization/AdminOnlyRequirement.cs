using Microsoft.AspNetCore.Authorization;

namespace Modulo5.Api.Authorization;

/// <summary>
/// Marcador de la política "AdminOnly" (Block 4 del spec FEAT-001a). Sin datos propios: la lógica de
/// evaluación real vive en <see cref="AdminOnlyHandler"/>.
/// </summary>
public class AdminOnlyRequirement : IAuthorizationRequirement
{
}
