using Moq;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorAvailableDays;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Shared.Enums;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Test.Tests;

public class GetDoctorAvailableDaysHandlerTests
{
    private readonly Mock<IDoctorAvailabilitySlotRepository> _slotRepository = new();
    private readonly Mock<IAppointmentRepository> _appointmentRepository = new();

    private readonly GetDoctorAvailableDaysHandler _sut;

    public GetDoctorAvailableDaysHandlerTests()
    {
        _sut = new GetDoctorAvailableDaysHandler(
            _slotRepository.Object,
            _appointmentRepository.Object);
    }

    [Fact]
    public async Task GetAvailableDays_WhenNoSlots_ReturnsEmpty()
    {
        _slotRepository
            .Setup(x => x.ListByDoctorAsync("doc-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.Handle(
            new GetDoctorAvailableDaysQuery("doc-1", DateOnly.FromDateTime(DateTime.UtcNow), 7),
            CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableDays_WhenSlotAvailable_IncludesDay()
    {
        var startDate = NextDateFor(DayOfWeek.Monday);
        var slot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));

        _slotRepository
            .Setup(x => x.ListByDoctorAsync("doc-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([slot]);
        _appointmentRepository
            .Setup(x => x.ListByDoctorAsync("doc-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.Handle(
            new GetDoctorAvailableDaysQuery("doc-1", startDate, 7),
            CancellationToken.None);

        Assert.Contains(startDate, result);
    }

    [Fact]
    public async Task GetAvailableDays_WhenAllSlotsOccupied_ExcludesDay()
    {
        var monday = NextDateFor(DayOfWeek.Monday);
        var slot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var appointment = Appointment.Create(slot, monday, "doc-1", "patient-1", null);

        _slotRepository
            .Setup(x => x.ListByDoctorAsync("doc-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([slot]);
        _appointmentRepository
            .Setup(x => x.ListByDoctorAsync("doc-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([appointment]);

        var result = await _sut.Handle(
            new GetDoctorAvailableDaysQuery("doc-1", monday, 7),
            CancellationToken.None);

        Assert.DoesNotContain(monday, result);
    }

    [Fact]
    public async Task GetAvailableDays_WhenOneOfTwoSlotsOccupied_IncludesDay()
    {
        var monday = NextDateFor(DayOfWeek.Monday);
        var slot1 = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var slot2 = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(11), TimeSpan.FromHours(12));
        var appointment = Appointment.Create(slot1, monday, "doc-1", "patient-1", null);

        _slotRepository
            .Setup(x => x.ListByDoctorAsync("doc-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([slot1, slot2]);
        _appointmentRepository
            .Setup(x => x.ListByDoctorAsync("doc-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([appointment]);

        var result = await _sut.Handle(
            new GetDoctorAvailableDaysQuery("doc-1", monday, 7),
            CancellationToken.None);

        Assert.Contains(monday, result);
    }

    [Fact]
    public async Task GetAvailableDays_IgnoresCancelledAppointments()
    {
        var monday = NextDateFor(DayOfWeek.Monday);
        var slot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var appointment = Appointment.Create(slot, monday, "doc-1", "patient-1", null);
        appointment.Cancel();

        _slotRepository
            .Setup(x => x.ListByDoctorAsync("doc-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([slot]);
        _appointmentRepository
            .Setup(x => x.ListByDoctorAsync("doc-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([appointment]);

        var result = await _sut.Handle(
            new GetDoctorAvailableDaysQuery("doc-1", monday, 7),
            CancellationToken.None);

        Assert.Contains(monday, result);
    }

    [Fact]
    public async Task GetAvailableDays_OnlyReturnsDatesinRequestedRange()
    {
        var startDate = NextDateFor(DayOfWeek.Monday);
        var slot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));

        _slotRepository
            .Setup(x => x.ListByDoctorAsync("doc-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([slot]);
        _appointmentRepository
            .Setup(x => x.ListByDoctorAsync("doc-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.Handle(
            new GetDoctorAvailableDaysQuery("doc-1", startDate, 1),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(startDate, result[0]);
    }

    [Fact]
    public async Task GetAvailableDays_WhenDayHasNoSlots_ExcludesDay()
    {
        var tuesday = NextDateFor(DayOfWeek.Tuesday);
        var slot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));

        _slotRepository
            .Setup(x => x.ListByDoctorAsync("doc-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([slot]);
        _appointmentRepository
            .Setup(x => x.ListByDoctorAsync("doc-1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.Handle(
            new GetDoctorAvailableDaysQuery("doc-1", tuesday, 1),
            CancellationToken.None);

        Assert.Empty(result);
    }

    private static DateOnly NextDateFor(DayOfWeek day)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1));
        while (date.DayOfWeek != day)
            date = date.AddDays(1);
        return date;
    }
}
