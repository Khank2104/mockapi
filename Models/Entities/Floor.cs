using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementSystem.Models
{
    public class Floor
    {
        [Key]
        public int FloorId { get; set; }
        public int MotelId { get; set; }
        public int FloorNumber { get; set; }
        [MaxLength(100)]
        public string? FloorName { get; set; }
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("MotelId")]
        
        public Motel Motel { get; set; } = null!;
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
