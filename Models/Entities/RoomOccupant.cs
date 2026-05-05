using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementSystem.Models
{
    public class RoomOccupant
    {
        [Key]
        public int RoomOccupantId { get; set; }
        public int RoomId { get; set; }
        public int TenantId { get; set; }
        [Required]
        [MaxLength(20)]
        public string OccupantRole { get; set; } = "Member";
        public DateTime CheckInDate { get; set; } = DateTime.Now;
        public DateTime? CheckOutDate { get; set; }
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Staying"; // Default: Staying, MovedOut
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("RoomId")]
        public Room Room { get; set; } = null!;
        [ForeignKey("TenantId")]
        public Tenant Tenant { get; set; } = null!;
    }
}
