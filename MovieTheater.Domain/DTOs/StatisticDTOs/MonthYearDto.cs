namespace MovieTheater.Domain.DTOs.StatisticDTOs
{
    public class MonthYearDto
    {
        public int Month { get; set; } // 1-12
        public int Year { get; set; }  // e.g., 2023

        // Default constructor: sets Year to current year
        public MonthYearDto()
        {
            Month = DateTime.UtcNow.Month;
            Year = DateTime.UtcNow.Year;
        }

        // Constructor with month only: sets Year to current year
        public MonthYearDto(int month)
        {
            Month = month;
            Year = DateTime.UtcNow.Year;
        }

        // Constructor with month and year
        public MonthYearDto(int month, int year)
        {
            Month = month;
            Year = year;
        }

        public override string ToString()
        {
            return $"{Month:00}/{Year}";
        }
    }
}
