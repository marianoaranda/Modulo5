using Modulo5.Domain.Articulos;

namespace Modulo5.Domain.Tests.Articulos;

/// <summary>
/// Tests del Block 1 del spec FEAT-001b: cálculo del Precio de Venta (FR-04).
/// </summary>
public class PrecioVentaCalculatorTests
{
    [Fact]
    public void Calcular_con_precioCosto_100_y_margen_20_devuelve_120()
    {
        // Arrange — soporta AC-04
        const decimal precioCosto = 100m;
        const decimal margen = 20m;

        // Act
        var precioVenta = PrecioVentaCalculator.Calcular(precioCosto, margen);

        // Assert
        Assert.Equal(120m, precioVenta);
    }

    [Fact]
    public void Calcular_con_margen_cero_devuelve_el_mismo_precioCosto()
    {
        // Arrange — soporta AC-04 (edge case)
        const decimal precioCosto = 75.50m;
        const decimal margen = 0m;

        // Act
        var precioVenta = PrecioVentaCalculator.Calcular(precioCosto, margen);

        // Assert
        Assert.Equal(precioCosto, precioVenta);
    }
}
