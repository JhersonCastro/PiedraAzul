using System;
using System.ComponentModel.DataAnnotations;
using PiedraAzul.Contracts.Enums;

namespace PiedraAzul.GraphQL.Inputs;

public record RegisterInput(
    [Required(ErrorMessage = "El correo es requerido")]
    [EmailAddress(ErrorMessage = "El correo no es válido")]
    string Email,

    [Required(ErrorMessage = "La contraseña es requerida")]
    [StringLength(255, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 255 caracteres")]
    string Password,

    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 255 caracteres")]
    string Name,

    [Required(ErrorMessage = "El teléfono es requerido")]
    string Phone,

    [Required(ErrorMessage = "La identificación es requerida")]
    string IdentificationNumber,

    DocumentType DocumentType,

    GenderType Gender,

    DateTime? BirthDate,

    [Required(ErrorMessage = "Al menos un rol es requerido")]
    List<string> Roles
);
