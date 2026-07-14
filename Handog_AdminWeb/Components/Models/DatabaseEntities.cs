using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Handog_AdminWeb.Models
{
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
        public int? LocaleNum { get; set; }
        public int AbsenceCount { get; set; }
        public string ProfilePicUrl { get; set; }

        [ForeignKey("LocaleNum")]
        public virtual Locale Locale { get; set; }
    }

    [Table("EVENT")]
    public class Event
    {
        [Key]
        public int EventNum { get; set; }
        public string Event_ID { get; set; }
        public int OrganizerNum { get; set; }
        public int? ProposalNum { get; set; }
        public int CategoryNum { get; set; }
        public int? LocaleNum { get; set; }
        public string EventTitle { get; set; }
        public string EventDescription { get; set; }
        public DateTime EventDate { get; set; }
        public string EventAddress { get; set; }
        public string EventVenue { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int ExpectedParticipants { get; set; }
        public int VolunteerCapacity { get; set; }
        public string EventStatus { get; set; }
        public string AdminRemarks { get; set; }
        public DateTime DateCreated { get; set; }
        public string RejectionReason { get; set; }
        public bool EventCompleted { get; set; }

        // Navigation Properties based on ERD lines
        [ForeignKey("CategoryNum")]
        public virtual EventCategory Category { get; set; }

        [ForeignKey("OrganizerNum")]
        public virtual Account Organizer { get; set; }

        [ForeignKey("LocaleNum")]
        public virtual Locale Locale { get; set; }
    }

    [Table("EVENTREGISTRATION")]
    public class EventRegistration
    {
        [Key]
        public int RegistrationNum { get; set; }
        public string Registration_ID { get; set; }
        public int EventNum { get; set; }
        public int AccountNum { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string RegistrationStatus { get; set; }
        public string AttendanceStatus { get; set; } // e.g., "Present", "Absent"

        [ForeignKey("EventNum")]
        public virtual Event Event { get; set; }

        [ForeignKey("AccountNum")]
        public virtual Account Account { get; set; }
    }
}