using PiedraAzul.Domain.Entities.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Application.Common.Models.Doctor
{
    public class DoctorDto
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? AvatarUrl { get; set; }

        public DoctorType Specialty { get; set; }
        public string LicenseNumber { get; set; } = default!;
        public string? Notes { get; set; }
    }
}
