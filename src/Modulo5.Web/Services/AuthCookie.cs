namespace Modulo5.Web.Services;

/// <summary>
/// Nombre de la cookie donde <c>AccountController</c> guarda el JWT y de la que <see cref="ApiClient"/>
/// lo lee para reenviarlo como <c>Authorization: Bearer</c> (spec Block 5, "Logic" — mitigación del
/// riesgo #2 del threat model: el JWT nunca vive en <c>localStorage</c> ni es accesible desde JS).
/// Centralizado acá para que ambas clases no dupliquen el literal.
/// </summary>
public static class AuthCookie
{
    public const string Name = "jwt_token";
}
