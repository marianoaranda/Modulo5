namespace Modulo5.Domain.Exceptions;

/// <summary>
/// Se lanza cuando se busca una entidad por su identificador y no existe (p. ej. UsuarioId
/// inexistente en un PUT/DELETE). El middleware de la Api (Block 3) la traduce a HTTP 404.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
