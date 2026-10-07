using System;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.BillingDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Services.BillingServices;

public class PaymentService : IPaymentService
{
    private readonly GymDbContext _context;

    public PaymentService(GymDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<PaymentDto>> GetPaymentsAsync(PaymentQueryParameters query)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        var paymentsQuery = _context.Payments
            .AsNoTracking()
            .Include(p => p.Invoice)
            .Include(p => p.Member)
            .Include(p => p.Branch)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            paymentsQuery = paymentsQuery.Where(p =>
                p.PaymentCode.ToLower().Contains(keyword) ||
                (p.Member != null && p.Member.FullName.ToLower().Contains(keyword)) ||
                (p.Reference != null && p.Reference.ToLower().Contains(keyword)));
        }

        if (query.InvoiceId.HasValue) paymentsQuery = paymentsQuery.Where(p => p.InvoiceId == query.InvoiceId);
        if (query.MemberId.HasValue) paymentsQuery = paymentsQuery.Where(p => p.MemberId == query.MemberId);
        if (query.BranchId.HasValue) paymentsQuery = paymentsQuery.Where(p => p.BranchId == query.BranchId);
        if (!string.IsNullOrWhiteSpace(query.PaymentMethod)) paymentsQuery = paymentsQuery.Where(p => p.PaymentMethod == query.PaymentMethod);
        if (query.FromDate.HasValue) paymentsQuery = paymentsQuery.Where(p => p.PaymentDate >= query.FromDate);
        if (query.ToDate.HasValue) paymentsQuery = paymentsQuery.Where(p => p.PaymentDate <= query.ToDate);

        var totalCount = await paymentsQuery.CountAsync();

        var payments = await paymentsQuery
            .OrderByDescending(p => p.PaymentDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = payments.Select(MapToDto).ToList();

        return PaginatedResponse<PaymentDto>.Create(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PaymentDto> GetPaymentByIdAsync(Guid paymentId)
    {
        var payment = await _context.Payments
            .AsNoTracking()
            .Include(p => p.Invoice)
            .Include(p => p.Member)
            .Include(p => p.Branch)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu thu.");

        return MapToDto(payment);
    }

    public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentRequest request, Guid? currentUserId)
    {
        if (request.Amount <= 0)
            throw new BusinessException("Số tiền thu phải lớn hơn 0.");

        var validMethods = new[]
        {
            DomainConstants.PaymentMethod.Cash,
            DomainConstants.PaymentMethod.Transfer,
            DomainConstants.PaymentMethod.Card,
            DomainConstants.PaymentMethod.EWallet
        };

        if (!validMethods.Contains(request.PaymentMethod))
            throw new BusinessException($"Phương thức thanh toán không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validMethods)}.");

        Invoice? invoice = null;

        if (request.InvoiceId.HasValue)
        {
            invoice = await _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.InvoiceId == request.InvoiceId)
                ?? throw new KeyNotFoundException("Không tìm thấy hóa đơn.");

            if (invoice.Status == DomainConstants.InvoiceStatus.Cancelled)
                throw new BusinessException("Hóa đơn đã bị hủy, không thể thu tiền.");

            var remaining = invoice.TotalAmount - invoice.PaidAmount;
            if (remaining <= 0)
                throw new BusinessException("Hóa đơn này đã được thanh toán đầy đủ.");

            if (request.Amount > remaining)
                throw new BusinessException($"Số tiền thu vượt quá số tiền còn lại của hóa đơn ({remaining:N0} đ).");
        }

        var now = DateTime.UtcNow;
        var sequence = await _context.Payments.CountAsync() + 1;

        var payment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            PaymentCode = CodeGenerator.PaymentCode(now, sequence),
            InvoiceId = request.InvoiceId,
            MemberId = request.MemberId ?? invoice?.MemberId,
            BranchId = request.BranchId ?? invoice?.BranchId,
            PaymentMethod = request.PaymentMethod,
            Amount = request.Amount,
            PaymentDate = request.PaymentDate ?? now,
            Reference = request.Reference,
            Status = DomainConstants.PaymentStatus.Success,
            Notes = request.Notes,
            CreatedBy = currentUserId,
            CreatedAt = now
        };

        _context.Payments.Add(payment);

