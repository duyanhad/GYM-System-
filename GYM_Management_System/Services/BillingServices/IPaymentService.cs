using System;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.BillingDTOs;

namespace GYM_Management_System.Services.BillingServices;

public interface IPaymentService
{
    Task<PaginatedResponse<PaymentDto>> GetPaymentsAsync(PaymentQueryParameters query);

    Task<PaymentDto> GetPaymentByIdAsync(Guid paymentId);

    Task<PaymentDto> CreatePaymentAsync(CreatePaymentRequest request, Guid? currentUserId);

    Task<PaymentDto> RefundPaymentAsync(Guid paymentId, string? reason);

    Task DeletePaymentAsync(Guid paymentId);
}
