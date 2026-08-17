namespace Modulo5.Web.Services;

/// <summary>Refleja el body de error uniforme que emite <c>ExceptionHandlingMiddleware</c> de la Api:
/// <c>{ "mensaje": string }</c>.</summary>
public record ApiErrorDto(string Mensaje);
