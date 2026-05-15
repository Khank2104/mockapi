using System;
using System.Collections.Generic;

namespace UserManagementSystem.Models
{
    public class CreateTenantProfileRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string CitizenId { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? PermanentAddress { get; set; }
        public string? EmergencyContact { get; set; }
    }

    public class UpdateTenantProfileRequest : CreateTenantProfileRequest
    {
        public string TenantStatus { get; set; } = "Prospective";
    }

    public class CreateTenantRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = "123456";
    }

    public class TenantResponse
    {
        public int TenantId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string IdCard { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string Status { get; set; } = "Active";
        public string? CurrentRoomCode { get; set; }
        public bool HasActiveContract { get; set; }
        public decimal Balance { get; set; }
    }


}
