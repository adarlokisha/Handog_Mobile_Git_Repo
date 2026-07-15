using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Handog_MobileApp.Models
{
    public class Account
    {
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
    }
}
