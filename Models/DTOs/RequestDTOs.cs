using System;
using System.Collections.Generic;

namespace UserManagementSystem.Models
{
    public class CreateServiceRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RequestType { get; set; } = "Repair"; // Repair, Complaint, Other
    }

    public class UpdateRequestStatus
    {
        public string Status { get; set; } = "Pending"; // Pending, InProgress, Resolved, Rejected
        public string? ResolutionNote { get; set; }
    }

    public class ServiceRequestResponse
    {
        public int RequestId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RequestType { get; set; } = "Repair";
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
        public string? ResolutionNote { get; set; }
        public string? TenantName { get; set; }
        public string? RoomCode { get; set; }
        public string? MotelName { get; set; }
    }


}
