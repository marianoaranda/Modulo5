namespace Modulo5.Web.Services;

/// <summary>Refleja <c>Modulo5.Api.Dtos.ArticuloRequest</c> — mismo motivo por el que ese DTO no usa
/// Data Annotations: <see cref="Codigo"/> se envía solo en el alta (en la modificación viene de la
/// ruta y este campo se ignora), igual criterio que <c>Usuario</c> en <c>UsuarioRequestDto</c>.
/// <c>PrecioVenta</c> deliberadamente no existe acá: la Api siempre lo recalcula (spec Block 2,
/// mitigación de Tampering del threat model).</summary>
public record ArticuloRequestDto(
    string? Codigo,
    string Descripcion,
    decimal PrecioCosto,
    decimal Margen,
    int StockMinimo,
    int PuntoPedido,
    int StockIdeal);
