using System;
using System.Threading.Tasks;
using GYM_Management_System.DTOs.AttendanceDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Models;
using GYM_Management_System.Services.AttendanceServices;
using GYM_Management_System.UnitTests.Fakes;

namespace GYM_Management_System.UnitTests.Services.AttendanceServices;

public class CheckInServiceTests
{
    private static (GymDbContext Context, Member Member, MemberSubscription? Subscription) SeedMember(bool withActiveSubscription)
    {
        var context = TestDbFactory.Create();

        var member = new Member
        {
            MemberId = Guid.NewGuid(),
            MemberCode = "MEM00001",
            FullName = "Hội viên Check-in",
            Phone = "0909999999",
            JoinDate = DateTime.UtcNow,
            Status = DomainConstants.MemberStatus.Active
        };

        context.Members.Add(member);

        MemberSubscription? subscription = null;

        if (withActiveSubscription)
        {
            subscription = new MemberSubscription
            {
                SubscriptionId = Guid.NewGuid(),
                SubscriptionCode = "SUB20260101-0001",
                MemberId = member.MemberId,
                PlanId = Guid.NewGuid(),
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(29),
                Status = DomainConstants.SubscriptionStatus.Active
            };

            context.MemberSubscriptions.Add(subscription);
        }

        context.SaveChanges();

        return (context, member, subscription);
    }

    [Fact]
    public async Task CheckInAsync_WithActiveSubscription_ShouldCreateCheckIn()
    {
        var (context, member, _) = SeedMember(withActiveSubscription: true);
        using var _db = context;

        var sender = new FakeNotificationSender();
        var service = new CheckInService(context, sender);

        var result = await service.CheckInAsync(new CreateCheckInRequest
        {
            MemberCode = "MEM00001",
            Method = DomainConstants.CheckInMethod.Qr
        }, null);

        Assert.Equal(member.MemberId, result.MemberId);
        Assert.Equal("Hội viên Check-in", result.MemberName);
        Assert.Null(result.CheckOutTime);
        Assert.Single(context.CheckIns);
    }

    [Fact]
    public async Task CheckInAsync_WithoutActiveSubscription_ShouldThrow()
    {
        var (context, _, _) = SeedMember(withActiveSubscription: false);
        using var _db = context;

        var service = new CheckInService(context, new FakeNotificationSender());

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.CheckInAsync(new CreateCheckInRequest { MemberCode = "MEM00001" }, null));

        Assert.Contains("gói tập", exception.Message);
    }

    [Fact]
    public async Task CheckInAsync_WhenMemberAlreadyInside_ShouldThrow()
    {
        var (context, _, _) = SeedMember(withActiveSubscription: true);
        using var _db = context;

        var service = new CheckInService(context, new FakeNotificationSender());

        await service.CheckInAsync(new CreateCheckInRequest { MemberCode = "MEM00001" }, null);

        await Assert.ThrowsAsync<BusinessException>(
            () => service.CheckInAsync(new CreateCheckInRequest { MemberCode = "MEM00001" }, null));
    }

    [Fact]
    public async Task CheckInAsync_WithUnknownMemberCode_ShouldThrowKeyNotFound()
    {
        var (context, _, _) = SeedMember(withActiveSubscription: true);
        using var _db = context;

        var service = new CheckInService(context, new FakeNotificationSender());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CheckInAsync(new CreateCheckInRequest { MemberCode = "KHONG-TON-TAI" }, null));
    }

    [Fact]
    public async Task CheckOutAsync_ShouldSetCheckOutTime()
    {
        var (context, _, _) = SeedMember(withActiveSubscription: true);
        using var _db = context;

        var service = new CheckInService(context, new FakeNotificationSender());

        var checkIn = await service.CheckInAsync(new CreateCheckInRequest { MemberCode = "MEM00001" }, null);
        var checkedOut = await service.CheckOutAsync(new CheckOutRequest { CheckInId = checkIn.CheckInId });

        Assert.NotNull(checkedOut.CheckOutTime);
        Assert.NotNull(checkedOut.DurationMinutes);

        await Assert.ThrowsAsync<BusinessException>(
            () => service.CheckOutAsync(new CheckOutRequest { CheckInId = checkIn.CheckInId }));
    }

    [Fact]
    public async Task GetStatsAsync_ShouldCountTodayCheckIns()
    {
        var (context, _, _) = SeedMember(withActiveSubscription: true);
        using var _db = context;

        var service = new CheckInService(context, new FakeNotificationSender());
        await service.CheckInAsync(new CreateCheckInRequest { MemberCode = "MEM00001" }, null);

        var stats = await service.GetStatsAsync(null, null);

        Assert.Equal(1, stats.TotalCheckIns);
        Assert.Equal(1, stats.CurrentlyInside);
        Assert.Equal(1, stats.UniqueMembers);
    }
}
