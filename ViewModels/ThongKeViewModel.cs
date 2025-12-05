using VinhuniEvent.Models;

namespace VinhuniEvent.ViewModels
{
    public class ThongKeViewModel
    {
        public int TotalEvents { get; set; }
        public int TotalRegistrations { get; set; }
        public int TotalUsers { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalStudents { get; set; }
        public int TotalOrganizers { get; set; }
        public int UpcomingEvents { get; set; }
        public int OngoingEvents { get; set; }

        public List<TopEventDto> TopEvents { get; set; }

        public List<Event> LatestEvents { get; set; }

        public int TotalCheckIn { get; set; }

        public int TotalAbsent { get; set; }
    }
    public class TopEventDto
    {
        public string? EventName { get; set; }
        public int RegistrationCount { get; set; }
        public DateTime StartTime { get; set;      }
    }

   
}
