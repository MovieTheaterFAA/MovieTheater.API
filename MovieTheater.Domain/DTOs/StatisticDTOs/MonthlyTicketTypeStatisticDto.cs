namespace MovieTheater.Domain.DTOs.StatisticDTOs
{
    public class MonthlyTicketTypeStatisticDto
    {
        public int OnlineTicketCount { get; set; }
        public int OfflineTicketCount { get; set; }
        public int GuestTicketCount { get; set; }
        public int TicketCount { get; set; }
    }
}
