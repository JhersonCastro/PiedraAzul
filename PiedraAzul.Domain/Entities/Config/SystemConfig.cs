using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Domain.Entities.Config
{
    public class SystemConfig
    {
        public int BookingWindowWeeks { get; private set; }

        private SystemConfig() { }

        public SystemConfig(int bookingWindowWeeks)
        {
            BookingWindowWeeks = bookingWindowWeeks;
        }

        public bool CanBook(DateTime date)
        {
            return date <= DateTime.UtcNow.AddDays(BookingWindowWeeks * 7);
        }
    }
}
