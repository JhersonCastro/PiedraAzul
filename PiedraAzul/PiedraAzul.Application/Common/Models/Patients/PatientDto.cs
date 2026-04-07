using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Application.Common.Models.Patients
{
    public record PatientDto(
    string Id,
    string Name,
    string Type // "Registered" | "Guest"
);
}
