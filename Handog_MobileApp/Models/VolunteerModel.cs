using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Handog_MobileApp.Models
{
    public class VolunteerDashboardData
    {
        public string FirstName { get; set; } = string.Empty;
        public int JoinedEvents { get; set; }
        public int TotalEvents { get; set; }

        // Calculates the percentage automatically
        public double ParticipationRate => TotalEvents > 0
            ? Math.Round((double)JoinedEvents / TotalEvents * 100)
            : 0;
    }
}
