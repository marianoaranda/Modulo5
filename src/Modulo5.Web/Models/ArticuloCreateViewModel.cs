using System.ComponentModel.DataAnnotations;

namespace Modulo5.Web.Models;

/// <summary>
/// ViewModel de <c>Views/Articulos/Create.cshtml</c> (spec Block 3, "Input validation"): Data
/// Annotations que replican las reglas del Block 2 (<c>Codigo</c> máx. 30, <c>Descripcion</c> máx.
/// 200, valores numéricos no negativos vía <see cref="RangeAttribute"/>) — defensa en profundidad
/// client+server; la validación real (incluyendo <c>StockMinimo ≤ PuntoPedido ≤ StockIdeal</c>, que
/// no se puede expresar con Data Annotations simples) sigue viviendo en la Api
/// (<c>ArticuloValidationPolicy</c>, Block 1).
/// </summary>
public class ArticuloCreateViewModel
{
    [Required(ErrorMessage = "El Código es requerido.")]
    [StringLength(30, ErrorMessage = "El Código debe tener máximo 30 caracteres.")]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La Descripción es requerida.")]
    [StringLength(200, ErrorMessage = "La Descripción debe tener máximo 200 caracteres.")]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El Precio de Costo es requerido.")]
    [Range(0, double.MaxValue, ErrorMessage = "El Precio de Costo no puede ser negativo.")]
    [Display(Name = "Precio de Costo")]
    public decimal PrecioCosto { get; set; }

    [Required(ErrorMessage = "El Margen es requerido.")]
    [Range(0, double.MaxValue, ErrorMessage = "El Margen no puede ser negativo.")]
    [Display(Name = "Margen (%)")]
    public decimal Margen { get; set; }

    [Required(ErrorMessage = "El Stock Mínimo es requerido.")]
    [Range(0, int.MaxValue, ErrorMessage = "El Stock Mínimo no puede ser negativo.")]
    [Display(Name = "Stock Mínimo")]
    public int StockMinimo { get; set; }

    [Required(ErrorMessage = "El Punto de Pedido es requerido.")]
    [Range(0, int.MaxValue, ErrorMessage = "El Punto de Pedido no puede ser negativo.")]
    [Display(Name = "Punto de Pedido")]
    public int PuntoPedido { get; set; }

    [Required(ErrorMessage = "El Stock Ideal es requerido.")]
    [Range(0, int.MaxValue, ErrorMessage = "El Stock Ideal no puede ser negativo.")]
    [Display(Name = "Stock Ideal")]
    public int StockIdeal { get; set; }
}
