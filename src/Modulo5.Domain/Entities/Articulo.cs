namespace Modulo5.Domain.Entities;

/// <summary>
/// Artículo del catálogo. <see cref="Codigo"/> es su PK natural — a diferencia de
/// <see cref="Usuario"/>/<see cref="Perfil"/>, el PRD de Artículos (AC-01/AC-02) nunca menciona un
/// identificador técnico separado (ver spec Block 1, sección "Logic").
/// </summary>
public class Articulo
{
    public string Codigo { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public decimal PrecioCosto { get; set; }

    public decimal Margen { get; set; }

    // Calculado por PrecioVentaCalculator (FR-04); nunca aceptado tal cual del cliente.
    public decimal PrecioVenta { get; set; }

    public int StockMinimo { get; set; }

    public int PuntoPedido { get; set; }

    public int StockIdeal { get; set; }
}
