using Modulo5.Domain.Articulos;
using Modulo5.Domain.Exceptions;

namespace Modulo5.Domain.Tests.Articulos;

/// <summary>
/// Tests del Block 1 del spec FEAT-001b: reglas de negocio de Artículo sin dependencia de base de
/// datos (FR-06 valores no negativos, FR-07 orden de umbrales de stock).
/// </summary>
public class ArticuloValidationPolicyTests
{
    [Theory]
    [InlineData(-1, 20, 5, 10, 20)] // PrecioCosto negativo
    [InlineData(100, -1, 5, 10, 20)] // Margen negativo
    [InlineData(100, 20, -1, 10, 20)] // StockMinimo negativo
    [InlineData(100, 20, 5, -1, 20)] // PuntoPedido negativo
    [InlineData(100, 20, 5, 10, -1)] // StockIdeal negativo
    public void Validate_rechaza_cualquier_valor_negativo_con_el_mensaje_exacto(
        decimal precioCosto, decimal margen, int stockMinimo, int puntoPedido, int stockIdeal)
    {
        // Arrange & Act & Assert — soporta AC-06 (sad path)
        var ex = Assert.Throws<ValidationException>(() =>
            ArticuloValidationPolicy.Validate(precioCosto, margen, stockMinimo, puntoPedido, stockIdeal));

        Assert.Equal(
            "Los valores de Precio de Costo, Margen, Stock Mínimo, Punto de Pedido y Stock Ideal " +
            "no pueden ser negativos.",
            ex.Message);
    }

    [Fact]
    public void Validate_rechaza_StockMinimo_mayor_a_PuntoPedido_con_el_mensaje_exacto()
    {
        // Arrange — soporta AC-07 (sad path)
        const int stockMinimo = 11;
        const int puntoPedido = 10;
        const int stockIdeal = 20;

        // Act & Assert
        var ex = Assert.Throws<ValidationException>(() =>
            ArticuloValidationPolicy.Validate(100m, 20m, stockMinimo, puntoPedido, stockIdeal));

        Assert.Equal(
            "El Stock Mínimo debe ser menor o igual al Punto de Pedido, y este menor o igual al " +
            "Stock Ideal.",
            ex.Message);
    }

    [Fact]
    public void Validate_rechaza_PuntoPedido_mayor_a_StockIdeal_con_el_mensaje_exacto()
    {
        // Arrange — soporta AC-07 (sad path)
        const int stockMinimo = 5;
        const int puntoPedido = 21;
        const int stockIdeal = 20;

        // Act & Assert
        var ex = Assert.Throws<ValidationException>(() =>
            ArticuloValidationPolicy.Validate(100m, 20m, stockMinimo, puntoPedido, stockIdeal));

        Assert.Equal(
            "El Stock Mínimo debe ser menor o igual al Punto de Pedido, y este menor o igual al " +
            "Stock Ideal.",
            ex.Message);
    }

    [Fact]
    public void Validate_acepta_StockMinimo_igual_a_PuntoPedido_igual_a_StockIdeal()
    {
        // Arrange — soporta AC-07 (edge case, límites inclusive)
        const int stockMinimo = 10;
        const int puntoPedido = 10;
        const int stockIdeal = 10;

        // Act
        var exception = Record.Exception(() =>
            ArticuloValidationPolicy.Validate(100m, 20m, stockMinimo, puntoPedido, stockIdeal));

        // Assert
        Assert.Null(exception);
    }
}
