using System.ComponentModel.DataAnnotations;

namespace PiedraAzul.GraphQL.Inputs;

public record BeginPasskeyRegistrationInput(
    [Required(ErrorMessage = "El ID de usuario es requerido")]
    string UserId,

    [Required(ErrorMessage = "El correo es requerido")]
    [EmailAddress(ErrorMessage = "El correo no es válido")]
    string Email,

    [Required(ErrorMessage = "El nombre de visualización es requerido")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 255 caracteres")]
    string DisplayName
);

public record CompletePasskeyRegistrationInput(
    [Required(ErrorMessage = "El ID de usuario es requerido")]
    string UserId,

    [Required(ErrorMessage = "La respuesta de atestación es requerida")]
    string AttestationResponse,

    [Required(ErrorMessage = "El nombre amigable es requerido")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 255 caracteres")]
    string FriendlyName
);

public record CompletePasskeyAssertionInput(
    [Required(ErrorMessage = "La respuesta de afirmación es requerida")]
    string AssertionResponse
);
