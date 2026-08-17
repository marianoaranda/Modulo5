namespace Modulo5.Api.Dtos;

/// <summary>
/// Response 200 de <c>POST /api/auth/login</c> (Block 3, "API contract"):
/// <c>{ "token": string, "expiraEn": "&lt;ISO8601&gt;" }</c>. El serializador JSON por defecto de
/// ASP.NET Core usa camelCase, así que `Token`/`ExpiraEn` se emiten como `token`/`expiraEn` y
/// `DateTime` se serializa en ISO 8601 sin configuración adicional.
/// </summary>
public class LoginResponse
{
    public LoginResponse(string token, DateTime expiraEn)
    {
        Token = token;
        ExpiraEn = expiraEn;
    }

    public string Token { get; }

    public DateTime ExpiraEn { get; }
}
