using Microsoft.Maui.Graphics;

namespace Handog_MobileApp.Models
{
    public class EventModel
    {
        public int EventID { get; set; }
        public string EventTitle { get; set; }
        public string OrganizerName { get; set; }
        public string EventVenue { get; set; }
        public string EventAddress { get; set; }
        public string EventTime { get; set; }
        public string EventDate { get; set; }
        public string EventDetails { get; set; }
        public string EventDescription { get; set; }
        public string Location { get; set; }
        public bool IsMyEvent { get; set; }
        public string CategoryImage { get; set; }

        // --- Approval workflow ---
        public string EventStatus { get; set; }
        public string RejectionReason { get; set; }

        public bool IsPending => string.Equals(EventStatus, "Pending", StringComparison.OrdinalIgnoreCase);
        public bool IsRejected => string.Equals(EventStatus, "Rejected", StringComparison.OrdinalIgnoreCase);
        public bool IsPublished => string.Equals(EventStatus, "Published", StringComparison.OrdinalIgnoreCase);

        // Only the owning organizer may edit & resubmit a rejected event
        public bool CanEdit => IsRejected && IsMyEvent;
        public bool HasRejectionReason => IsRejected && !string.IsNullOrWhiteSpace(RejectionReason);

        // Label + colour for the small status badge shown on the organizer's event card
        public string StatusLabel => (EventStatus ?? string.Empty).ToUpperInvariant();

        public Color StatusBadgeColor => EventStatus?.ToLowerInvariant() switch
        {
            "pending" => Color.FromArgb("#FAD02C"),   // amber
            "published" => Color.FromArgb("#00BAC7"),  // teal
            "rejected" => Color.FromArgb("#FF5A5F"),   // red
            "completed" => Color.FromArgb("#777777"),  // grey
            _ => Colors.Transparent
        };

        public Color StatusTextColor => IsPending ? Color.FromArgb("#1A1A1A") : Colors.White;
    }
}
