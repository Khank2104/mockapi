using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementSystem.Models
{
    public class MeterReading
    {
        [Key]
        public int ReadingId { get; set; }
        public int RoomId { get; set; }
        public int ServiceId { get; set; }
        public double PreviousReading { get; set; }
        public double CurrentReading { get; set; }
        public double UsageAmount { get; set; }
        public int BillingMonth { get; set; }
        public int BillingYear { get; set; }
        public int? RecordedBy { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.Now;
        public string? Note { get; set; }

        [ForeignKey("RoomId")]
        
        public Room Room { get; set; } = null!;
        [ForeignKey("ServiceId")]
        
        public Service Service { get; set; } = null!;
        [ForeignKey("RecordedBy")]
        
        public User? Recorder { get; set; }
    }
}
