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

    public class Motel
    {
        [Key]
        public int MotelId { get; set; }
        [Required]
        [MaxLength(200)]
        public string MotelName { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? Address { get; set; }
        public string? Description { get; set; }
        public int OwnerUserId { get; set; }
        public bool UseFloorManagement { get; set; } = true;
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("OwnerUserId")]
        
        public User Owner { get; set; } = null!;
        public ICollection<Floor> Floors { get; set; } = new List<Floor>();
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }

    public class Floor
    {
        [Key]
        public int FloorId { get; set; }
        public int MotelId { get; set; }
        public int FloorNumber { get; set; }
        [MaxLength(100)]
        public string? FloorName { get; set; }
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("MotelId")]
        
        public Motel Motel { get; set; } = null!;
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }

    public class Room
    {
        [Key]
        public int RoomId { get; set; }
        public int MotelId { get; set; }
        public int? FloorId { get; set; }
        [Required]
        [MaxLength(50)]
        public string RoomCode { get; set; } = string.Empty;
        public double Area { get; set; }
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Available";
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("MotelId")]
        
        public Motel Motel { get; set; } = null!;
        [ForeignKey("FloorId")]
        
        public Floor? Floor { get; set; }
        public RoomSetting? Setting { get; set; }
        public ICollection<RoomServiceSetting> ServiceSettings { get; set; } = new List<RoomServiceSetting>();
        public ICollection<RoomOccupant> Occupants { get; set; } = new List<RoomOccupant>();
        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    }

    public class RoomSetting
    {
        [Key]
        public int RoomSettingId { get; set; }
        public int RoomId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseRent { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositAmount { get; set; }
        public int StandardOccupants { get; set; } = 1;
        public int MaxOccupants { get; set; } = 2;
        [Column(TypeName = "decimal(18,2)")]
        public decimal ExtraOccupantFee { get; set; }
        public bool ApplyExtraOccupantFee { get; set; } = false;
        public DateTime EffectiveFrom { get; set; } = DateTime.Now;
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("RoomId")]
        
        public Room Room { get; set; } = null!;
        [ForeignKey("CreatedBy")]
        
        public User? Creator { get; set; }
    }

    public class Service
    {
        [Key]
        public int ServiceId { get; set; }
        [Required]
        [MaxLength(100)]
        public string ServiceName { get; set; } = string.Empty;
        [Required]
        [MaxLength(50)]
        public string ServiceCode { get; set; } = string.Empty;
        [Required]
        [MaxLength(50)]
        public string Unit { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,2)")]
        public decimal DefaultPrice { get; set; }
        [Required]
        [MaxLength(50)]
        public string CalculationType { get; set; } = "Fixed";
        public bool IsSystemDefault { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("CreatedBy")]
        public User? Creator { get; set; }
    }

    public class RoomServiceSetting
    {
        [Key]
        public int RoomServiceSettingId { get; set; }
        public int RoomId { get; set; }
        public int ServiceId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        [Required]
        [MaxLength(50)]
        public string CalculationType { get; set; } = "Fixed";
        public bool IsActive { get; set; } = true;
        public DateTime EffectiveFrom { get; set; } = DateTime.Now;
        public string? Note { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("RoomId")]
        
        public Room Room { get; set; } = null!;
        [ForeignKey("ServiceId")]
        
        public Service Service { get; set; } = null!;
        [ForeignKey("CreatedBy")]
        
        public User? Creator { get; set; }
    }

    public class Tenant
    {
        [Key]
        public int TenantId { get; set; }
        public int? UserId { get; set; }
        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;
        [Required]
        [MaxLength(20)]
        public string CitizenId { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        [MaxLength(10)]
        public string? Gender { get; set; }
        [MaxLength(500)]
        public string? PermanentAddress { get; set; }
        [MaxLength(20)]
        public string? Phone { get; set; }
        [MaxLength(200)]
        public string? EmergencyContact { get; set; }
        [MaxLength(500)]
        public string? AvatarUrl { get; set; }
        [Required]
        [MaxLength(20)]
        public string TenantStatus { get; set; } = "Prospective"; // Default: Prospective, Staying, MovedOut, Inactive
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        
        public User? User { get; set; }
        public ICollection<RoomOccupant> RoomOccupancies { get; set; } = new List<RoomOccupant>();
    }

    public class RoomOccupant
    {
        [Key]
        public int RoomOccupantId { get; set; }
        public int RoomId { get; set; }
        public int TenantId { get; set; }
        [Required]
        [MaxLength(20)]
        public string OccupantRole { get; set; } = "Member";
        public DateTime CheckInDate { get; set; } = DateTime.Now;
        public DateTime? CheckOutDate { get; set; }
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Staying"; // Default: Staying, MovedOut
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("RoomId")]
        public Room Room { get; set; } = null!;
        [ForeignKey("TenantId")]
        public Tenant Tenant { get; set; } = null!;
    }

    public class Contract
    {
        [Key]
        public int ContractId { get; set; }
        public int RoomId { get; set; }
        public int PrimaryTenantId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyRent { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositAmount { get; set; }
        [Required]
        [MaxLength(20)]
        public string ContractStatus { get; set; } = "Active";
        public string? Terms { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("RoomId")]
        
        public Room Room { get; set; } = null!;
        [ForeignKey("PrimaryTenantId")]
        
        public Tenant PrimaryTenant { get; set; } = null!;
        [ForeignKey("CreatedBy")]
        
        public User? Creator { get; set; }
    }

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

    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }
        public int RoomId { get; set; }
        public int PrimaryTenantId { get; set; }
        public int? ContractId { get; set; }
        public int BillingMonth { get; set; }
        public int BillingYear { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal RoomRent { get; set; }
        public int OccupantCount { get; set; }
        public int ExtraOccupantCount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal ExtraOccupantTotal { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal ServiceTotal { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal OtherFee { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        public DateTime DueDate { get; set; }
        [Required]
        [MaxLength(20)]
        public string InvoiceStatus { get; set; } = "Unpaid";
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("RoomId")]
        
        public Room Room { get; set; } = null!;
        [ForeignKey("PrimaryTenantId")]
        
        public Tenant PrimaryTenant { get; set; } = null!;
        [ForeignKey("ContractId")]
        
        public Contract? Contract { get; set; }
        [ForeignKey("CreatedBy")]
        
        public User? Creator { get; set; }
        public ICollection<InvoiceDetail> Details { get; set; } = new List<InvoiceDetail>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }

    public class InvoiceDetail
    {
        [Key]
        public int InvoiceDetailId { get; set; }
        public int InvoiceId { get; set; }
        [Required]
        [MaxLength(200)]
        public string ItemName { get; set; } = string.Empty;
        public int? ServiceId { get; set; }
        public double Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        public string? Note { get; set; }

        [ForeignKey("InvoiceId")]
        
        public Invoice Invoice { get; set; } = null!;
        [ForeignKey("ServiceId")]
        
        public Service? Service { get; set; }
    }

    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }
        public int InvoiceId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }
        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "Cash";
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public int? ReceivedBy { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("InvoiceId")]
        
        public Invoice Invoice { get; set; } = null!;
        [ForeignKey("ReceivedBy")]
        
        public User? Receiver { get; set; }
    }

    public class Request
    {
        [Key]
        public int RequestId { get; set; }
        public int RoomId { get; set; }
        public int TenantId { get; set; }
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [Required]
        [MaxLength(50)]
        public string RequestType { get; set; } = "Repair";
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? HandledBy { get; set; }
        public DateTime? HandledAt { get; set; }
        public string? ResolutionNote { get; set; }

        [ForeignKey("RoomId")]
        
        public Room Room { get; set; } = null!;
        [ForeignKey("TenantId")]
        
        public Tenant Tenant { get; set; } = null!;
        [ForeignKey("HandledBy")]
        
        public User? Handler { get; set; }
    }
}
