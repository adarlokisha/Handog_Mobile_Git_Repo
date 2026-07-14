namespace Handog_API.Models
{
    public class Account
    {
        public int AccountNum { get; set; }              // PK
        public string Account_ID { get; set; }           // NVARCHAR(10)
        public string Lastname { get; set; }
        public string Firstname { get; set; }
        public string Email { get; set; }
        public string ContactNum { get; set; }
        public string AccPassword { get; set; }
        public string AccRole { get; set; }
        public string AccountStatus { get; set; }
        public string? ChurchID { get; set; }
        public int? LocaleNum { get; set; }              // FK to LOCALE
        public int AbsenceCount { get; set; }            // Default 0
        public string? ProfilePicUrl { get; set; }
    }
}
