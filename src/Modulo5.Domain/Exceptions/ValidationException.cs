namespace Modulo5.Domain.Exceptions;

/// <summary>
/// Se lanza cuando una operación de dominio recibe datos que no cumplen sus reglas de validación
/// (p. ej. contraseña que no cumple la política, nombre de usuario duplicado). El middleware de la
/// Api (Block 3) la traduce a HTTP 400.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
