using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Domain.Entities.Profiles.Patients
{
    public class GuestPatient : Patient
    {
        public string Phone { get; }
        public string ExtraInfo { get; }

        private GuestPatient() { }

        public GuestPatient(string id, string name, string phone, string extraInfo)
        {
            Id = id;
            Name = name;
            Phone = phone;
            ExtraInfo = extraInfo;
        }
    }
}
