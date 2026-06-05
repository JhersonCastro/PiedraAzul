using Moq;
using PiedraAzul.Application.Common.Caching;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Notifications;
using PiedraAzul.Application.Features.Appointments.Notifications;

namespace PiedraAzul.Test.Tests;

public class AppointmentCacheInvalidationHandlerTests
{
    private readonly Mock<ICacheService> _cache = new();
    private readonly AppointmentCacheInvalidationHandler _sut;

    public AppointmentCacheInvalidationHandlerTests()
    {
        _sut = new AppointmentCacheInvalidationHandler(_cache.Object);
    }

    [Fact]
    public async Task Created_InvalidatesSingleDay()
    {
        var date = new DateOnly(2026, 6, 8);

        await _sut.Handle(Notification(AppointmentChange.Created, "doc-1", date), CancellationToken.None);

        _cache.Verify(x => x.Remove(CacheKeys.DoctorDaySlots("doc-1", date)), Times.Once);
        _cache.Verify(x => x.RemoveByTag(CacheKeys.TagDoctorDays("doc-1")), Times.Once);
        _cache.Verify(x => x.Remove(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Cancelled_InvalidatesSingleDay()
    {
        var date = new DateOnly(2026, 6, 8);

        await _sut.Handle(Notification(AppointmentChange.Cancelled, "doc-1", date), CancellationToken.None);

        _cache.Verify(x => x.Remove(CacheKeys.DoctorDaySlots("doc-1", date)), Times.Once);
        _cache.Verify(x => x.RemoveByTag(CacheKeys.TagDoctorDays("doc-1")), Times.Once);
        _cache.Verify(x => x.Remove(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Rescheduled_InvalidatesBothNewAndOldDays()
    {
        var newDate = new DateOnly(2026, 6, 10);
        var oldDate = new DateOnly(2026, 6, 8);

        var notification = new AppointmentNotification(
            AppointmentChange.Rescheduled,
            PatientUserId: "user-1",
            PatientGuestId: null,
            DoctorId: "doc-new",
            SlotId: Guid.NewGuid(),
            Date: newDate,
            OldDoctorId: "doc-old",
            OldDate: oldDate);

        await _sut.Handle(notification, CancellationToken.None);

        // Día/doctor nuevo
        _cache.Verify(x => x.Remove(CacheKeys.DoctorDaySlots("doc-new", newDate)), Times.Once);
        _cache.Verify(x => x.RemoveByTag(CacheKeys.TagDoctorDays("doc-new")), Times.Once);
        // Día/doctor de origen
        _cache.Verify(x => x.Remove(CacheKeys.DoctorDaySlots("doc-old", oldDate)), Times.Once);
        _cache.Verify(x => x.RemoveByTag(CacheKeys.TagDoctorDays("doc-old")), Times.Once);
        // Exactamente dos invalidaciones puntuales (no más).
        _cache.Verify(x => x.Remove(It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Rescheduled_WithoutOldData_InvalidatesOnlyNewDay()
    {
        var newDate = new DateOnly(2026, 6, 10);

        // Sin OldDoctorId/OldDate: solo se invalida el día nuevo.
        await _sut.Handle(Notification(AppointmentChange.Rescheduled, "doc-1", newDate), CancellationToken.None);

        _cache.Verify(x => x.Remove(CacheKeys.DoctorDaySlots("doc-1", newDate)), Times.Once);
        _cache.Verify(x => x.Remove(It.IsAny<string>()), Times.Once);
    }

    private static AppointmentNotification Notification(AppointmentChange change, string doctorId, DateOnly date) =>
        new(change, PatientUserId: "user-1", PatientGuestId: null, DoctorId: doctorId, SlotId: Guid.NewGuid(), Date: date);
}
