namespace Modulo5.Web.Services;

/// <summary>Refleja <c>Modulo5.Api.Dtos.ArticuloResponse</c> (spec Block 3, "Files").</summary>
public record ArticuloDto(
    string Codigo,
    string Descripcion,
    decimal PrecioCosto,
    decimal Margen,
    decimal PrecioVenta,
    int StockMinimo,
    int PuntoPedido,
    int StockIdeal);
