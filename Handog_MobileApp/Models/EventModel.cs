using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Handog_MobileApp.Models
{
    public class EventModel
    {
        // Primary Key from your database
        public int EventNum { get; set; }

        // Match the VARCHAR/NVARCHAR lengths using strings
        public string Event_ID { get; set; }
        public string EventTitle { get; set; }
        public string EventDescription { get; set; }

        // Maps directly to SQL DATE and TIME types
        public DateTime EventDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        // Maps to SQL INT types
        public int ExpectedParticipants { get; set; }
        public int VolunteerCapacity { get; set; }


        public string Location { get; set; } = "Not Specified";
        public string EventImage { get; set; } = "calendar_icon.png"; // Fallback asset name

        // Helpers to format the data cleanly for your XAML CollectionView
        public string FormattedDate => EventDate.ToString("MMMM dd, yyyy");
        public string FormattedTime => $"{DateTime.Today.Add(StartTime):hh:mm tt} - {DateTime.Today.Add(EndTime):hh:mm tt}";
    }
}
