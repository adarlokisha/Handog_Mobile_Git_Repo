using Microsoft.Maui.Graphics;

namespace Handog_MobileApp.Models
{
    public class ReportsModel
    {
        public string VolunteerName { get; set; }
        public string AttendanceStatus { get; set; }

        // Dynamically turns Green for Present, Red for Absent
        public Color StatusColor => AttendanceStatus == "Present" ? Colors.Green : Colors.Red;
    }
}