using Mediator;
using PiedraAzul.Domain.Entities.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Application.Features.Patients.Commands.CreateGuestPatient
{
    public record CreateGuestPatientCommand(
        string IdentificationId,
        string Name,
        string Phone,
        string ExtraInfo,
        string? Email = null,
        DocumentType DocumentType = DocumentType.CC
) : IRequest<string>;
}
