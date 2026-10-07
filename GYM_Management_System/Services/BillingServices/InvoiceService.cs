using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.DTOs;
using GYM_Management_System.DTOs.BillingDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Models;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Services.BillingServices;

public class InvoiceService : IInvoiceService
{
    private readonly GymDbContext _context;

    public InvoiceService(GymDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResponse<InvoiceDto>> GetInvoicesAsync(InvoiceQueryParameters query)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        var invoicesQuery = _context.Invoices
            .AsNoTracking()
            .Include(i => i.Member)
            .Include(i => i.Branch)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var keyword = query.Search.Trim().ToLower();
            invoicesQuery = invoicesQuery.Where(i =>
                i.InvoiceCode.ToLower().Contains(keyword) ||
                (i.Member != null && i.Member.FullName.ToLower().Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
            invoicesQuery = invoicesQuery.Where(i => i.Status == query.Status);

        if (query.MemberId.HasValue) invoicesQuery = invoicesQuery.Where(i => i.MemberId == query.MemberId);
        if (query.BranchId.HasValue) invoicesQuery = invoicesQuery.Where(i => i.BranchId == query.BranchId);
        if (query.FromDate.HasValue) invoicesQuery = invoicesQuery.Where(i => i.InvoiceDate >= query.FromDate);
        if (query.ToDate.HasValue) invoicesQuery = invoicesQuery.Where(i => i.InvoiceDate <= query.ToDate);

        var totalCount = await invoicesQuery.CountAsync();

        var invoices = await invoicesQuery
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(i => i.Items)
            .ToListAsync();

        var items = invoices.Select(MapToDto).ToList();

        return PaginatedResponse<InvoiceDto>.Create(items, totalCount, pageNumber, pageSize);
    }

    public async Task<InvoiceDto> GetInvoiceByIdAsync(Guid invoiceId)
    {
        var invoice = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Member)
            .Include(i => i.Branch)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId)
            ?? throw new KeyNotFoundException("Không tìm thấy hóa đơn.");

        return MapToDto(invoice);
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request, Guid? currentUserId)
    {
        if (request.Items == null || request.Items.Count == 0)
            throw new BusinessException("Hóa đơn phải có ít nhất 1 dòng chi tiết.");

        if (request.MemberId.HasValue && !await _context.Members.AnyAsync(m => m.MemberId == request.MemberId))
            throw new KeyNotFoundException("Không tìm thấy hội viên.");

        var subTotal = request.Items.Sum(i => i.Quantity * i.UnitPrice);

        if (request.DiscountAmount < 0 || request.DiscountAmount > subTotal)
            throw new BusinessException("Số tiền giảm giá không hợp lệ.");

        var now = DateTime.UtcNow;
        var sequence = await _context.Invoices.CountAsync() + 1;

        var invoice = new Invoice
        {
            InvoiceId = Guid.NewGuid(),
            InvoiceCode = CodeGenerator.InvoiceCode(now, sequence),
            MemberId = request.MemberId,
            BranchId = request.BranchId,
            SubscriptionId = request.SubscriptionId,
            InvoiceDate = request.InvoiceDate ?? now,
            SubTotal = subTotal,
            DiscountAmount = request.DiscountAmount,
            TotalAmount = subTotal - request.DiscountAmount,
            PaidAmount = 0,
            Status = DomainConstants.InvoiceStatus.Unpaid,
            Notes = request.Notes,
            CreatedBy = currentUserId,
            CreatedAt = now,
            UpdatedAt = now,
            Items = request.Items.Select(item => new InvoiceItem
            {
                InvoiceItemId = Guid.NewGuid(),
                ItemType = item.ItemType,
                ReferenceId = item.ReferenceId,
                ItemName = item.ItemName.Trim(),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Amount = item.Quantity * item.UnitPrice,
                Notes = item.Notes
            }).ToList()
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        if (invoice.MemberId.HasValue)
            await _context.Entry(invoice).Reference(i => i.Member).LoadAsync();
        if (invoice.BranchId.HasValue)
            await _context.Entry(invoice).Reference(i => i.Branch).LoadAsync();

        return MapToDto(invoice);
    }

    public async Task<InvoiceDto> CancelInvoiceAsync(Guid invoiceId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Member)
            .Include(i => i.Branch)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId)
            ?? throw new KeyNotFoundException("Không tìm thấy hóa đơn.");

        if (invoice.Status == DomainConstants.InvoiceStatus.Cancelled)
            throw new BusinessException("Hóa đơn này đã được hủy trước đó.");

