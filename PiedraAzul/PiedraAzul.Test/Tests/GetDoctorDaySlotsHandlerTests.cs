using Moq;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorDaySlots;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Shared.Enums;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Test.Tests;

public class GetDoctorDaySlotsHandlerTests
{
    private readonly Mock<IDoctorRepository> _doctorRepository = new();
    private readonly Mock<IDoctorAvailabilitySlotRepository> _slotRepository = new();
    private readonly Mock<IAppointmentRepository> _appointmentRepository = new();

    private readonly GetDoctorDaySlotsHandler _sut;

    public GetDoctorDaySlotsHandlerTests()
    {
        _sut = new GetDoctorDaySlotsHandler(
            _doctorRepository.Object,
            _slotRepository.Object,
            _appointmentRepository.Object);
    }

    [Fact]
    public async Task GetDoctorDaySlots_WhenDoctorNotFound_Throws()
    {
        _doctorRepository
            .Setup(x => x.ExistsAsync("doc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.Handle(
                new GetDoctorDaySlotsQuery("doc-1", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))),
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task GetDoctorDaySlots_WhenNoSlots_ReturnsEmpty()
    {
        var monday = NextDateFor(DayOfWeek.Monday);

        _doctorRepository.Setup(x => x.ExistsAsync("doc-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _slotRepository.Setup(x => x.ListByDoctorAsync("doc-1", false, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _appointmentRepository.Setup(x => x.ListByDoctorAsync("doc-1", monday, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await _sut.Handle(new GetDoctorDaySlotsQuery("doc-1", monday), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDoctorDaySlots_ReturnsOnlySlotsMatchingDate()
    {
        var monday = NextDateFor(DayOfWeek.Monday);
        var mondaySlot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var tuesdaySlot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Tuesday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));

        _doctorRepository.Setup(x => x.ExistsAsync("doc-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _slotRepository.Setup(x => x.ListByDoctorAsync("doc-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([mondaySlot, tuesdaySlot]);
        _appointmentRepository.Setup(x => x.ListByDoctorAsync("doc-1", monday, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.Handle(new GetDoctorDaySlotsQuery("doc-1", monday), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(mondaySlot.Id, result[0].Id);
    }

    [Fact]
    public async Task GetDoctorDaySlots_MarksOccupiedSlotsAsUnavailable()
    {
        var monday = NextDateFor(DayOfWeek.Monday);
        var slot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var appointment = Appointment.Create(slot, monday, "doc-1", "patient-1", null);

        _doctorRepository.Setup(x => x.ExistsAsync("doc-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _slotRepository.Setup(x => x.ListByDoctorAsync("doc-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([slot]);
        _appointmentRepository.Setup(x => x.ListByDoctorAsync("doc-1", monday, It.IsAny<CancellationToken>()))
            .ReturnsAsync([appointment]);

        var result = await _sut.Handle(new GetDoctorDaySlotsQuery("doc-1", monday), CancellationToken.None);

        Assert.Single(result);
        Assert.False(result[0].IsAvailable);
    }

    [Fact]
    public async Task GetDoctorDaySlots_MarksAvailableSlotsAsAvailable()
    {
        var monday = NextDateFor(DayOfWeek.Monday);
        var slot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));

        _doctorRepository.Setup(x => x.ExistsAsync("doc-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _slotRepository.Setup(x => x.ListByDoctorAsync("doc-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([slot]);
        _appointmentRepository.Setup(x => x.ListByDoctorAsync("doc-1", monday, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.Handle(new GetDoctorDaySlotsQuery("doc-1", monday), CancellationToken.None);

        Assert.Single(result);
        Assert.True(result[0].IsAvailable);
    }

    [Fact]
    public async Task GetDoctorDaySlots_ReturnsCorrectStartAndEndTime()
    {
        var monday = NextDateFor(DayOfWeek.Monday);
        var start = TimeSpan.FromHours(9);
        var end = TimeSpan.FromHours(10);
        var slot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, start, end);

        _doctorRepository.Setup(x => x.ExistsAsync("doc-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _slotRepository.Setup(x => x.ListByDoctorAsync("doc-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([slot]);
        _appointmentRepository.Setup(x => x.ListByDoctorAsync("doc-1", monday, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.Handle(new GetDoctorDaySlotsQuery("doc-1", monday), CancellationToken.None);

        Assert.Equal(start, result[0].StartTime);
        Assert.Equal(end, result[0].EndTime);
    }

    private static DateOnly NextDateFor(DayOfWeek day)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1));
        while (date.DayOfWeek != day)
            date = date.AddDays(1);
        return date;
    }
}
