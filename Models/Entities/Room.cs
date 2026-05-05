using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementSystem.Models
{
    public class Room
    {
        [Key]
        public int RoomId { get; set; }
        public int MotelId { get; set; }
        public int? FloorId { get; set; }
        [Required]
        [MaxLength(50)]
        public string RoomCode { get; set; } = string.Empty;
        public double Area { get; set; }
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Available";
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("MotelId")]
        
        public Motel Motel { get; set; } = null!;
        [ForeignKey("FloorId")]
        
        public Floor? Floor { get; set; }
        public RoomSetting? Setting { get; set; }
        public ICollection<RoomServiceSetting> ServiceSettings { get; set; } = new List<RoomServiceSetting>();
        public ICollection<RoomOccupant> Occupants { get; set; } = new List<RoomOccupant>();
        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    }
}
