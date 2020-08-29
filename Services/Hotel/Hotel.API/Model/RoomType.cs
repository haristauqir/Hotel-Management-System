using System.Collections.Generic;
namespace Hotel.API.Model
{
    public class RoomType
    {
        public int Id { get; set; }
        public decimal price { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
        public ICollection<Room> Rooms { get; set; }
    }
}