using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hotel.API.Model
{
    public class Room
    {
        public int Id { get; set; }
        public bool Available { get; set; }
        public string Description { get; set; }
        public int MaximumGuests { get; set; }
        public int Number { get; set; }
         [Column(TypeName = "decimal(18,4)")]
        public decimal Price { get; set; }
        public int? HotelId { get; set; }
        public Hotel Hotel { get; set; }
        public int RoomTypeId { get; set; }
        public RoomType RoomType { get; set; }
        public ICollection<RoomFacilities> RoomFacilities { get; set; }
        public ICollection<Images> Images { get; set; }
    }
}