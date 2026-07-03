namespace Handog_MobileApp.Models
{
    public class EventModel
    {
        public int EventID { get; set; }
        public string EventTitle { get; set; }
        public string OrganizerName { get; set; }
        public string EventVenue { get; set; }
        public string EventAddress { get; set; }
        public string EventTime { get; set; }
        public string EventDate { get; set; }
        public string EventDetails { get; set; }
        public string EventDescription { get; set; }
        public string Location { get; set; }
        public bool IsMyEvent { get; set; }
        public string CategoryImage { get; set; }
    }
}