namespace Modulo5.Web.Services;

/// <summary>
/// DTO propio de <c>Modulo5.Web</c> que refleja el contrato HTTP de <c>Modulo5.Api</c> (Block 3/4
/// del spec FEAT-001a). Deliberadamente NO se referencia el proyecto <c>Modulo5.Api</c> desde acá
/// (AGENTS.md — "Web nunca habla directo con Data; siempre a través de Api", y la comunicación entre
/// ambos proyectos web-facing es exclusivamente HTTP, no una referencia de proyecto): estos tipos son
/// la copia local, del lado del cliente, de lo que la Api documenta en sus propios DTOs.
/// </summary>
public record LoginResultDto(string Token, DateTime ExpiraEn);
