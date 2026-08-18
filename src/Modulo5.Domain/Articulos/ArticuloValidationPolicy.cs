using Modulo5.Domain.Exceptions;

namespace Modulo5.Domain.Articulos;

/// <summary>
/// Reglas de negocio de Artículo sin dependencia de base de datos: FR-06 (ningún valor negativo) y
/// FR-07 (StockMinimo ≤ PuntoPedido ≤ StockIdeal) del PRD FEAT-001b. Clase estática, igual patrón
/// que <see cref="Security.PasswordPolicy"/> de FEAT-001a.
/// </summary>
public static class ArticuloValidationPolicy
{
    public const string NegativeValuesMessage =
        "Los valores de Precio de Costo, Margen, Stock Mínimo, Punto de Pedido y Stock Ideal no " +
        "pueden ser negativos.";

    public const string StockOrderMessage =
        "El Stock Mínimo debe ser menor o igual al Punto de Pedido, y este menor o igual al Stock " +
        "Ideal.";

    /// <summary>
    /// Valida los valores recibidos por Block 2 (alta/modificación) y lanza
    /// <see cref="ValidationException"/> con el mensaje exacto del PRD si no cumplen FR-06/FR-07.
    /// </summary>
    public static void Validate(
        decimal precioCosto, decimal margen, int stockMinimo, int puntoPedido, int stockIdeal)
    {
        if (precioCosto < 0 || margen < 0 || stockMinimo < 0 || puntoPedido < 0 || stockIdeal < 0)
        {
            throw new ValidationException(NegativeValuesMessage);
        }

        if (stockMinimo > puntoPedido || puntoPedido > stockIdeal)
        {
            throw new ValidationException(StockOrderMessage);
        }
    }
}
