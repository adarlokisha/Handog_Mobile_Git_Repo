using Handog_AdminWeb.Components.Models;
using Handog_AdminWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace Handog_AdminWeb.Data
{
    public class DashboardService
    {
        private readonly AppDbContext _db;

        public DashboardService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<DashboardKpis> GetKpisAsync()
        {
            return new DashboardKpis
            {
                // Count registrations where the volunteer actually showed up
                TotalAttendance = await _db.EventRegistrations
                    .CountAsync(er => er.AttendanceStatus == "Present" || er.AttendanceStatus == "Attended"),

                // Count events that are currently active
                ActiveEvents = await _db.Events
                    .CountAsync(e => e.EventStatus == "Active" || e.EventStatus == "Upcoming"),

                // Count distinct accounts that are listed as organizers on events
                DistinctOrganizers = await _db.Events
                    .Select(e => e.OrganizerNum)
                    .Distinct()
                    .CountAsync()
            };
        }

        public async Task<List<EventTypeData>> GetEventsByTypeAsync()
        {
            // Join Events and Category tables, group by Category Name
            return await _db.Events
                .Include(e => e.Category)
                .GroupBy(e => e.Category.CategoryName)
                .Select(group => new EventTypeData
                {
                    TypeName = group.Key,
                    EventCount = group.Count()
                })
                .ToListAsync();
        }

        public async Task<List<OrganizerData>> GetOrganizerDataAsync()
        {
            // Group events by the Organizer (Account)
            return await _db.Events
                .Include(e => e.Organizer)
                .GroupBy(e => new { e.Organizer.Firstname, e.Organizer.Lastname, e.Organizer.AccRole })
                .Select(group => new OrganizerData
                {
                    OrganizerName = group.Key.Firstname + " " + group.Key.Lastname,
                    Role = group.Key.AccRole,
                    EventCount = group.Count()
                })
                .OrderByDescending(x => x.EventCount)
                .ToListAsync();
        }

        public async Task<List<MonthlyEventData>> GetMonthlyDataAsync()
        {
            // Get events for the current year, grouped by month
            int currentYear = DateTime.Now.Year;

            var rawData = await _db.Events
                .Where(e => e.EventDate.Year == currentYear)
                .GroupBy(e => e.EventDate.Month)
                .Select(group => new
                {
                    MonthNum = group.Key,
                    EventCount = group.Count()
                })
                .ToListAsync();

            // Convert month numbers to short month names (Jan, Feb, etc.)
            return rawData.Select(d => new MonthlyEventData
            {
                Month = new DateTime(currentYear, d.MonthNum, 1).ToString("MMM"),
                EventCount = d.EventCount
            }).ToList();
        }

        public async Task<List<VolunteerAttendance>> GetAttendanceByEventAsync()
        {
            // Get the top events and count how many volunteers attended them
            return await _db.EventRegistrations
                .Include(er => er.Event)
                .Where(er => er.AttendanceStatus == "Present" || er.AttendanceStatus == "Attended")
                .GroupBy(er => er.Event.EventTitle)
                .Select(group => new VolunteerAttendance
                {
                    EventName = group.Key,
                    AttendanceCount = group.Count()
                })
                .OrderByDescending(x => x.AttendanceCount)
                .Take(10) // Only show top 10 on the chart
                .ToListAsync();
        }
    }
}