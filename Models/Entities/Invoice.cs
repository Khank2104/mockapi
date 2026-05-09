using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementSystem.Models
{
    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }
        public int RoomId { get; set; }
        public int PrimaryTenantId { get; set; }
        public int? ContractId { get; set; }
        public int BillingMonth { get; set; }
        public int BillingYear { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal RoomRent { get; set; }
        public int OccupantCount { get; set; }
        public int ExtraOccupantCount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal ExtraOccupantTotal { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal ServiceTotal { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal OtherFee { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        public DateTime DueDate { get; set; }
        [Required]
        [MaxLength(20)]
        public string InvoiceStatus { get; set; } = "Unpaid";
        public string? PaymentProofPath { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("RoomId")]
        
        public Room Room { get; set; } = null!;
        [ForeignKey("PrimaryTenantId")]
        
        public Tenant PrimaryTenant { get; set; } = null!;
        [ForeignKey("ContractId")]
        
        public Contract? Contract { get; set; }
        [ForeignKey("CreatedBy")]
        
        public User? Creator { get; set; }
        public ICollection<InvoiceDetail> Details { get; set; } = new List<InvoiceDetail>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
