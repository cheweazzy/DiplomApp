namespace DiplomApp.Helpers
{
    public static class TimeSlotHelper
    {
        // Operating hours: 8 AM to 8 PM
        private const int StartHour = 8;
        private const int EndHour = 20;
        private const int SlotDurationMinutes = 30; // Changed to 30 minutes per slot
        private const int SlotDurationHours = 2; // Legacy support for 2-hour slots

        /// <summary>
        /// Gets all available time slots for a specific date
        /// </summary>
        public static List<DateTime> GetTimeSlotsForDate(DateTime date)
        {
            var slots = new List<DateTime>();
            var baseDate = date.Date; // Strip time component

            for (int hour = StartHour; hour < EndHour; hour += SlotDurationHours)
            {
                slots.Add(baseDate.AddHours(hour));
            }

            return slots;
        }

        /// <summary>
        /// Validates if a given DateTime is a valid time slot
        /// </summary>
        public static bool IsValidTimeSlot(DateTime dateTime)
        {
            // Check if the hour is one of our valid slots
            var hour = dateTime.Hour;
            
            // Must be on the hour (no minutes, seconds, or milliseconds)
            if (dateTime.Minute != 0 || dateTime.Second != 0 || dateTime.Millisecond != 0)
                return false;

            // Must be within operating hours and at a valid slot
            if (hour < StartHour || hour >= EndHour)
                return false;

            // Must be at a 2-hour interval starting from StartHour
            return (hour - StartHour) % SlotDurationHours == 0;
        }

        /// <summary>
        /// Gets the formatted display string for a time slot
        /// </summary>
        public static string FormatTimeSlot(DateTime dateTime)
        {
            var endTime = dateTime.AddHours(SlotDurationHours);
            return $"{dateTime:HH:mm} - {endTime:HH:mm}";
        }

        /// <summary>
        /// Gets all time slots as display strings
        /// </summary>
        public static List<string> GetAllTimeSlotDisplayStrings()
        {
            var displayStrings = new List<string>();
            
            for (int hour = StartHour; hour < EndHour; hour += SlotDurationHours)
            {
                var start = new DateTime(1, 1, 1, hour, 0, 0);
                var end = start.AddHours(SlotDurationHours);
                displayStrings.Add($"{start:HH:mm} - {end:HH:mm}");
            }

            return displayStrings;
        }

        /// <summary>
        /// Rounds a datetime to the nearest valid time slot
        /// </summary>
        public static DateTime RoundToNearestSlot(DateTime dateTime)
        {
            var baseDate = dateTime.Date;
            var hour = dateTime.Hour;

            // Find the closest valid slot
            int nearestSlotHour = StartHour;
            int minDiff = Math.Abs(hour - StartHour);

            for (int h = StartHour; h < EndHour; h += SlotDurationHours)
            {
                int diff = Math.Abs(hour - h);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    nearestSlotHour = h;
                }
            }

            return baseDate.AddHours(nearestSlotHour);
        }

        // ========== New methods for 30-minute slots ==========

        /// <summary>
        /// Gets all available 30-minute time slots for a specific date (8:00 - 20:00)
        /// </summary>
        public static List<DateTime> GetTimeSlots30MinutesForDate(DateTime date)
        {
            var slots = new List<DateTime>();
            var baseDate = date.Date;

            // Generate slots every 30 minutes from 8:00 to 19:30 (last slot starts at 19:30, ends at 20:00)
            for (int hour = StartHour; hour < EndHour; hour++)
            {
                for (int minute = 0; minute < 60; minute += SlotDurationMinutes)
                {
                    // Skip if this would go past EndHour
                    if (hour == EndHour - 1 && minute >= SlotDurationMinutes)
                        break;

                    slots.Add(baseDate.AddHours(hour).AddMinutes(minute));
                }
            }

            return slots;
        }

        /// <summary>
        /// Validates if a given DateTime is a valid 30-minute time slot
        /// </summary>
        public static bool IsValid30MinuteTimeSlot(DateTime dateTime)
        {
            var hour = dateTime.Hour;
            var minute = dateTime.Minute;

            // Must be within operating hours
            if (hour < StartHour || hour >= EndHour)
                return false;

            // Must be at 30-minute intervals (0 or 30 minutes)
            if (minute != 0 && minute != 30)
                return false;

            // Must not have seconds or milliseconds
            if (dateTime.Second != 0 || dateTime.Millisecond != 0)
                return false;

            // Last valid slot is 19:30 (ends at 20:00)
            if (hour == EndHour - 1 && minute >= SlotDurationMinutes)
                return false;

            return true;
        }

        /// <summary>
        /// Gets the formatted display string for a 30-minute time slot
        /// </summary>
        public static string Format30MinuteTimeSlot(DateTime dateTime)
        {
            var endTime = dateTime.AddMinutes(SlotDurationMinutes);
            return $"{dateTime:HH:mm} - {endTime:HH:mm}";
        }

        /// <summary>
        /// Gets all 30-minute time slots as display strings
        /// </summary>
        public static List<string> GetAll30MinuteTimeSlotDisplayStrings()
        {
            var displayStrings = new List<string>();

            for (int hour = StartHour; hour < EndHour; hour++)
            {
                for (int minute = 0; minute < 60; minute += SlotDurationMinutes)
                {
                    if (hour == EndHour - 1 && minute >= SlotDurationMinutes)
                        break;

                    var start = new DateTime(1, 1, 1, hour, minute, 0);
                    var end = start.AddMinutes(SlotDurationMinutes);
                    displayStrings.Add($"{start:HH:mm} - {end:HH:mm}");
                }
            }

            return displayStrings;
        }

        /// <summary>
        /// Converts DateTime to minutes from midnight (for storage)
        /// </summary>
        public static int DateTimeToMinutes(DateTime dateTime)
        {
            return dateTime.Hour * 60 + dateTime.Minute;
        }

        /// <summary>
        /// Converts minutes from midnight to DateTime (for a specific date)
        /// </summary>
        public static DateTime MinutesToDateTime(DateTime baseDate, int minutesFromMidnight)
        {
            var hours = minutesFromMidnight / 60;
            var minutes = minutesFromMidnight % 60;
            return baseDate.Date.AddHours(hours).AddMinutes(minutes);
        }

        /// <summary>
        /// Gets all possible 30-minute slot times as minutes from midnight
        /// </summary>
        public static List<int> GetAll30MinuteSlotMinutes()
        {
            var minutes = new List<int>();

            for (int hour = StartHour; hour < EndHour; hour++)
            {
                for (int minute = 0; minute < 60; minute += SlotDurationMinutes)
                {
                    if (hour == EndHour - 1 && minute >= SlotDurationMinutes)
                        break;

                    minutes.Add(hour * 60 + minute);
                }
            }

            return minutes;
        }
    }
}


