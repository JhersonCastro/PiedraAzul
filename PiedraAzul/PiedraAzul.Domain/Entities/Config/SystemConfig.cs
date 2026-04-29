namespace PiedraAzul.Domain.Entities.Config;

public class SystemConfig
{
    public int Id { get; private set; }
    public int BookingWindowWeeks { get; private set; }

    private SystemConfig() { }

    public SystemConfig(int bookingWindowWeeks)
    {
        UpdateBookingWindowWeeks(bookingWindowWeeks);
    }

    public void UpdateBookingWindowWeeks(int bookingWindowWeeks)
    {
        if (bookingWindowWeeks < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(bookingWindowWeeks), "Booking window must be at least 1 week.");
        }

        BookingWindowWeeks = bookingWindowWeeks;
    }

    public bool CanBook(DateTime date)
    {
        return date <= DateTime.UtcNow.AddDays(BookingWindowWeeks * 7);
    }
}
