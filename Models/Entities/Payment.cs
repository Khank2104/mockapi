using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementSystem.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }
        public int InvoiceId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }
        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "Cash";
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public int? ReceivedBy { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("InvoiceId")]
        
        public Invoice Invoice { get; set; } = null!;
        [ForeignKey("ReceivedBy")]
        
        public User? Receiver { get; set; }
    }
}
