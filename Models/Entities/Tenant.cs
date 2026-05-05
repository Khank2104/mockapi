using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementSystem.Models
{
    public class Tenant
    {
        [Key]
        public int TenantId { get; set; }
        public int? UserId { get; set; }
        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;
        [Required]
        [MaxLength(20)]
        public string CitizenId { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        [MaxLength(10)]
        public string? Gender { get; set; }
        [MaxLength(500)]
        public string? PermanentAddress { get; set; }
        [MaxLength(20)]
        public string? Phone { get; set; }
        [MaxLength(200)]
        public string? EmergencyContact { get; set; }
        [MaxLength(500)]
        public string? AvatarUrl { get; set; }
        [Required]
        [MaxLength(20)]
        public string TenantStatus { get; set; } = "Prospective"; // Default: Prospective, Staying, MovedOut, Inactive
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        
        public User? User { get; set; }
        public ICollection<RoomOccupant> RoomOccupancies { get; set; } = new List<RoomOccupant>();
    }
}
