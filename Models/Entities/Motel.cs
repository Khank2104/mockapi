using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementSystem.Models
{
    public class Motel
    {
        [Key]
        public int MotelId { get; set; }
        [Required]
        [MaxLength(200)]
        public string MotelName { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? Address { get; set; }
        public string? Description { get; set; }
        public int OwnerUserId { get; set; }
        public bool UseFloorManagement { get; set; } = true;
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("OwnerUserId")]
        
        public User Owner { get; set; } = null!;
        public ICollection<Floor> Floors { get; set; } = new List<Floor>();
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
