using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementSystem.Models
{
    public class InvoiceDetail
    {
        [Key]
        public int InvoiceDetailId { get; set; }
        public int InvoiceId { get; set; }
        [Required]
        [MaxLength(200)]
        public string ItemName { get; set; } = string.Empty;
        public int? ServiceId { get; set; }
        public double Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        public string? Note { get; set; }

        [ForeignKey("InvoiceId")]
        
        public Invoice Invoice { get; set; } = null!;
        [ForeignKey("ServiceId")]
        
        public Service? Service { get; set; }
    }
}
