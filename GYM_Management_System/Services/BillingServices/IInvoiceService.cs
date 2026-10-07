using System;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.BillingDTOs;

namespace GYM_Management_System.Services.BillingServices;

public interface IInvoiceService
{
    Task<PaginatedResponse<InvoiceDto>> GetInvoicesAsync(InvoiceQueryParameters query);

    Task<InvoiceDto> GetInvoiceByIdAsync(Guid invoiceId);

    Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request, Guid? currentUserId);

    Task<InvoiceDto> CancelInvoiceAsync(Guid invoiceId);

    Task<RevenueSummaryDto> GetRevenueSummaryAsync(DateTime? fromDate, DateTime? toDate);
}
