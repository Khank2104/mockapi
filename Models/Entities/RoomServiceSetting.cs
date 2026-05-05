using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementSystem.Models
{
    public class RoomServiceSetting
    {
        [Key]
        public int RoomServiceSettingId { get; set; }
        public int RoomId { get; set; }
        public int ServiceId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        [Required]
        [MaxLength(50)]
        public string CalculationType { get; set; } = "Fixed";
        public bool IsActive { get; set; } = true;
        public DateTime EffectiveFrom { get; set; } = DateTime.Now;
        public string? Note { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("RoomId")]
        
        public Room Room { get; set; } = null!;
        [ForeignKey("ServiceId")]
        
        public Service Service { get; set; } = null!;
        [ForeignKey("CreatedBy")]
        
        public User? Creator { get; set; }
    }
}
