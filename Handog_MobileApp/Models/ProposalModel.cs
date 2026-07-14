using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Handog_MobileApp.Models
{
    public class ProposalModel
    {
        public string RequestorName { get; set; } = "Unknown";
        public string RequestType { get; set; } = "General";
        public string Description { get; set; } = string.Empty;

    }
}
