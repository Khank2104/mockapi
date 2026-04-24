using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementSystem.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        
        [Required, MaxLength(100)]
        public string Title { get; set; } = string.Empty;
        
        [Required, MaxLength(500)]
        public string Message { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string Type { get; set; } = "info"; 
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);
        public bool IsRead { get; set; } = false;
        
        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
