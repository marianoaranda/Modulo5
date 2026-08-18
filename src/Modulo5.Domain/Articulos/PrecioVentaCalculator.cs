namespace Modulo5.Domain.Articulos;

/// <summary>
/// Cálculo del Precio de Venta de un Artículo (FR-04 del PRD FEAT-001b). Clase estática, pura y sin
/// dependencias de Data/Api, igual patrón que <see cref="Security.PasswordPolicy"/> de FEAT-001a.
/// </summary>
public static class PrecioVentaCalculator
{
    /// <summary>
    /// PrecioVenta = PrecioCosto × (1 + Margen/100).
    /// </summary>
    public static decimal Calcular(decimal precioCosto, decimal margen) =>
        precioCosto * (1 + margen / 100m);
}