        if (invoice.PaidAmount > 0)
            throw new BusinessException("Hóa đơn đã phát sinh thanh toán nên không thể hủy. Vui lòng hoàn tiền trước.");

        invoice.Status = DomainConstants.InvoiceStatus.Cancelled;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(invoice);
    }

    public async Task<RevenueSummaryDto> GetRevenueSummaryAsync(DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate ?? VietnamTime.MonthStartUtc;
        var to = toDate ?? DateTime.UtcNow;

        var invoiceStats = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to && i.Status != DomainConstants.InvoiceStatus.Cancelled)
            .Select(i => new { i.Status, i.TotalAmount, i.PaidAmount, i.Items })
            .ToListAsync();

        var payments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.PaymentDate >= from && p.PaymentDate <= to && p.Status == DomainConstants.PaymentStatus.Success)
            .ToListAsync();

        var subscriptionInvoiceIds = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to && i.SubscriptionId != null)
            .Select(i => i.InvoiceId)
            .ToListAsync();

        var subscriptionRevenue = payments
            .Where(p => p.InvoiceId.HasValue && subscriptionInvoiceIds.Contains(p.InvoiceId.Value))
            .Sum(p => p.Amount);

        var paymentItemBreakdown = await _context.Payments
            .AsNoTracking()
            .Where(p => p.PaymentDate >= from && p.PaymentDate <= to
                        && p.Status == DomainConstants.PaymentStatus.Success
                        && p.InvoiceId != null
                        && !subscriptionInvoiceIds.Contains(p.InvoiceId.Value))
            .SelectMany(p => p.Invoice!.Items)
            .GroupBy(item => item.ItemType)
            .Select(g => new { ItemType = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync();

        var classRevenue = paymentItemBreakdown
            .Where(x => x.ItemType is DomainConstants.InvoiceItemType.Class or DomainConstants.InvoiceItemType.PersonalTraining)
            .Sum(x => x.Amount);

        var totalRevenue = payments.Sum(p => p.Amount);
        var otherRevenue = totalRevenue - subscriptionRevenue - classRevenue;

        return new RevenueSummaryDto
        {
            FromDate = from,
            ToDate = to,
            TotalRevenue = totalRevenue,
            SubscriptionRevenue = subscriptionRevenue,
            ClassRevenue = classRevenue,
            OtherRevenue = otherRevenue < 0 ? 0 : otherRevenue,
            PaidInvoiceCount = invoiceStats.Count(i => i.Status == DomainConstants.InvoiceStatus.Paid),
            UnpaidInvoiceCount = invoiceStats.Count(i => i.Status != DomainConstants.InvoiceStatus.Paid),
            OutstandingAmount = invoiceStats
                .Where(i => i.Status != DomainConstants.InvoiceStatus.Paid)
                .Sum(i => i.TotalAmount - i.PaidAmount)
        };
    }

    private static InvoiceDto MapToDto(Invoice invoice) => new()
    {
        InvoiceId = invoice.InvoiceId,
        InvoiceCode = invoice.InvoiceCode,
        MemberId = invoice.MemberId,
        MemberName = invoice.Member?.FullName,
        MemberCode = invoice.Member?.MemberCode,
        BranchId = invoice.BranchId,
        BranchName = invoice.Branch?.BranchName,
        SubscriptionId = invoice.SubscriptionId,
        InvoiceDate = invoice.InvoiceDate,
        SubTotal = invoice.SubTotal,
        DiscountAmount = invoice.DiscountAmount,
        TotalAmount = invoice.TotalAmount,
        PaidAmount = invoice.PaidAmount,
        RemainingAmount = invoice.TotalAmount - invoice.PaidAmount,
        Status = invoice.Status,
        Notes = invoice.Notes,
        CreatedAt = invoice.CreatedAt,
        Items = invoice.Items.Select(item => new InvoiceItemDto
        {
            InvoiceItemId = item.InvoiceItemId,
            ItemType = item.ItemType,
            ReferenceId = item.ReferenceId,
            ItemName = item.ItemName,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            Amount = item.Amount,
            Notes = item.Notes
        }).ToList(),
        Payments = invoice.Payments.Select(payment => new PaymentDto
        {
            PaymentId = payment.PaymentId,
            PaymentCode = payment.PaymentCode,
            InvoiceId = payment.InvoiceId,
            MemberId = payment.MemberId,
            PaymentMethod = payment.PaymentMethod,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate,
            Reference = payment.Reference,
            Status = payment.Status,
            Notes = payment.Notes,
            CreatedAt = payment.CreatedAt
        }).ToList()
    };
}
