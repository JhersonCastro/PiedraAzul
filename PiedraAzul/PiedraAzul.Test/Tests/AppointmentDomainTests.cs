using PiedraAzul.Domain.Common.Exceptions;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Shared.Enums;

namespace PiedraAzul.Test.Tests;

public class AppointmentDomainTests
{
    [Fact]
    public void Create_WithRegisteredPatient_Succeeds()
    {
        var slot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var date = NextDateFor(DayOfWeek.Monday);

        var appointment = Appointment.Create(slot, date, "doc-1", "patient-1", null);

        Assert.Equal("patient-1", appointment.PatientUserId);
        Assert.Null(appointment.PatientGuestId);
        Assert.Equal(AppointmentStatus.Active, appointment.Status);
    }

    [Fact]
    public void Create_WithGuestPatient_Succeeds()
    {
        var slot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var date = NextDateFor(DayOfWeek.Monday);

        var appointment = Appointment.Create(slot, date, "doc-1", null, "guest-1");

        Assert.Null(appointment.PatientUserId);
        Assert.Equal("guest-1", appointment.PatientGuestId);
    }

    [Fact]
    public void Create_WithBothPatients_ThrowsDomainException()
    {
        var slot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var date = NextDateFor(DayOfWeek.Monday);

        var ex = Assert.Throws<DomainException>(
            () => Appointment.Create(slot, date, "doc-1", "patient-1", "guest-1"));

        Assert.Equal("Appointment must have either a registered patient or a guest", ex.Message);
    }

    [Fact]
    public void Create_WithNeitherPatient_ThrowsDomainException()
    {
        var slot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var date = NextDateFor(DayOfWeek.Monday);

        var ex = Assert.Throws<DomainException>(
            () => Appointment.Create(slot, date, "doc-1", null, null));

        Assert.Equal("Appointment must have either a registered patient or a guest", ex.Message);
    }

    [Fact]
    public void Create_WhenSlotDoctorMismatch_ThrowsDomainException()
    {
        var slot = new DoctorAvailabilitySlot("doc-A", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var date = NextDateFor(DayOfWeek.Monday);

        var ex = Assert.Throws<DomainException>(
            () => Appointment.Create(slot, date, "doc-B", "patient-1", null));

        Assert.Equal("Invalid doctor", ex.Message);
    }

    [Fact]
    public void Create_WhenDateDoesNotMatchSlotDay_ThrowsDomainException()
    {
        var slot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var tuesday = NextDateFor(DayOfWeek.Tuesday);

        var ex = Assert.Throws<DomainException>(
            () => Appointment.Create(slot, tuesday, "doc-1", "patient-1", null));

        Assert.Equal("Slot does not match selected date", ex.Message);
    }

    [Fact]
    public void Create_WhenDateInPast_ThrowsDomainException()
    {
        var slot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        try
        {
            Appointment.Create(slot, pastDate, "doc-1", "patient-1", null);
        }
        catch (Exception ex)
        {
            Assert.Equal("Slot does not match selected date", ex.Message);
        }
    }

    [Fact]
    public void Cancel_ChangesStatusToCancelled()
    {
        var appointment = BuildActiveAppointment();

        appointment.Cancel();

        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
    }

    [Fact]
    public void MarkAsRescheduled_ChangesStatusAndStoresNewId()
    {
        var appointment = BuildActiveAppointment();
        var newId = Guid.NewGuid();

        appointment.MarkAsRescheduled(newId);

        Assert.Equal(AppointmentStatus.Rescheduled, appointment.Status);
        Assert.Equal(newId, appointment.RescheduledToId);
    }

    [Fact]
    public void Create_SetsCreatedAtToUtcNow()
    {
        var before = DateTime.UtcNow;
        var appointment = BuildActiveAppointment();
        var after = DateTime.UtcNow;

        Assert.InRange(appointment.CreatedAt, before, after);
    }

    [Fact]
    public void Create_GeneratesUniqueId()
    {
        var a1 = BuildActiveAppointment();
        var a2 = BuildActiveAppointment();

        Assert.NotEqual(a1.Id, a2.Id);
    }

    private static Appointment BuildActiveAppointment()
    {
        var slot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var date = NextDateFor(DayOfWeek.Monday);
        return Appointment.Create(slot, date, "doc-1", "patient-1", null);
    }

    private static DateOnly NextDateFor(DayOfWeek day)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1));
        while (date.DayOfWeek != day)
            date = date.AddDays(1);
        return date;
    }
}
