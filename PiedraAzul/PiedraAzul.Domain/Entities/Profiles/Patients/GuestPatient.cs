using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Domain.Entities.Profiles.Patients
{
    public class GuestPatient : Patient
    {
        public string Phone { get; private set; } = "";
        public string ExtraInfo { get; private set; } = "";
        public string? Email { get; private set; }

        private GuestPatient() { }

        public GuestPatient(string id, string name, string phone, string extraInfo, string? email = null)
        {
            Id = id;
            Name = name;
            Phone = phone;
            ExtraInfo = extraInfo;
            Email = email;
        }

        public void UpdateInfo(string name, string phone, string? email)
        {
            Name = name;
            Phone = phone;
            Email = email;
        }
    }
}
