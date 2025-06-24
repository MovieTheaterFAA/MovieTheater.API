using MovieTheater.Domain.Enums;

namespace MovieTheater.Domain.DTOs.CinemaRoomDTOs
{
    public class CinemaRoomDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public RoomType Type { get; set; }
    }

    public class CreateCinemaRoomDto
    {
        public string Name { get; set; }
        public RoomType Type { get; set; }
    }

    public class UpdateCinemaRoomDto
    {
        public string Name { get; set; }
        public RoomType Type { get; set; }
    }
}
