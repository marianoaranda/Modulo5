using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modulo5.Api.Dtos;
using Modulo5.Domain.Articulos;
using Modulo5.Domain.Entities;
using Modulo5.Domain.Exceptions;
using Modulo5.Domain.Repositories;

namespace Modulo5.Api.Controllers;

/// <summary>
/// ABM de Artículos (Block 2 del spec FEAT-001b). A diferencia de <c>UsuariosController</c>, solo
/// exige <c>[Authorize]</c> (JWT válido) — sin la política "AdminOnly": el PRD de Artículos no
/// restringe esta pantalla a ningún perfil (ya evaluado en el threat model). Antes de persistir
/// (alta y modificación) se invoca <see cref="ArticuloValidationPolicy.Validate"/> (Block 1) y
/// siempre se recalcula <c>PrecioVenta</c> con <see cref="PrecioVentaCalculator.Calcular"/>,
/// ignorando cualquier <c>PrecioVenta</c> que pudiera venir del cliente (mitigación de Tampering del
/// threat model). Cada operación exitosa loguea el <c>UsuarioId</c> del actor + timestamp
/// (mitigación de Repudiation del threat model).
/// </summary>
[ApiController]
[Route("api/articulos")]
[Authorize]
public class ArticulosController : ControllerBase
{
    private const string UsuarioIdClaimType = "UsuarioId";
    private const int CodigoMaxLength = 30;
    private const int DescripcionMaxLength = 200;

    private readonly IArticuloRepository _articuloRepository;
    private readonly ILogger<ArticulosController> _logger;

    public ArticulosController(
        IArticuloRepository articuloRepository,
        ILogger<ArticulosController> logger)
    {
        _articuloRepository = articuloRepository;
        _logger = logger;
    }

    /// <summary>
    /// <c>POST /api/articulos</c> — alta de artículo (FR-01/AC-01). <c>PrecioVenta</c> del request
    /// (si viniera) se ignora: siempre se recalcula acá con <see cref="PrecioVentaCalculator"/>. El
    /// Código duplicado lo traduce a <see cref="ValidationException"/> el repositorio (Block 1), no
    /// este método.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ArticuloResponse>> Create([FromBody] ArticuloRequest request)
    {
        ValidateCodigo(request.Codigo);
        ValidateDescripcion(request.Descripcion);

        ArticuloValidationPolicy.Validate(
            request.PrecioCosto, request.Margen, request.StockMinimo, request.PuntoPedido,
            request.StockIdeal);

        var articulo = new Articulo
        {
            Codigo = request.Codigo!,
            Descripcion = request.Descripcion,
            PrecioCosto = request.PrecioCosto,
            Margen = request.Margen,
            PrecioVenta = PrecioVentaCalculator.Calcular(request.PrecioCosto, request.Margen),
            StockMinimo = request.StockMinimo,
            PuntoPedido = request.PuntoPedido,
            StockIdeal = request.StockIdeal
        };

        var creado = await _articuloRepository.AddAsync(articulo);

        LogOperacionExitosa("alta", creado.Codigo);

        return StatusCode(StatusCodes.Status201Created, MapToResponse(creado));
    }

    /// <summary>
    /// <c>PUT /api/articulos/{codigo}</c> — modificación de artículo (FR-03/AC-03). <c>Codigo</c> no
    /// es editable por contrato del bloque (viene de la ruta, no de <paramref name="request"/>).
    /// </summary>
    [HttpPut("{codigo}")]
    public async Task<ActionResult<ArticuloResponse>> Update(
        string codigo, [FromBody] ArticuloRequest request)
    {
        var articulo = await _articuloRepository.GetByCodigoAsync(codigo)
            ?? throw new NotFoundException($"No existe el artículo con Código {codigo}.");

        ValidateDescripcion(request.Descripcion);

        ArticuloValidationPolicy.Validate(
            request.PrecioCosto, request.Margen, request.StockMinimo, request.PuntoPedido,
            request.StockIdeal);

        articulo.Descripcion = request.Descripcion;
        articulo.PrecioCosto = request.PrecioCosto;
        articulo.Margen = request.Margen;
        articulo.PrecioVenta = PrecioVentaCalculator.Calcular(request.PrecioCosto, request.Margen);
        articulo.StockMinimo = request.StockMinimo;
        articulo.PuntoPedido = request.PuntoPedido;
        articulo.StockIdeal = request.StockIdeal;

        await _articuloRepository.UpdateAsync(articulo);

        LogOperacionExitosa("modificación", articulo.Codigo);

        return Ok(MapToResponse(articulo));
    }

    /// <summary>
    /// <c>DELETE /api/articulos/{codigo}</c> — baja de artículo (FR-02/AC-02).
    /// </summary>
    [HttpDelete("{codigo}")]
    public async Task<IActionResult> Delete(string codigo)
    {
        var articulo = await _articuloRepository.GetByCodigoAsync(codigo)
            ?? throw new NotFoundException($"No existe el artículo con Código {codigo}.");

        await _articuloRepository.DeleteAsync(articulo);

        LogOperacionExitosa("baja", articulo.Codigo);

        return NoContent();
    }

    private static void ValidateCodigo(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo) || codigo.Length > CodigoMaxLength)
        {
            throw new ValidationException(
                $"El Código es requerido y debe tener máximo {CodigoMaxLength} caracteres.");
        }
    }

    private static void ValidateDescripcion(string descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion) || descripcion.Length > DescripcionMaxLength)
        {
            throw new ValidationException(
                $"La Descripción es requerida y debe tener máximo {DescripcionMaxLength} caracteres.");
        }
    }

    /// <summary>
    /// Mitigación de Repudiation (threat model): loguea el <c>UsuarioId</c> del actor —tomado del
    /// claim del JWT, no del body— + timestamp en cada operación exitosa de ABM de Artículos.
    /// </summary>
    private void LogOperacionExitosa(string operacion, string codigoArticulo)
    {
        var actorId = User.FindFirst(UsuarioIdClaimType)?.Value ?? "desconocido";
        _logger.LogInformation(
            "ABM Artículos: {Operacion} sobre Código {CodigoArticulo} por actor UsuarioId " +
            "{ActorId} a las {Timestamp:o}",
            operacion,
            codigoArticulo,
            actorId,
            DateTime.UtcNow);
    }

    private static ArticuloResponse MapToResponse(Articulo articulo) => new(
        articulo.Codigo,
        articulo.Descripcion,
        articulo.PrecioCosto,
        articulo.Margen,
        articulo.PrecioVenta,
        articulo.StockMinimo,
        articulo.PuntoPedido,
        articulo.StockIdeal);
}
