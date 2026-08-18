namespace Modulo5.Api.Dtos;

/// <summary>
/// Request compartido de <c>POST /api/articulos</c> y <c>PUT /api/articulos/{codigo}</c> (Block 2,
/// "API contract"). <see cref="Codigo"/> solo se usa en el POST — en el PUT viene de la ruta y este
/// campo se ignora, igual criterio que <c>Usuario</c> en <c>UsuarioRequest</c> de FEAT-001a.
/// <see cref="PrecioVenta"/> deliberadamente NO existe acá: el controller SIEMPRE lo recalcula con
/// <c>PrecioVentaCalculator</c> (mitigación de Tampering del threat model), así que no hay ningún
/// campo del request que pueda usarse para setearlo.
/// </summary>
public class ArticuloRequest
{
    /// <summary>Requerido en el alta (POST). Ignorado en la modificación (PUT): el Código no es
    /// editable por contrato del bloque, viene de la ruta.</summary>
    public string? Codigo { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public decimal PrecioCosto { get; set; }

    public decimal Margen { get; set; }

    public int StockMinimo { get; set; }

    public int PuntoPedido { get; set; }

    public int StockIdeal { get; set; }
}
