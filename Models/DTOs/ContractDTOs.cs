using System;
using System.Collections.Generic;

namespace UserManagementSystem.Models
{
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
        public List<int> SelectedServiceIds { get; set; } = new();
        public int StandardOccupants { get; set; } = 2;
        public decimal ExtraOccupantFee { get; set; } = 0;
    }


}
