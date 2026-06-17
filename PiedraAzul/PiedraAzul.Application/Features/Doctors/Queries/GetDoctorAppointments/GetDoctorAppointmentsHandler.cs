using Mediator;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.Appointments;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Application.Features.Doctors.Queries.GetDoctorAppointments
{
    public class GetDoctorAppointmentsHandler
    : IRequestHandler<GetDoctorAppointmentsQuery, IReadOnlyList<AppointmentDto>>
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IDoctorAvailabilitySlotRepository _slotRepository;
        private readonly IIdentityService _identityService;
        private readonly IPatientGuestRepository _guestRepository;

        public GetDoctorAppointmentsHandler(
            IAppointmentRepository appointmentRepository,
            IDoctorAvailabilitySlotRepository slotRepository,
            IIdentityService identityService,
            IPatientGuestRepository guestRepository)
        {
            _appointmentRepository = appointmentRepository;
            _slotRepository = slotRepository;
            _identityService = identityService;
            _guestRepository = guestRepository;
        }

        public async ValueTask<IReadOnlyList<AppointmentDto>> Handle(
            GetDoctorAppointmentsQuery request,
            CancellationToken cancellationToken)
        {
            var appointments = await _appointmentRepository
                .ListByDoctorAsync(request.DoctorId, request.Date, cancellationToken);

            if (appointments.Count == 0)
                return [];

            var slotIds = appointments.Select(a => a.DoctorAvailabilitySlotId).Distinct().ToList();

            var slots = await _slotRepository.GetByIdsAsync(slotIds, cancellationToken);
            var slotDict = slots.ToDictionary(s => s.Id);

            var userIds = appointments
                .Where(a => a.PatientUserId != null)
                .Select(a => a.PatientUserId!)
                .Distinct()
                .ToList();

            var users = await _identityService.GetByIds(userIds);
            var userDict = users.ToDictionary(u => u.Id);

            var guestIds = appointments
                .Where(a => a.PatientGuestId != null)
                .Select(a => a.PatientGuestId!)
                .Distinct()
                .ToList();

            var guests = await _guestRepository.GetByIdsAsync(guestIds, cancellationToken);
            var guestDict = guests.ToDictionary(g => g.Id);

            return appointments.Select(a =>
            {
                var slot = slotDict[a.DoctorAvailabilitySlotId];

                var start = a.Date.ToDateTime(TimeOnly.MinValue)
                    .Add(slot.StartTime);

                string name;
                string type;

                if (a.PatientUserId != null)
                {
                    var user = userDict[a.PatientUserId];
                    name = user.Name;
                    type = "Registered";
                }
                else
                {
                    var guest = guestDict[a.PatientGuestId!];
                    name = guest.Name;
                    type = "Guest";
                }

                return new AppointmentDto
                {
                    Id = a.Id,
                    PatientUserId = a.PatientUserId,
                    PatientGuestId = a.PatientGuestId,

                    PatientName = name,
                    PatientType = type,

                    SlotId = a.DoctorAvailabilitySlotId,
                    Start = start,
                    CreatedAt = a.CreatedAt
                };
            }).ToList();
        }
    }
}

