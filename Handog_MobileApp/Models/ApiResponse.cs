using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Handog_MobileApp.Models
{
    public class ApiResponse
    {
        public string Message { get; set; }
        public string VerificationCode { get; set; } // 👈 Add this so SignUpViewModel compiles
    }
}
