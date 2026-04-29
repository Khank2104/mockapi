using System;
using System.Collections.Generic;

namespace UserManagementSystem.Models
{
    // --- Meter Readings ---
    public class CreateMeterReadingRequest
    {
        public int RoomId { get; set; }
        public int ServiceId { get; set; }
        public int BillingMonth { get; set; }
        public int BillingYear { get; set; }
        public double ReadingValue { get; set; }
        public string? Note { get; set; }
    }

    // --- Invoices ---
    public class GenerateInvoiceRequest
    {
        public int RoomId { get; set; }
        public int BillingMonth { get; set; }
        public int BillingYear { get; set; }
        public DateTime DueDate { get; set; }
    }

    public class InvoiceResponse
    {
        public int InvoiceId { get; set; }
        public int RoomId { get; set; }
        public string RoomCode { get; set; } = string.Empty;
        public int BillingMonth { get; set; }
        public int BillingYear { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount => TotalAmount - PaidAmount;
        public string Status { get; set; } = "Unpaid";
        public DateTime DueDate { get; set; }
        public List<InvoiceDetailResponse> Details { get; set; } = new();
    }

    public class InvoiceDetailResponse
    {
        public string ServiceName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
    }

    // --- Payments ---
    public class CreatePaymentRequest
    {
        public int InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Transfer
        public string? Note { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
    }

    // --- Requests ---
    public class CreateTenantRequest
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
}
