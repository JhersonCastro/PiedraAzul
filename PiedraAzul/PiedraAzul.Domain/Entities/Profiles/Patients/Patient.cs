using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Domain.Entities.Profiles.Patients
{
    public abstract class Patient
    {
        public string Id { get; protected set; }
        public string Name { get; protected set; }
    }
}
