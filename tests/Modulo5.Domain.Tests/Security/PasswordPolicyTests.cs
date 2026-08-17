using Modulo5.Domain.Exceptions;
using Modulo5.Domain.Security;

namespace Modulo5.Domain.Tests.Security;

/// <summary>
/// Tests del Block 2 del spec FEAT-001a: política de contraseñas (longitud/formato).
/// </summary>
public class PasswordPolicyTests
{
    [Fact]
    public void Password_de_7_caracteres_alfanumericos_es_rechazada_con_el_mensaje_exacto()
    {
        // Arrange — soporta AC-06 (sad path)
        const string password = "abc1234";

        // Act & Assert
        Assert.False(PasswordPolicy.IsValid(password));

        var ex = Assert.Throws<ValidationException>(() => PasswordPolicy.Validate(password));
        Assert.Equal("La contraseña debe tener al menos 8 caracteres alfanuméricos.", ex.Message);
    }

    [Fact]
    public void Password_de_8_caracteres_alfanumericos_es_aceptada()
    {
        // Arrange — soporta AC-07
        const string password = "abc12345";

        // Act & Assert
        Assert.True(PasswordPolicy.IsValid(password));

        var exception = Record.Exception(() => PasswordPolicy.Validate(password));
        Assert.Null(exception);
    }
}
