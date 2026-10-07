using System;
using System.Linq;
using System.Threading.Tasks;
using GYM_Management_System.DTOs.MemberDTOs;
using GYM_Management_System.Exceptions;
using GYM_Management_System.Models;
using GYM_Management_System.Services.MemberServices;

namespace GYM_Management_System.UnitTests.Services.MemberServices;

public class MemberServiceTests
{
    private static CreateMemberRequest ValidRequest(string phone = "0901234567") => new()
    {
        FullName = "  Nguyễn Văn Test  ",
        Phone = phone,
        Email = $"{phone}@gym.local",
        Gender = DomainConstants.Gender.Male
    };

    [Fact]
    public async Task CreateMemberAsync_ShouldGenerateSequentialMemberCode_AndTrimName()
    {
        using var context = TestDbFactory.Create();
        var service = new MemberService(context);

        var first = await service.CreateMemberAsync(ValidRequest("0900000001"));
        var second = await service.CreateMemberAsync(ValidRequest("0900000002"));

        Assert.Equal("MEM00001", first.MemberCode);
        Assert.Equal("MEM00002", second.MemberCode);
        Assert.Equal("Nguyễn Văn Test", first.FullName);
        Assert.Equal(DomainConstants.MemberStatus.Active, first.Status);
    }

    [Fact]
    public async Task CreateMemberAsync_WithDuplicatePhone_ShouldThrowBusinessException()
    {
        using var context = TestDbFactory.Create();
        var service = new MemberService(context);

        await service.CreateMemberAsync(ValidRequest("0900000003"));

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.CreateMemberAsync(ValidRequest("0900000003")));

        Assert.Contains("Số điện thoại", exception.Message);
    }

    [Fact]
    public async Task GetMembersAsync_ShouldFilterBySearchKeyword()
    {
        using var context = TestDbFactory.Create();
        var service = new MemberService(context);

        await service.CreateMemberAsync(new CreateMemberRequest { FullName = "Trần Thị Hoa", Phone = "0900000010" });
        await service.CreateMemberAsync(new CreateMemberRequest { FullName = "Lê Văn Nam", Phone = "0900000011" });

        var result = await service.GetMembersAsync(new MemberQueryParameters { Search = "hoa" });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Trần Thị Hoa", result.Items.Single().FullName);
    }

    [Fact]
    public async Task ChangeStatusAsync_WithInvalidStatus_ShouldThrowBusinessException()
    {
        using var context = TestDbFactory.Create();
        var service = new MemberService(context);

        var member = await service.CreateMemberAsync(ValidRequest("0900000020"));

        await Assert.ThrowsAsync<BusinessException>(
            () => service.ChangeStatusAsync(member.MemberId, new ChangeMemberStatusRequest { Status = "KHONG_HOP_LE" }));
    }

    [Fact]
    public async Task DeleteMemberAsync_WhenMemberHasSubscription_ShouldThrowBusinessException()
    {
        using var context = TestDbFactory.Create();
        var service = new MemberService(context);

        var member = await service.CreateMemberAsync(ValidRequest("0900000030"));

        context.MemberSubscriptions.Add(new MemberSubscription
        {
            SubscriptionId = Guid.NewGuid(),
            SubscriptionCode = "SUB-TEST",
            MemberId = member.MemberId,
            PlanId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = DomainConstants.SubscriptionStatus.Active
        });
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<BusinessException>(() => service.DeleteMemberAsync(member.MemberId));
    }

    [Fact]
    public async Task DeleteMemberAsync_WithoutHistory_ShouldRemoveMember()
    {
        using var context = TestDbFactory.Create();
        var service = new MemberService(context);

        var member = await service.CreateMemberAsync(ValidRequest("0900000040"));

        await service.DeleteMemberAsync(member.MemberId);

        Assert.False(context.Members.Any(m => m.MemberId == member.MemberId));
    }
}
