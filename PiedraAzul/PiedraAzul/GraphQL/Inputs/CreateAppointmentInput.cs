using PiedraAzul.Contracts.Enums;

namespace PiedraAzul.GraphQL.Inputs;

public record GuestPatientInput(
    string Identification,
    string Name,
    string? Phone,
    string? ExtraInfo,
    string? Email = null,
    DocumentType DocumentType = DocumentType.CC
);

public record CreateAppointmentInput(
    string DoctorId,
    string DoctorAvailabilitySlotId,
    DateTime Date,
    string? PatientUserId = null,
    GuestPatientInput? Guest = null
);
