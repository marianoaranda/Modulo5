namespace Modulo5.Domain.Exceptions;

/// <summary>
/// Se lanza cuando una operación de dominio falla por credenciales inválidas (usuario inexistente o
/// contraseña incorrecta en el login, Block 3). Se llama "Domain" para no colisionar con
/// <c>UnauthorizedAccessException</c> del BCL ni con la semántica HTTP 401, ya que el PRD la mapea a
/// un 400 lógico de aplicación (ver Block 3 del spec).
/// </summary>
public class UnauthorizedDomainException : Exception
{
    public UnauthorizedDomainException(string message) : base(message)
    {
    }

    public UnauthorizedDomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
