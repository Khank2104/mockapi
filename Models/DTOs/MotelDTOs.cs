using System;
using System.Collections.Generic;

namespace UserManagementSystem.Models
{
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

    public class MotelResponse
    {
        public int MotelId { get; set; }
        public string MotelName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Description { get; set; }
        public bool UseFloorManagement { get; set; }
        public string Status { get; set; } = "Active";
        public List<FloorResponse> Floors { get; set; } = new();
        public List<RoomResponse> Rooms { get; set; } = new();
    }

    public class FloorResponse
    {
        public int FloorId { get; set; }
        public int FloorNumber { get; set; }
        public string? FloorName { get; set; }
        public string Status { get; set; } = "Active";
        public List<RoomResponse> Rooms { get; set; } = new();
    }


}
