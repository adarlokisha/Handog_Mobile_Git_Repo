
namespace Handog_MobileApp.Models
{
    public class O_EventProposal
    {
        public int ProposalNum { get; set; }
        public string ProposalTitle { get; set; }
        public string ProposalDetails { get; set; }
        public string ProposerName { get; set; }
        public DateTime PreferredDate { get; set; }
        public TimeSpan PreferredStartTime { get; set; }
        public TimeSpan PreferredEndTime { get; set; }
        public int CategoryNum { get; set; }
        public string Proposal_ID { get; set; }

        public string ProposalStatus { get; set; }
    }
}