using System;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.DTOs.MembershipDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Models;
using GYM_Management_System.Services.BillingServices;
using GYM_Management_System.Services.MembershipServices;

namespace GYM_Management_System.UnitTests.Services.MembershipServices;

public class SubscriptionServiceTests
{
    private static async Task<(GymDbContext Context, Member Member, MembershipPlan Plan)> SeedMemberAndPlanAsync(int durationDays = 30)
    {
        var context = TestDbFactory.Create();

        var member = new Member
        {
            MemberId = Guid.NewGuid(),
            MemberCode = "MEM00001",
            FullName = "Hội viên Test",
            Phone = "0901234567",
            JoinDate = DateTime.UtcNow,
            Status = DomainConstants.MemberStatus.Active
        };

        var plan = new MembershipPlan
        {
            PlanId = Guid.NewGuid(),
            PlanCode = "PLAN001",
            PlanName = "Gói 1 tháng",
            DurationDays = durationDays,
            Price = 600000,
            MaxFreezeDays = 5,
            IsActive = true
        };

        context.Members.Add(member);
        context.MembershipPlans.Add(plan);
        await context.SaveChangesAsync();

        return (context, member, plan);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_ShouldComputeEndDate_AndCreateInvoice()
    {
        var (context, member, plan) = await SeedMemberAndPlanAsync();
        using var _ = context;

        var service = new SubscriptionService(context, new InvoiceService(context));

        var result = await service.CreateSubscriptionAsync(new CreateSubscriptionRequest
        {
            MemberId = member.MemberId,
            PlanId = plan.PlanId,
            StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DiscountAmount = 100000,
            CreateInvoice = true
        }, null);

        Assert.Equal(new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc), result.EndDate);
        Assert.Equal(500000, result.FinalAmount);
        Assert.Equal(DomainConstants.SubscriptionStatus.Active, result.Status);
        Assert.Equal(DomainConstants.PaymentStatus.Unpaid, result.PaymentStatus);

        var invoice = context.Invoices.Single();
        Assert.Equal(plan.PlanId, invoice.Items.Single().ReferenceId);
        Assert.Equal(600000, invoice.SubTotal);
        Assert.Equal(500000, invoice.TotalAmount);
        Assert.Equal(DomainConstants.InvoiceStatus.Unpaid, invoice.Status);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_WhenMemberAlreadyHasActiveSubscription_ShouldThrow()
    {
        var (context, member, plan) = await SeedMemberAndPlanAsync();
        using var _ = context;

        var service = new SubscriptionService(context, new InvoiceService(context));

        var request = new CreateSubscriptionRequest { MemberId = member.MemberId, PlanId = plan.PlanId };
        await service.CreateSubscriptionAsync(request, null);

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.CreateSubscriptionAsync(request, null));

        Assert.Contains("gia hạn", exception.Message);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_WhenPlanIsInactive_ShouldThrow()
    {
        var (context, member, plan) = await SeedMemberAndPlanAsync();
        using var _ = context;

        plan.IsActive = false;
        await context.SaveChangesAsync();

        var service = new SubscriptionService(context, new InvoiceService(context));

        await Assert.ThrowsAsync<BusinessException>(() => service.CreateSubscriptionAsync(
            new CreateSubscriptionRequest { MemberId = member.MemberId, PlanId = plan.PlanId }, null));
    }

    [Fact]
    public async Task FreezeSubscriptionAsync_ShouldExtendEndDate_AndSetFrozenStatus()
    {
        var (context, member, plan) = await SeedMemberAndPlanAsync();
        using var _ = context;

        var service = new SubscriptionService(context, new InvoiceService(context));

        var subscription = await service.CreateSubscriptionAsync(new CreateSubscriptionRequest
        {
            MemberId = member.MemberId,
            PlanId = plan.PlanId,
            StartDate = DateTime.UtcNow.Date,
            CreateInvoice = false
        }, null);

        var frozen = await service.FreezeSubscriptionAsync(subscription.SubscriptionId, new FreezeSubscriptionRequest { Days = 5 });

        Assert.Equal(DomainConstants.SubscriptionStatus.Frozen, frozen.Status);
        Assert.Equal(subscription.EndDate.AddDays(5), frozen.EndDate);

        var unfrozen = await service.UnfreezeSubscriptionAsync(subscription.SubscriptionId);
        Assert.Equal(DomainConstants.SubscriptionStatus.Active, unfrozen.Status);
    }

    [Fact]
    public async Task FreezeSubscriptionAsync_WhenDaysExceedPlanLimit_ShouldThrow()
    {
        var (context, member, plan) = await SeedMemberAndPlanAsync();
        using var _ = context;

        var service = new SubscriptionService(context, new InvoiceService(context));

        var subscription = await service.CreateSubscriptionAsync(new CreateSubscriptionRequest
        {
            MemberId = member.MemberId,
            PlanId = plan.PlanId,
            CreateInvoice = false
        }, null);

        await Assert.ThrowsAsync<BusinessException>(
            () => service.FreezeSubscriptionAsync(subscription.SubscriptionId, new FreezeSubscriptionRequest { Days = 99 }));
    }
}
