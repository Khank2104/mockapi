using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementSystem.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }
        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; } = string.Empty;
        [MaxLength(200)]
        public string? Description { get; set; }

        
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