        if (invoice != null)
        {
            invoice.PaidAmount += request.Amount;
            invoice.Status = invoice.PaidAmount >= invoice.TotalAmount
                ? DomainConstants.InvoiceStatus.Paid
                : DomainConstants.InvoiceStatus.Partial;
            invoice.UpdatedAt = now;

            if (invoice.SubscriptionId.HasValue)
            {
                var subscription = await _context.MemberSubscriptions
                    .FirstOrDefaultAsync(s => s.SubscriptionId == invoice.SubscriptionId);

                if (subscription != null)
                {
                    subscription.PaidAmount = Math.Min(subscription.FinalAmount, subscription.PaidAmount + request.Amount);
                    subscription.PaymentStatus = subscription.PaidAmount >= subscription.FinalAmount
                        ? DomainConstants.PaymentStatus.Paid
                        : subscription.PaidAmount > 0
                            ? DomainConstants.PaymentStatus.Partial
                            : DomainConstants.PaymentStatus.Unpaid;
                    subscription.UpdatedAt = now;
                }
            }
        }

        await _context.SaveChangesAsync();

        if (payment.InvoiceId.HasValue)
            await _context.Entry(payment).Reference(p => p.Invoice).LoadAsync();
        if (payment.MemberId.HasValue)
            await _context.Entry(payment).Reference(p => p.Member).LoadAsync();
        if (payment.BranchId.HasValue)
            await _context.Entry(payment).Reference(p => p.Branch).LoadAsync();

        return MapToDto(payment);
    }

    public async Task<PaymentDto> RefundPaymentAsync(Guid paymentId, string? reason)
    {
        var payment = await _context.Payments
            .Include(p => p.Invoice)
            .Include(p => p.Member)
            .Include(p => p.Branch)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu thu.");

        if (payment.Status == DomainConstants.PaymentStatus.Refunded)
            throw new BusinessException("Phiếu thu này đã được hoàn tiền trước đó.");

        payment.Status = DomainConstants.PaymentStatus.Refunded;
        payment.Notes = string.IsNullOrWhiteSpace(reason) ? payment.Notes : $"{payment.Notes}\n[Hoàn tiền] {reason}".Trim();

        if (payment.Invoice != null)
        {
            payment.Invoice.PaidAmount = Math.Max(0, payment.Invoice.PaidAmount - payment.Amount);
            payment.Invoice.Status = payment.Invoice.PaidAmount <= 0
                ? DomainConstants.InvoiceStatus.Unpaid
                : payment.Invoice.PaidAmount >= payment.Invoice.TotalAmount
                    ? DomainConstants.InvoiceStatus.Paid
                    : DomainConstants.InvoiceStatus.Partial;
            payment.Invoice.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return MapToDto(payment);
    }

    public async Task DeletePaymentAsync(Guid paymentId)
    {
        var payment = await _context.Payments
            .Include(p => p.Invoice)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu thu.");

        if (payment.Invoice != null)
        {
            payment.Invoice.PaidAmount = Math.Max(0, payment.Invoice.PaidAmount - payment.Amount);
            payment.Invoice.Status = payment.Invoice.PaidAmount <= 0
                ? DomainConstants.InvoiceStatus.Unpaid
                : payment.Invoice.PaidAmount >= payment.Invoice.TotalAmount
                    ? DomainConstants.InvoiceStatus.Paid
                    : DomainConstants.InvoiceStatus.Partial;
            payment.Invoice.UpdatedAt = DateTime.UtcNow;
        }

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();
    }

    private static PaymentDto MapToDto(Payment payment) => new()
    {
        PaymentId = payment.PaymentId,
        PaymentCode = payment.PaymentCode,
        InvoiceId = payment.InvoiceId,
        InvoiceCode = payment.Invoice?.InvoiceCode,
        MemberId = payment.MemberId,
        MemberName = payment.Member?.FullName,
        BranchId = payment.BranchId,
        BranchName = payment.Branch?.BranchName,
        PaymentMethod = payment.PaymentMethod,
        Amount = payment.Amount,
        PaymentDate = payment.PaymentDate,
        Reference = payment.Reference,
        Status = payment.Status,
        Notes = payment.Notes,
        CreatedAt = payment.CreatedAt
    };
}
