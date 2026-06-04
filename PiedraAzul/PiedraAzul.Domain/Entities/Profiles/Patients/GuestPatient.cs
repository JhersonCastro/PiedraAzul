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

        /// <summary>
        /// Si no es null, este guest fue absorbido (merge) por el usuario registrado
        /// con este Id. Sus citas ya fueron transferidas y no debe volver a ofrecerse el merge.
        /// </summary>
        public string? MergedToUserId { get; private set; }

        /// <summary>Fecha en que se realizó el merge (UTC).</summary>
        public DateTime? MergedAt { get; private set; }

        public bool IsMerged => MergedToUserId is not null;

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

        /// <summary>
        /// Marca este guest como vinculado a una cuenta registrada. Idempotente a nivel de
        /// negocio: no se permite re-mergear un guest ya mergeado.
        /// </summary>
        public void MarkAsMerged(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId requerido para el merge", nameof(userId));
            if (IsMerged)
                throw new InvalidOperationException("Este invitado ya fue vinculado a una cuenta.");

            MergedToUserId = userId;
            MergedAt = DateTime.UtcNow;
        }
    }
}
