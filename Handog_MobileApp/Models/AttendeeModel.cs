using Microsoft.Maui.Graphics; // Put this at the very top of the file

namespace Handog_MobileApp
{
    public class AttendeeModel
    {
        public int RegistrationNum { get; set; }
        public int AccountNum { get; set; }
        public string VolunteerName { get; set; }
        public string RegistrationDate { get; set; }
        public string AttendanceStatus { get; set; }

        // --- ADD THESE TWO UI HELPERS: ---

        // If they are Present, turn the dot Teal. If not, turn it Orange.
        public Color StatusColor => AttendanceStatus == "Present"
            ? Color.FromArgb("#00BAC7")
            : Color.FromArgb("#FFA500");

        // If status is "Present", return FALSE (hiding the confirm button).
        public bool IsNotConfirmed => AttendanceStatus != "Present";
    }
}