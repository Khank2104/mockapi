using System;
using System.Collections.Generic;

namespace UserManagementSystem.Models
{
    // --- Admin & Account Management ---
    public class CreateAdminRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }

    public class CreateTenantAccountRequest
    {
        public int TenantId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    // --- Tenant Profile ---
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

    // --- Motel Structure ---
    public class MotelRequest
    {
        public string MotelName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Description { get; set; }
        public bool UseFloorManagement { get; set; } = true;
    }

    public class FloorRequest
    {
        public int MotelId { get; set; }
        public int FloorNumber { get; set; }
        public string? FloorName { get; set; }
        public string Status { get; set; } = "Active"; // Active, Inactive, Maintenance
        public string? Description { get; set; }
    }

    public class RoomRequest
    {
        public int MotelId { get; set; }
        public int? FloorId { get; set; }
        public string RoomCode { get; set; } = string.Empty;
        public double Area { get; set; }
        public string Status { get; set; } = "Available"; // Available, Occupied, Maintenance
        public string? Description { get; set; }
    }

    // --- Room & Service Settings ---
    public class RoomSettingRequest
    {
        public int RoomId { get; set; }
        public decimal BaseRent { get; set; }
        public decimal DepositAmount { get; set; }
        public int StandardOccupants { get; set; } = 1;
        public int MaxOccupants { get; set; } = 2;
        public decimal ExtraOccupantFee { get; set; } = 0;
        public bool ApplyExtraOccupantFee { get; set; } = false;
        public DateTime EffectiveFrom { get; set; } = DateTime.Now;
    }

    public class RoomServiceSettingRequest
    {
        public int RoomId { get; set; }
        public int ServiceId { get; set; }
        public decimal UnitPrice { get; set; }
        public string CalculationType { get; set; } = "Fixed"; // Fixed, Usage
        public bool IsActive { get; set; } = true;
        public string? Note { get; set; }
    }

    // --- Occupancy & Contracts ---
    public class RoomOccupantRequest
    {
        public int RoomId { get; set; }
        public int TenantId { get; set; }
        public string OccupantRole { get; set; } = "Member"; // Primary, Member
        public DateTime CheckInDate { get; set; } = DateTime.Now;
    }

    public class ContractRequest
    {
        public int RoomId { get; set; }
        public int PrimaryTenantId { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime? EndDate { get; set; }
        public decimal MonthlyRent { get; set; }
        public decimal DepositAmount { get; set; }
        public string? Terms { get; set; }
    }
}
