﻿namespace PC2.Models
{
    public class EventsModel
    {
        public List<CalendarEvent> CalendarEvents {  get; set; } = new List<CalendarEvent>();

        public bool IsPC2EventCalendar { get; set; } = true;
        /// <summary>
        // List of all standing meeting events.
        /// </summary>

        public List<string> StandingMeetings { get; set; } = new List<string>();
    }
}
