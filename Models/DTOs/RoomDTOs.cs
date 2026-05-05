using System;
using System.Collections.Generic;

namespace UserManagementSystem.Models
{
    public class RoomRequest
    {
        public int MotelId { get; set; }
        public int? FloorId { get; set; }
        public string RoomCode { get; set; } = string.Empty;
        public double Area { get; set; }
        public string Status { get; set; } = "Available"; // Available, Occupied, Maintenance
        public string? Description { get; set; }
    }

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

    public class RoomResponse
    {
        public int RoomId { get; set; }
        public string RoomCode { get; set; } = string.Empty;
        public double Area { get; set; }
        public string Status { get; set; } = "Available";
        public string? Description { get; set; }
        public decimal? CurrentRent { get; set; }
    }


}
