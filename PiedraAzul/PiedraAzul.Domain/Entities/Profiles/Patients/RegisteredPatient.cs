using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Domain.Entities.Profiles.Patients
{
    public class RegisteredPatient : Patient
    {
        public string UserId { get; }

        private RegisteredPatient() { }

        public RegisteredPatient(string userId, string name)
        {
            UserId = userId;
            Id = userId;
            Name = name;
        }
    }
}
