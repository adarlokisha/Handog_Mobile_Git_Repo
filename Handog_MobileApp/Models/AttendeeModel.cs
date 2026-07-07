namespace Handog_MobileApp.Models
{
    public class AttendeeModel
    {
        public int RegistrationNum { get; set; }
        public int AccountNum { get; set; }
        public string VolunteerName { get; set; }
        public string RegistrationDate { get; set; }
        public string AttendanceStatus { get; set; }

        // --- Dynamic UI Properties ---
        // If they are 'Present', the dot turns Green. Otherwise, it stays Yellow.
        public string StatusColor => AttendanceStatus == "Present" ? "#00C853" : "#FFC107";

        // This hides the manual "Confirm" button in the table if they are already marked Present
        public bool IsNotConfirmed => AttendanceStatus != "Present";
    }
}