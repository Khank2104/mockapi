using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementSystem.Models
{
    public class Service
    {
        [Key]
        public int ServiceId { get; set; }
        [Required]
        [MaxLength(100)]
        public string ServiceName { get; set; } = string.Empty;
        [Required]
        [MaxLength(50)]
        public string ServiceCode { get; set; } = string.Empty;
        [Required]
        [MaxLength(50)]
        public string Unit { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,2)")]
        public decimal DefaultPrice { get; set; }
        [Required]
        [MaxLength(50)]
        public string CalculationType { get; set; } = "Fixed";
        public bool IsSystemDefault { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("CreatedBy")]
        public User? Creator { get; set; }
    }
}
