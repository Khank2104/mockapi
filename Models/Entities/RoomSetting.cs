using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementSystem.Models
{
    public class RoomSetting
    {
        [Key]
        public int RoomSettingId { get; set; }
        public int RoomId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseRent { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositAmount { get; set; }
        public int StandardOccupants { get; set; } = 1;
        public int MaxOccupants { get; set; } = 2;
        [Column(TypeName = "decimal(18,2)")]
        public decimal ExtraOccupantFee { get; set; }
        public bool ApplyExtraOccupantFee { get; set; } = false;
        public DateTime EffectiveFrom { get; set; } = DateTime.Now;
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("RoomId")]
        
        public Room Room { get; set; } = null!;
        [ForeignKey("CreatedBy")]
        
        public User? Creator { get; set; }
    }
}
