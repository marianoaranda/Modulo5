using System.ComponentModel.DataAnnotations;

namespace Modulo5.Web.Models;

/// <summary>
/// ViewModel de <c>Views/Articulos/Edit.cshtml</c> (spec Block 3). Igual que <c>ArticuloRequest</c>
/// del lado de la Api (Block 2): <c>Codigo</c> NO es editable por contrato del bloque (viene de la
/// ruta, no del body).
///
/// ASSUMPTION documentada en el reporte del bloque: igual que en <c>UsuarioEditViewModel</c>
/// (FEAT-001a), la Api (Block 2) no expone ningún endpoint <c>GET</c> para recuperar un artículo
/// existente por <c>Codigo</c>, así que este formulario no puede pre-cargar los valores actuales — el
/// usuario debe reingresarlos completos (el <c>PUT</c> de la Api tampoco admite un patch parcial).
/// </summary>
public class ArticuloEditViewModel
{
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
