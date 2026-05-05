using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementSystem.Models
{
    public class Contract
    {
        [Key]
        public int ContractId { get; set; }
        public int RoomId { get; set; }
        public int PrimaryTenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyRent { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositAmount { get; set; }
        [Required]
        [MaxLength(20)]
        public string ContractStatus { get; set; } = "Active";
        public string? Terms { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("RoomId")]
        
        public Room Room { get; set; } = null!;
        [ForeignKey("PrimaryTenantId")]
        
        public Tenant PrimaryTenant { get; set; } = null!;
        [ForeignKey("CreatedBy")]
        
        public User? Creator { get; set; }
    }
}
