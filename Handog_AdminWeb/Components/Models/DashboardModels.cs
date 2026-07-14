using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Handog_AdminWeb.Components.Models
{
    // for statistics page
    public class DashboardKpis
    {
        public int TotalAttendance { get; set; }
        public int ActiveEvents { get; set; }
        public int DistinctOrganizers { get; set; }
    }

    public class VolunteerAttendance
    {
        public string EventName { get; set; }
        public int AttendanceCount { get; set; }
    }

    public class MonthlyEventData
    {
        public string Month { get; set; }
        public int EventCount { get; set; }
    }

    public class EventTypeData
    {
        public string TypeName { get; set; }
        public int EventCount { get; set; }
    }

    public class OrganizerData
    {
        public string OrganizerName { get; set; }
        public string Role { get; set; }
        public int EventCount { get; set; }
    }

    // database tables
    [Table("LOCALE")]
    public class Locale
    {
        [Key]
        public int LocaleNum { get; set; }

        public string Locale_ID { get; set; }
        public string LocaleName { get; set; }
        public string LocaleAddress { get; set; }
    }

    [Table("EVENTCATEGORY")]
    public class EventCategory
    {
        [Key]
        public int CategoryNum { get; set; }

        public string CategoryName { get; set; }
    }

    [Table("ACCOUNT")]
    public class Account
    {
        [Key]
        public int AccountNum { get; set; }

        public string Account_ID { get; set; }
        public string Lastname { get; set; }
        public string Firstname { get; set; }
        public string Email { get; set; }
        public string ContactNum { get; set; }
        public string AccPassword { get; set; }
        public string AccRole { get; set; }
        public string AccountStatus { get; set; }
        public string ChurchID { get; set; }

        // This is nullable (INT NULL) in your SQL, so we use int? in C#
        public int? LocaleNum { get; set; }
        public int AbsenceCount { get; set; }
        public string ProfilePicUrl { get; set; }

        // This tells EF Core about your Foreign Key relationship!
        [ForeignKey("LocaleNum")]
        public Locale Locale { get; set; }
    }
}
