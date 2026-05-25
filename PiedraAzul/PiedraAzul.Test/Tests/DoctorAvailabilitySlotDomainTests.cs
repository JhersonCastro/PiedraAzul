using PiedraAzul.Domain.Common.Exceptions;
using PiedraAzul.Domain.Entities.Profiles.Doctor;

namespace PiedraAzul.Test.Tests;

public class DoctorAvailabilitySlotDomainTests
{
    [Fact]
    public void Create_WithValidRange_Succeeds()
    {
        var slot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));

        Assert.Equal("doc-1", slot.DoctorId);
        Assert.Equal(DayOfWeek.Monday, slot.DayOfWeek);
        Assert.Equal(TimeSpan.FromHours(9), slot.StartTime);
        Assert.Equal(TimeSpan.FromHours(10), slot.EndTime);
        Assert.False(slot.IsDeleted);
    }

    [Fact]
    public void Create_WithInvalidRange_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(
            () => new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(10), TimeSpan.FromHours(9)));

        Assert.Equal("Invalid range", ex.Message);
    }

    [Fact]
    public void Create_WhenStartEqualsEnd_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(
            () => new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(9)));
    }

    [Fact]
    public void Matches_WhenDateMatchesDayOfWeek_ReturnsTrue()
    {
        var slot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var monday = NextDateFor(DayOfWeek.Monday);

        Assert.True(slot.Matches(monday));
    }

    [Fact]
    public void Matches_WhenDateDoesNotMatchDayOfWeek_ReturnsFalse()
    {
        var slot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var tuesday = NextDateFor(DayOfWeek.Tuesday);

        Assert.False(slot.Matches(tuesday));
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedTrue()
    {
        var slot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));

        slot.SoftDelete();

        Assert.True(slot.IsDeleted);
        Assert.NotNull(slot.DeletedAt);
    }

    [Fact]
    public void SoftDelete_SetsDeletedAtToNow()
    {
        var before = DateTime.UtcNow;
        var slot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));

        slot.SoftDelete();

        var after = DateTime.UtcNow;
        Assert.InRange(slot.DeletedAt!.Value, before, after);
    }

    [Fact]
    public void Restore_ClearsIsDeleted()
    {
        var slot = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        slot.SoftDelete();

        slot.Restore();

        Assert.False(slot.IsDeleted);
        Assert.Null(slot.DeletedAt);
    }

    [Fact]
    public void Create_GeneratesUniqueId()
    {
        var s1 = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        var s2 = new DoctorAvailabilitySlot("doc-1", DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));

        Assert.NotEqual(s1.Id, s2.Id);
    }

    private static DateOnly NextDateFor(DayOfWeek day)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1));
        while (date.DayOfWeek != day)
            date = date.AddDays(1);
        return date;
    }
}
