using Moq;
using PiedraAzul.Application.Features.Doctors.Queries.GetScheduleConfig;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Shared.Enums;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Test.Tests;

public class GetScheduleConfigByDoctorIdHandlerTests
{
    private readonly Mock<IDoctorRepository> _doctorRepository = new();
    private readonly Mock<IDoctorAvailabilitySlotRepository> _slotRepository = new();

    private readonly GetScheduleConfigByDoctorIdHandler _sut;

    public GetScheduleConfigByDoctorIdHandlerTests()
    {
        _sut = new GetScheduleConfigByDoctorIdHandler(
            _doctorRepository.Object,
            _slotRepository.Object);
    }

    [Fact]
    public async Task GetScheduleConfig_WhenDoctorNull_DefaultsBookingWindowTo4()
    {
        SetupSlots();
        _doctorRepository
            .Setup(x => x.GetByIdAsync("doc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Doctor?)null);

        var result = await _sut.Handle(new GetScheduleConfigByDoctorIdQuery("doc-1"), CancellationToken.None);

        Assert.Equal(4, result.BookingWindowWeeks);
    }

    [Fact]
    public async Task GetScheduleConfig_UsesDoctorBookingWindow()
    {
        SetupSlots();
        var doctor = new Doctor("doc-1", DoctorType.NaturalMedicine, "LIC", "");
        doctor.SetBookingWindow(8);
        _doctorRepository
            .Setup(x => x.GetByIdAsync("doc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(doctor);

        var result = await _sut.Handle(new GetScheduleConfigByDoctorIdQuery("doc-1"), CancellationToken.None);

        Assert.Equal(8, result.BookingWindowWeeks);
    }

    [Fact]
    public async Task GetScheduleConfig_DerivesAvailabilityFromActiveSlots()
    {
        // Lunes con dos franjas (9-10, 10-11): habilitado, inicio 9:00, fin 11:00.
        SetupSlots(
            new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10)),
            new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(10), TimeSpan.FromHours(11)));
        SetupDoctor();

        var result = await _sut.Handle(new GetScheduleConfigByDoctorIdQuery("doc-1"), CancellationToken.None);

        var monday = result.Availability.Single(d => d.DayOfWeek == DayOfWeek.Monday);
        Assert.True(monday.IsEnabled);
        Assert.Equal(TimeSpan.FromHours(9), monday.StartTime);
        Assert.Equal(TimeSpan.FromHours(11), monday.EndTime);

        var tuesday = result.Availability.Single(d => d.DayOfWeek == DayOfWeek.Tuesday);
        Assert.False(tuesday.IsEnabled);
        Assert.Equal(TimeSpan.Zero, tuesday.StartTime);
        Assert.Equal(TimeSpan.Zero, tuesday.EndTime);
    }

    [Fact]
    public async Task GetScheduleConfig_ComputesIntervalFromFirstTwoSlots()
    {
        SetupSlots(
            new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromMinutes(9 * 60 + 30)),
            new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromMinutes(9 * 60 + 30), TimeSpan.FromHours(10)));
        SetupDoctor();

        var result = await _sut.Handle(new GetScheduleConfigByDoctorIdQuery("doc-1"), CancellationToken.None);

        Assert.Equal(30, result.IntervalMinutes);
    }

    [Fact]
    public async Task GetScheduleConfig_WhenSingleSlot_IntervalDefaultsTo15()
    {
        SetupSlots(new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10)));
        SetupDoctor();

        var result = await _sut.Handle(new GetScheduleConfigByDoctorIdQuery("doc-1"), CancellationToken.None);

        Assert.Equal(15, result.IntervalMinutes);
    }

    [Fact]
    public async Task GetScheduleConfig_SlotsIncludeDeleted_ButAvailabilityIgnoresThem()
    {
        var active = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var deleted = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Tuesday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        deleted.SoftDelete();
        SetupSlots(active, deleted);
        SetupDoctor();

        var result = await _sut.Handle(new GetScheduleConfigByDoctorIdQuery("doc-1"), CancellationToken.None);

        // Los slots crudos incluyen el borrado.
        Assert.Equal(2, result.Slots.Count);
        Assert.Contains(result.Slots, s => s.Id == deleted.Id && s.IsDeleted);

        // Pero la disponibilidad solo considera los activos: el martes (borrado) queda deshabilitado.
        var tuesday = result.Availability.Single(d => d.DayOfWeek == DayOfWeek.Tuesday);
        Assert.False(tuesday.IsEnabled);
    }

    private void SetupSlots(params DoctorAvailabilitySlot[] slots) =>
        _slotRepository
            .Setup(x => x.ListByDoctorAsync("doc-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slots);

    private void SetupDoctor() =>
        _doctorRepository
            .Setup(x => x.GetByIdAsync("doc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Doctor("doc-1", DoctorType.NaturalMedicine, "LIC", ""));
}
