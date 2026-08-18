namespace Modulo5.Api.Dtos;

/// <summary>
/// Response de <c>POST</c>/<c>PUT /api/articulos</c> (Block 2, "API contract").
/// </summary>
public class ArticuloResponse
{
    public ArticuloResponse(
        string codigo,
        string descripcion,
        decimal precioCosto,
        decimal margen,
        decimal precioVenta,
        int stockMinimo,
        int puntoPedido,
        int stockIdeal)
    {
        Codigo = codigo;
        Descripcion = descripcion;
        PrecioCosto = precioCosto;
        Margen = margen;
        PrecioVenta = precioVenta;
        StockMinimo = stockMinimo;
        PuntoPedido = puntoPedido;
        StockIdeal = stockIdeal;
    }

    public string Codigo { get; }

    public string Descripcion { get; }

    public decimal PrecioCosto { get; }

    public decimal Margen { get; }

    public decimal PrecioVenta { get; }

    public int StockMinimo { get; }

    public int PuntoPedido { get; }

    public int StockIdeal { get; }
}
