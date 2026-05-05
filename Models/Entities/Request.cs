using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementSystem.Models
{
    public class Request
    {
        [Key]
        public int RequestId { get; set; }
        public int RoomId { get; set; }
        public int TenantId { get; set; }
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [Required]
        [MaxLength(50)]
        public string RequestType { get; set; } = "Repair";
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? HandledBy { get; set; }
        public DateTime? HandledAt { get; set; }
        public string? ResolutionNote { get; set; }

        [ForeignKey("RoomId")]
        
        public Room Room { get; set; } = null!;
        [ForeignKey("TenantId")]
        
        public Tenant Tenant { get; set; } = null!;
        [ForeignKey("HandledBy")]
        
        public User? Handler { get; set; }
    }
}
