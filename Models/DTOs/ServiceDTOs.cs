using System;
using System.Collections.Generic;

namespace UserManagementSystem.Models
{
    public class RoomServiceSettingRequest
    {
        public int RoomId { get; set; }
        public int ServiceId { get; set; }
        public decimal UnitPrice { get; set; }
        public string CalculationType { get; set; } = "Fixed"; // Fixed, Usage
        public bool IsActive { get; set; } = true;
        public string? Note { get; set; }
    }

    public class GlobalServiceUpdateRequest
    {
        public decimal DefaultPrice { get; set; }
    }

    public class ServiceRequest
    {
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceCode { get; set; } = string.Empty;
        public string Unit { get; set; } = "Tháng";
        public decimal DefaultPrice { get; set; }
        public string CalculationType { get; set; } = "fixed"; // metered, per_person, fixed
    }


}
