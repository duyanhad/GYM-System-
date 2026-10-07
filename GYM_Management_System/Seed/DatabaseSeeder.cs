using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GYM_Management_System.Authorization;
using GYM_Management_System.Models;
using GYM_Management_System.Services;
using Microsoft.EntityFrameworkCore;

namespace GYM_Management_System.Seed;

/// <summary>
/// Tạo dữ liệu khởi tạo: quyền, role, tài khoản quản trị, chi nhánh, gói tập,
/// huấn luyện viên, lớp học và một ít hội viên mẫu.
/// Chạy mỗi lần khởi động nhưng chỉ thêm dữ liệu còn thiếu (idempotent).
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(GymDbContext context)
    {
        await SeedPermissionsAsync(context);
        await SeedRolesAsync(context);
        await SeedUsersAsync(context);
        await SeedBranchesAsync(context);
        await SeedMembershipPlansAsync(context);
        await SeedTrainersAsync(context);
        await SeedTrainingClassesAsync(context);
        await SeedMembersAsync(context);
        await SeedWorkoutLibraryAsync(context);
    }

    // =====================================================================
    // QUYỀN (PERMISSION)
    // =====================================================================
    private static async Task SeedPermissionsAsync(GymDbContext context)
    {
        var codes = GetAllPermissionCodes().ToList();
        var existingCodes = await context.Permissions.Select(p => p.PermissionCode).ToListAsync();

        var newPermissions = codes
            .Where(code => !existingCodes.Contains(code))
            .Select(code => new Permission
            {
                PermissionId = Guid.NewGuid(),
                PermissionCode = code,
                PermissionName = GetPermissionName(code),
                GroupName = code.Split('_')[0]
            })
            .ToList();

        if (newPermissions.Count == 0) return;

        context.Permissions.AddRange(newPermissions);
        await context.SaveChangesAsync();
    }

    /// <summary>Lấy toàn bộ mã quyền khai báo trong PermissionConstants (nguồn duy nhất).</summary>
    private static IEnumerable<string> GetAllPermissionCodes()
    {
        var fields = typeof(PermissionConstants)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        foreach (var field in fields)
        {
            if (field.IsLiteral && field.FieldType == typeof(string))
            {
                var value = (string)field.GetRawConstantValue()!;
                if (value == PermissionConstants.PERMISSION_CLAIM_TYPE) continue;

                yield return value;
            }
        }
    }

    private static string GetPermissionName(string code) => code switch
    {
        PermissionConstants.MEMBER_VIEW => "Xem danh sách hội viên",
        PermissionConstants.MEMBER_CREATE => "Thêm hội viên",
        PermissionConstants.MEMBER_UPDATE => "Cập nhật hội viên",
        PermissionConstants.MEMBER_DELETE => "Xóa hội viên",
        PermissionConstants.MEMBERSHIP_PLAN_VIEW => "Xem gói tập",
        PermissionConstants.MEMBERSHIP_PLAN_CREATE => "Tạo gói tập",
        PermissionConstants.MEMBERSHIP_PLAN_UPDATE => "Cập nhật gói tập",
        PermissionConstants.MEMBERSHIP_PLAN_DELETE => "Xóa gói tập",
        PermissionConstants.SUBSCRIPTION_VIEW => "Xem đăng ký gói tập",
        PermissionConstants.SUBSCRIPTION_CREATE => "Đăng ký gói tập cho hội viên",
        PermissionConstants.SUBSCRIPTION_UPDATE => "Gia hạn gói tập",
        PermissionConstants.SUBSCRIPTION_FREEZE => "Bảo lưu / mở bảo lưu gói tập",
        PermissionConstants.SUBSCRIPTION_CANCEL => "Hủy gói tập",
        PermissionConstants.TRAINER_VIEW => "Xem huấn luyện viên",
        PermissionConstants.TRAINER_CREATE => "Thêm huấn luyện viên",
        PermissionConstants.TRAINER_UPDATE => "Cập nhật huấn luyện viên",
        PermissionConstants.TRAINER_DELETE => "Xóa huấn luyện viên",
        PermissionConstants.CLASS_VIEW => "Xem lớp học",
        PermissionConstants.CLASS_CREATE => "Tạo lớp học",
        PermissionConstants.CLASS_UPDATE => "Cập nhật lớp học",
        PermissionConstants.CLASS_DELETE => "Xóa lớp học",
        PermissionConstants.CLASS_SCHEDULE_MANAGE => "Quản lý lịch học",
        PermissionConstants.BOOKING_VIEW => "Xem đăng ký lớp học",
        PermissionConstants.BOOKING_CREATE => "Đăng ký lớp học cho hội viên",
        PermissionConstants.BOOKING_UPDATE => "Cập nhật đăng ký lớp học",
        PermissionConstants.BOOKING_CANCEL => "Hủy đăng ký lớp học",
        PermissionConstants.CHECKIN_VIEW => "Xem lịch sử ra vào",
        PermissionConstants.CHECKIN_CREATE => "Check-in hội viên",
        PermissionConstants.CHECKIN_UPDATE => "Check-out hội viên",
        PermissionConstants.INVOICE_VIEW => "Xem hóa đơn",
        PermissionConstants.INVOICE_CREATE => "Tạo hóa đơn",
        PermissionConstants.INVOICE_UPDATE => "Cập nhật hóa đơn",
        PermissionConstants.INVOICE_CANCEL => "Hủy hóa đơn",
        PermissionConstants.PAYMENT_VIEW => "Xem phiếu thu",
        PermissionConstants.PAYMENT_CREATE => "Thu tiền",
        PermissionConstants.PAYMENT_UPDATE => "Hoàn tiền",
        PermissionConstants.PAYMENT_DELETE => "Xóa phiếu thu",
        PermissionConstants.BRANCH_VIEW => "Xem chi nhánh",
        PermissionConstants.BRANCH_CREATE => "Tạo chi nhánh",
        PermissionConstants.BRANCH_UPDATE => "Cập nhật chi nhánh",
        PermissionConstants.BRANCH_DELETE => "Xóa chi nhánh",
        PermissionConstants.DASHBOARD_VIEW => "Xem dashboard",
        PermissionConstants.REPORT_VIEW => "Xem báo cáo doanh thu",
        PermissionConstants.ADMIN_USER_VIEW => "Xem tài khoản hệ thống",
        PermissionConstants.ADMIN_USER_CREATE => "Tạo tài khoản hệ thống",
        PermissionConstants.ADMIN_USER_UPDATE => "Cập nhật tài khoản hệ thống",
        PermissionConstants.ADMIN_USER_DELETE => "Xóa tài khoản hệ thống",
        PermissionConstants.ADMIN_ROLE_VIEW => "Xem role & quyền",
        PermissionConstants.ADMIN_ROLE_CREATE => "Tạo role",
        PermissionConstants.ADMIN_ROLE_UPDATE => "Cập nhật role",
        PermissionConstants.ADMIN_ROLE_DELETE => "Xóa role",
        PermissionConstants.ADMIN_PERMISSION_ASSIGN => "Gán quyền riêng cho tài khoản",
        PermissionConstants.SYSTEM_MANAGE => "Quản trị hệ thống",
        PermissionConstants.SYSTEM_LOG_VIEW => "Xem nhật ký hệ thống",
        PermissionConstants.SYSTEM_NOTIFICATION_VIEW => "Xem thông báo hệ thống",
        PermissionConstants.SYSTEM_NOTIFICATION_CREATE => "Gửi thông báo hệ thống",
        _ => code
    };

    // =====================================================================
    // ROLE
    // =====================================================================
    private static async Task SeedRolesAsync(GymDbContext context)
    {
        var permissions = await context.Permissions.ToListAsync();

        await EnsureRoleAsync(context, DomainConstants.AdminRole, "Quản trị viên hệ thống - toàn quyền.", true,
            permissions.Select(p => p.PermissionCode).ToList(), permissions);

        await EnsureRoleAsync(context, DomainConstants.ManagerRole, "Chủ phòng gym / quản lý chi nhánh.", true,
            permissions.Where(p => !p.PermissionCode.StartsWith("ADMIN_")).Select(p => p.PermissionCode).ToList(), permissions);

        await EnsureRoleAsync(context, DomainConstants.ReceptionistRole, "Nhân viên lễ tân: hội viên, gói tập, thu tiền, check-in.", false,
            new List<string>
            {
                PermissionConstants.MEMBER_VIEW, PermissionConstants.MEMBER_CREATE, PermissionConstants.MEMBER_UPDATE,
                PermissionConstants.MEMBERSHIP_PLAN_VIEW,
                PermissionConstants.SUBSCRIPTION_VIEW, PermissionConstants.SUBSCRIPTION_CREATE, PermissionConstants.SUBSCRIPTION_UPDATE,
                PermissionConstants.BOOKING_VIEW, PermissionConstants.BOOKING_CREATE, PermissionConstants.BOOKING_CANCEL,
                PermissionConstants.CHECKIN_VIEW, PermissionConstants.CHECKIN_CREATE, PermissionConstants.CHECKIN_UPDATE,
                PermissionConstants.INVOICE_VIEW, PermissionConstants.INVOICE_CREATE,
                PermissionConstants.PAYMENT_VIEW, PermissionConstants.PAYMENT_CREATE,
                PermissionConstants.CLASS_VIEW, PermissionConstants.TRAINER_VIEW, PermissionConstants.BRANCH_VIEW,
                PermissionConstants.DASHBOARD_VIEW,
                PermissionConstants.WORKOUT_VIEW, PermissionConstants.WORKOUT_MANAGE, PermissionConstants.WORKOUT_SHARE
            }, permissions);

        await EnsureRoleAsync(context, DomainConstants.TrainerRole, "Huấn luyện viên: xem lớp, lịch dạy và học viên.", false,
            new List<string>
            {
                PermissionConstants.CLASS_VIEW,
                PermissionConstants.CLASS_SCHEDULE_MANAGE,
                PermissionConstants.BOOKING_VIEW, PermissionConstants.BOOKING_UPDATE,
                PermissionConstants.MEMBER_VIEW,
                PermissionConstants.CHECKIN_VIEW,
                PermissionConstants.DASHBOARD_VIEW,
                PermissionConstants.WORKOUT_VIEW, PermissionConstants.WORKOUT_MANAGE, PermissionConstants.WORKOUT_SHARE
            }, permissions);
    }

    private static async Task EnsureRoleAsync(
        GymDbContext context,
        string roleName,
        string description,
        bool isSystem,
        List<string> permissionCodes,
        List<Permission> allPermissions)
    {
        var role = await context.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.RoleName == roleName);

        if (role == null)
        {
            role = new Role
            {
                RoleId = Guid.NewGuid(),
                RoleName = roleName,
                Description = description,
                IsSystem = isSystem,
                CreatedAt = DateTime.UtcNow
            };

            context.Roles.Add(role);
        }

        foreach (var code in permissionCodes)
        {
            var permission = allPermissions.FirstOrDefault(p => p.PermissionCode == code);
            if (permission == null) continue;

            if (!role.Permissions.Any(p => p.PermissionCode == code))
                role.Permissions.Add(permission);
        }

        await context.SaveChangesAsync();
    }

    // =====================================================================
    // TÀI KHOẢN MẶC ĐỊNH
    // =====================================================================
    private static async Task SeedUsersAsync(GymDbContext context)
    {
        var branch = await context.Branches.OrderBy(b => b.CreatedAt).FirstOrDefaultAsync();

        await EnsureUserAsync(context, "admin", "Admin@123", "Quản trị hệ thống",
            "admin@gymsystem.local", "0900000001", DomainConstants.AdminRole, null);

        await EnsureUserAsync(context, "manager", "Manager@123", "Nguyễn Văn Quản Lý",
            "manager@gymsystem.local", "0900000002", DomainConstants.ManagerRole, branch?.BranchId);

        await EnsureUserAsync(context, "reception", "Reception@123", "Trần Thị Lễ Tân",
            "reception@gymsystem.local", "0900000003", DomainConstants.ReceptionistRole, branch?.BranchId);

        await EnsureUserAsync(context, "pt01", "Trainer@123", "Lê Văn Huấn Luyện",
            "pt01@gymsystem.local", "0900000004", DomainConstants.TrainerRole, branch?.BranchId);
    }

    private static async Task EnsureUserAsync(
        GymDbContext context,
        string username,
        string password,
        string fullName,
        string email,
        string phone,
        string roleName,
        Guid? branchId)
    {
        if (await context.Users.AnyAsync(u => u.Username == username)) return;

        var role = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
        var now = DateTime.UtcNow;

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = fullName,
            Email = email,
            PhoneNumber = phone,
            IsActive = true,
            IsVerified = true,
            Status = DomainConstants.UserStatus.Active,
            DefaultBranchId = branchId,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (role != null)
            user.Roles.Add(role);

        context.Users.Add(user);
        await context.SaveChangesAsync();
    }

    // =====================================================================
    // CHI NHÁNH
    // =====================================================================
    private static async Task SeedBranchesAsync(GymDbContext context)
    {
        if (await context.Branches.AnyAsync()) return;

        var manager = await context.Users.FirstOrDefaultAsync(u => u.Username == "manager");
        var now = DateTime.UtcNow;

        var branches = new List<Branch>
        {
            new()
            {
                BranchId = Guid.NewGuid(),
                BranchName = "GYM Center - Chi nhánh Quận 1",
                BranchCode = "BR001",
                Phone = "028 1234 5678",
                Email = "quan1@gymsystem.local",
                Address = "123 Nguyễn Huệ, Phường Bến Nghé",
                City = "TP. Hồ Chí Minh",
                OpeningTime = new TimeSpan(5, 0, 0),
                ClosingTime = new TimeSpan(22, 0, 0),
                ManagerUserId = manager?.UserId,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                BranchId = Guid.NewGuid(),
                BranchName = "GYM Center - Chi nhánh Thủ Đức",
                BranchCode = "BR002",
                Phone = "028 8765 4321",
                Email = "thuduc@gymsystem.local",
                Address = "456 Võ Văn Ngân, Phường Bình Thọ",
                City = "TP. Hồ Chí Minh",
                OpeningTime = new TimeSpan(5, 30, 0),
                ClosingTime = new TimeSpan(21, 30, 0),
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        context.Branches.AddRange(branches);
        await context.SaveChangesAsync();
    }

    // =====================================================================
    // GÓI TẬP
    // =====================================================================
    private static async Task SeedMembershipPlansAsync(GymDbContext context)
    {
        if (await context.MembershipPlans.AnyAsync()) return;

        var now = DateTime.UtcNow;

        var plans = new List<MembershipPlan>
        {
            new()
            {
                PlanId = Guid.NewGuid(), PlanCode = "PLAN001", PlanName = "Gói 1 tháng",
                Description = "Tập không giới hạn 30 ngày, không kèm PT.",
                DurationDays = 30, Price = 600000, MaxFreezeDays = 3, IsActive = true,
                CreatedAt = now, UpdatedAt = now
            },
            new()
            {
                PlanId = Guid.NewGuid(), PlanCode = "PLAN002", PlanName = "Gói 3 tháng",
                Description = "Tập không giới hạn 90 ngày, tặng 1 buổi PT.",
                DurationDays = 90, Price = 1500000, PersonalTrainingSessions = 1, MaxFreezeDays = 7, IsActive = true,
                CreatedAt = now, UpdatedAt = now
            },
            new()
            {
                PlanId = Guid.NewGuid(), PlanCode = "PLAN003", PlanName = "Gói 6 tháng",
                Description = "Tập không giới hạn 180 ngày, tặng 5 buổi PT.",
                DurationDays = 180, Price = 2700000, PersonalTrainingSessions = 5, MaxFreezeDays = 15, IsActive = true,
                CreatedAt = now, UpdatedAt = now
            },
            new()
            {
                PlanId = Guid.NewGuid(), PlanCode = "PLAN004", PlanName = "Gói 12 tháng",
                Description = "Tập không giới hạn 365 ngày, tặng 12 buổi PT và 1 tháng bảo lưu.",
                DurationDays = 365, Price = 4800000, PersonalTrainingSessions = 12, MaxFreezeDays = 30, IsActive = true,
                CreatedAt = now, UpdatedAt = now
            }
        };

        context.MembershipPlans.AddRange(plans);
        await context.SaveChangesAsync();
    }

    // =====================================================================
    // HUẤN LUYỆN VIÊN
    // =====================================================================
    private static async Task SeedTrainersAsync(GymDbContext context)
    {
        if (await context.Trainers.AnyAsync()) return;

        var branchId = await context.Branches.OrderBy(b => b.CreatedAt).Select(b => (Guid?)b.BranchId).FirstOrDefaultAsync();
        var trainerUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "pt01");
        var now = DateTime.UtcNow;

        var trainers = new List<Trainer>
        {
            new()
            {
                TrainerId = Guid.NewGuid(), TrainerCode = "PT0001", FullName = "Lê Văn Huấn Luyện",
                Phone = "0900000004", Email = "pt01@gymsystem.local", Gender = DomainConstants.Gender.Male,
                Specialization = "Gym - Thể hình", Bio = "8 năm kinh nghiệm huấn luyện thể hình và tăng cơ.",
                HourlyRate = 250000, JoinDate = now.AddMonths(-18), UserId = trainerUser?.UserId,
                BranchId = branchId, IsActive = true, CreatedAt = now, UpdatedAt = now
            },
            new()
            {
                TrainerId = Guid.NewGuid(), TrainerCode = "PT0002", FullName = "Phạm Thị Yoga",
                Phone = "0900000005", Email = "pt02@gymsystem.local", Gender = DomainConstants.Gender.Female,
                Specialization = "Yoga - Pilates", Bio = "Chứng chỉ Yoga quốc tế RYT-200.",
                HourlyRate = 300000, JoinDate = now.AddMonths(-12), BranchId = branchId,
                IsActive = true, CreatedAt = now, UpdatedAt = now
            },
            new()
            {
                TrainerId = Guid.NewGuid(), TrainerCode = "PT0003", FullName = "Hoàng Văn Boxing",
                Phone = "0900000006", Email = "pt03@gymsystem.local", Gender = DomainConstants.Gender.Male,
                Specialization = "Boxing - MMA", Bio = "Cựu vận động viên boxing, huấn luyện viên quốc gia.",
                HourlyRate = 350000, JoinDate = now.AddMonths(-6), BranchId = branchId,
                IsActive = true, CreatedAt = now, UpdatedAt = now
            }
        };

        context.Trainers.AddRange(trainers);
        await context.SaveChangesAsync();
    }

    // =====================================================================
    // LỚP HỌC & LỊCH HỌC
    // =====================================================================
    private static async Task SeedTrainingClassesAsync(GymDbContext context)
    {
        if (await context.TrainingClasses.AnyAsync()) return;

        var branchId = await context.Branches.OrderBy(b => b.CreatedAt).Select(b => (Guid?)b.BranchId).FirstOrDefaultAsync();
        var trainers = await context.Trainers.OrderBy(t => t.TrainerCode).ToListAsync();
        var now = DateTime.UtcNow;

        var yoga = trainers.FirstOrDefault(t => t.TrainerCode == "PT0002");
        var boxing = trainers.FirstOrDefault(t => t.TrainerCode == "PT0003");
        var gym = trainers.FirstOrDefault(t => t.TrainerCode == "PT0001");

        var classes = new List<TrainingClass>
        {
            new()
            {
                ClassId = Guid.NewGuid(), ClassCode = "CLS001", ClassName = "Yoga cơ bản",
                Description = "Lớp yoga cho người mới bắt đầu, tập trung hơi thở và dẻo dai.",
                Level = DomainConstants.ClassLevel.Beginner, TrainerId = yoga?.TrainerId, BranchId = branchId,
                Capacity = 20, DurationMinutes = 60, PricePerSession = 80000, IsActive = true,
                CreatedAt = now, UpdatedAt = now
            },
            new()
            {
                ClassId = Guid.NewGuid(), ClassCode = "CLS002", ClassName = "Boxing nâng cao",
                Description = "Lớp boxing kỹ thuật và thể lực dành cho học viên đã có nền tảng.",
                Level = DomainConstants.ClassLevel.Advanced, TrainerId = boxing?.TrainerId, BranchId = branchId,
                Capacity = 15, DurationMinutes = 75, PricePerSession = 120000, IsActive = true,
                CreatedAt = now, UpdatedAt = now
            },
            new()
            {
                ClassId = Guid.NewGuid(), ClassCode = "CLS003", ClassName = "Gym tổng hợp",
                Description = "Lớp tập sức mạnh - tăng cơ theo giáo án cá nhân hóa.",
                Level = DomainConstants.ClassLevel.Intermediate, TrainerId = gym?.TrainerId, BranchId = branchId,
                Capacity = 25, DurationMinutes = 90, PricePerSession = 100000, IsActive = true,
                CreatedAt = now, UpdatedAt = now
            },
            new()
            {
                ClassId = Guid.NewGuid(), ClassCode = "CLS004", ClassName = "Zumba - Cardio",
                Description = "Lớp nhảy giảm cân kết hợp cardio sôi động.",
                Level = DomainConstants.ClassLevel.Beginner, TrainerId = yoga?.TrainerId, BranchId = branchId,
                Capacity = 30, DurationMinutes = 45, PricePerSession = 70000, IsActive = true,
                CreatedAt = now, UpdatedAt = now
            }
        };

        context.TrainingClasses.AddRange(classes);
        await context.SaveChangesAsync();

        var schedules = new List<ClassSchedule>();
        var slots = new (TimeSpan Start, TimeSpan End)[] { (new(6, 0, 0), new(7, 0, 0)), (new(18, 0, 0), new(19, 0, 0)) };

        foreach (var trainingClass in classes)
        {
            schedules.Add(new ClassSchedule
            {
                ScheduleId = Guid.NewGuid(),
                ClassId = trainingClass.ClassId,
                TrainerId = trainingClass.TrainerId,
                Room = "Phòng 1",
                DayOfWeek = DayOfWeek.Monday,
                StartTime = slots[0].Start,
                EndTime = slots[0].End,
                EffectiveFrom = now,
                IsActive = true,
                CreatedAt = now
            });

            schedules.Add(new ClassSchedule
            {
                ScheduleId = Guid.NewGuid(),
                ClassId = trainingClass.ClassId,
                TrainerId = trainingClass.TrainerId,
                Room = "Phòng 2",
                DayOfWeek = DayOfWeek.Wednesday,
                StartTime = slots[1].Start,
                EndTime = slots[1].End,
                EffectiveFrom = now,
                IsActive = true,
                CreatedAt = now
            });
        }

        context.ClassSchedules.AddRange(schedules);
        await context.SaveChangesAsync();
    }

    // =====================================================================
    // HỘI VIÊN MẪU + GÓI TẬP + HÓA ĐƠN + CHECK-IN
    // =====================================================================
    private static async Task SeedMembersAsync(GymDbContext context)
    {
        if (await context.Members.AnyAsync()) return;

        var branchId = await context.Branches.OrderBy(b => b.CreatedAt).Select(b => (Guid?)b.BranchId).FirstOrDefaultAsync();
        var plans = await context.MembershipPlans.OrderBy(p => p.DurationDays).ToListAsync();
        var classes = await context.TrainingClasses.OrderBy(c => c.ClassCode).ToListAsync();
        var schedules = await context.ClassSchedules.OrderBy(s => s.CreatedAt).ToListAsync();

        if (plans.Count == 0) return;

        var now = DateTime.UtcNow;

        var seedMembers = new (string Name, string Phone, string Gender, int PlanIndex, int MonthsAgo)[]
        {
            ("Nguyễn Minh Anh", "0911111111", DomainConstants.Gender.Female, 0, 0),
            ("Trần Quốc Bảo", "0911111112", DomainConstants.Gender.Male, 1, 1),
            ("Lê Thu Hà", "0911111113", DomainConstants.Gender.Female, 2, 2),
            ("Phạm Đức Huy", "0911111114", DomainConstants.Gender.Male, 0, 1),
            ("Võ Ngọc Lan", "0911111115", DomainConstants.Gender.Female, 3, 3),
            ("Đặng Văn Khoa", "0911111116", DomainConstants.Gender.Male, 1, 2),
            ("Bùi Thị Mai", "0911111117", DomainConstants.Gender.Female, 2, 1),
            ("Hồ Nhật Nam", "0911111118", DomainConstants.Gender.Male, 0, 0),
            ("Ngô Thanh Tùng", "0911111119", DomainConstants.Gender.Male, 1, 4),
            ("Dương Thùy Dung", "0911111120", DomainConstants.Gender.Female, 2, 5)
        };

        var members = new List<Member>();
        var subscriptions = new List<MemberSubscription>();
        var invoices = new List<Invoice>();
        var payments = new List<Payment>();
        var checkIns = new List<CheckIn>();
        var bookings = new List<ClassBooking>();

        var memberSequence = 1;

        foreach (var seed in seedMembers)
        {
            var member = new Member
            {
                MemberId = Guid.NewGuid(),
                MemberCode = CodeGenerator.MemberCode(memberSequence++),
                FullName = seed.Name,
                Phone = seed.Phone,
                Email = $"{seed.Phone}@gymmember.local",
                Gender = seed.Gender,
                DateOfBirth = new DateTime(1995, 1, 1).AddDays(memberSequence * 37),
                Address = "TP. Hồ Chí Minh",
                City = "TP. Hồ Chí Minh",
                JoinDate = now.AddMonths(-seed.MonthsAgo),
                Status = DomainConstants.MemberStatus.Active,
                BranchId = branchId,
                CreatedAt = now,
                UpdatedAt = now
            };

            members.Add(member);

            var plan = plans[seed.PlanIndex % plans.Count];
            var startDate = now.AddMonths(-seed.MonthsAgo).Date;
            var endDate = startDate.AddDays(plan.DurationDays);
            var isActive = endDate > now;

            var subscription = new MemberSubscription
            {
                SubscriptionId = Guid.NewGuid(),
                SubscriptionCode = CodeGenerator.SubscriptionCode(startDate, subscriptions.Count + 1),
                MemberId = member.MemberId,
                PlanId = plan.PlanId,
                BranchId = branchId,
                StartDate = startDate,
                EndDate = endDate,
                Status = isActive ? DomainConstants.SubscriptionStatus.Active : DomainConstants.SubscriptionStatus.Expired,
                TotalPrice = plan.Price,
                DiscountAmount = 0,
                FinalAmount = plan.Price,
                PaidAmount = plan.Price,
                PaymentStatus = DomainConstants.PaymentStatus.Paid,
                RemainingPersonalTrainingSessions = plan.PersonalTrainingSessions ?? 0,
                CreatedAt = startDate,
                UpdatedAt = now
            };

            subscriptions.Add(subscription);

            var invoice = new Invoice
            {
                InvoiceId = Guid.NewGuid(),
                InvoiceCode = CodeGenerator.InvoiceCode(startDate, invoices.Count + 1),
                MemberId = member.MemberId,
                BranchId = branchId,
                SubscriptionId = subscription.SubscriptionId,
                InvoiceDate = startDate,
                SubTotal = plan.Price,
                DiscountAmount = 0,
                TotalAmount = plan.Price,
                PaidAmount = plan.Price,
                Status = DomainConstants.InvoiceStatus.Paid,
                Notes = $"Hóa đơn đăng ký gói tập {plan.PlanName}",
                CreatedAt = startDate,
                UpdatedAt = now
            };

            invoice.Items.Add(new InvoiceItem
            {
                InvoiceItemId = Guid.NewGuid(),
                ItemType = DomainConstants.InvoiceItemType.Subscription,
                ReferenceId = plan.PlanId,
                ItemName = plan.PlanName,
                Quantity = 1,
                UnitPrice = plan.Price,
                Amount = plan.Price
            });

            invoices.Add(invoice);

            payments.Add(new Payment
            {
                PaymentId = Guid.NewGuid(),
                PaymentCode = CodeGenerator.PaymentCode(startDate, payments.Count + 1),
                InvoiceId = invoice.InvoiceId,
                MemberId = member.MemberId,
                BranchId = branchId,
                PaymentMethod = payments.Count % 2 == 0 ? DomainConstants.PaymentMethod.Cash : DomainConstants.PaymentMethod.Transfer,
                Amount = plan.Price,
                PaymentDate = startDate,
                Status = DomainConstants.PaymentStatus.Success,
                Notes = "Thu tiền đăng ký gói tập",
                CreatedAt = startDate
            });

            // Lịch sử ra vào trong 14 ngày gần nhất
            for (var day = 1; day <= 14; day += 3)
            {
                if (!isActive) continue;

                var checkInTime = VietnamTime.TodayStartUtc.AddDays(-day).AddHours(6 + (memberSequence % 8));
                checkIns.Add(new CheckIn
                {
                    CheckInId = Guid.NewGuid(),
                    MemberId = member.MemberId,
                    BranchId = branchId,
                    CheckInTime = checkInTime,
                    CheckOutTime = checkInTime.AddMinutes(60 + memberSequence),
                    Method = DomainConstants.CheckInMethod.Qr,
                    CreatedAt = checkInTime
                });
            }
        }

        context.Members.AddRange(members);
        context.MemberSubscriptions.AddRange(subscriptions);
        context.Invoices.AddRange(invoices);
        context.Payments.AddRange(payments);
        context.CheckIns.AddRange(checkIns);
        await context.SaveChangesAsync();

        // Đăng ký lớp học mẫu cho 4 hội viên đầu
        var bookable = members.Take(4).ToList();
        for (var i = 0; i < bookable.Count && i < classes.Count; i++)
        {
            var trainingClass = classes[i];
            var schedule = schedules.FirstOrDefault(s => s.ClassId == trainingClass.ClassId);

            bookings.Add(new ClassBooking
            {
                BookingId = Guid.NewGuid(),
                ClassId = trainingClass.ClassId,
                ScheduleId = schedule?.ScheduleId,
                MemberId = bookable[i].MemberId,
                BookedAt = now,
                Status = DomainConstants.BookingStatus.Booked,
                Notes = "Đăng ký mẫu khi khởi tạo dữ liệu"
            });
        }

        context.ClassBookings.AddRange(bookings);
        await context.SaveChangesAsync();
    }

    // =====================================================================
    // THƯ VIỆN TẬP LUYỆN: BÀI TẬP MẪU + GIÁO ÁN MẪU
    // =====================================================================
    private static async Task SeedWorkoutLibraryAsync(GymDbContext context)
    {
        if (!await context.Exercises.AnyAsync(e => e.UserId == null))
        {
            var now = DateTime.UtcNow;

            var exercises = new List<Exercise>
            {
                NewExercise("Bench Press", "Ngực", 4, 10, 90, "Giữ vai cố định, hạ tạ chậm 2 giây.", now),
                NewExercise("Squat", "Chân", 4, 12, 120, "Lưng thẳng, xuống tới đùi song song sàn.", now),
                NewExercise("Deadlift", "Lưng", 3, 8, 150, "Siết core, không gù lưng.", now),
                NewExercise("Kéo xà", "Lưng", 3, 8, 90, "Kéo bằng lưng, không đung đưa.", now),
                NewExercise("Đẩy vai tạ đôi", "Vai", 3, 12, 75, null, now),
                NewExercise("Curl tạ tay", "Tay", 3, 15, 60, "Không dùng đà.", now),
                NewExercise("Plank", "Bụng", 3, 1, 45, "Giữ 45 giây mỗi hiệp.", now),
                NewExercise("Chạy bộ 3km", "Cardio", 1, 1, 0, "Pace 6:00/km.", now)
            };

            context.Exercises.AddRange(exercises);
            await context.SaveChangesAsync();
        }

        if (await context.WorkoutPlans.AnyAsync(p => p.UserId == null)) return;

        var templates = new (string Name, string Focus, string? Note, (string Exercise, int Sets, int Reps, int RestSeconds)[] Items)[]
        {
            ("Buổi Push (Ngực - Vai - Tay sau)", "Ngực · Vai · Tay", "Khởi động 5 phút, nghỉ 90 giây giữa các hiệp nặng.",
                new[] { ("Bench Press", 4, 10, 90), ("Đẩy vai tạ đôi", 4, 12, 75), ("Curl tạ tay", 3, 15, 60) }),
            ("Buổi Pull (Lưng - Tay trước)", "Lưng · Tay", "Tập trung cảm nhận cơ lưng, không dùng đà.",
                new[] { ("Deadlift", 4, 8, 150), ("Kéo xà", 4, 8, 90), ("Curl tạ tay", 3, 12, 60) }),
            ("Buổi Legs (Chân - Mông)", "Chân · Bụng", "Khởi động khớp gối kỹ trước khi vào hiệp nặng.",
                new[] { ("Squat", 5, 10, 120), ("Plank", 3, 1, 45) }),
            ("Buổi Cardio + Core", "Cardio · Bụng", "Buổi nhẹ, phù hợp ngày giữa tuần.",
                new[] { ("Chạy bộ 3km", 1, 1, 0), ("Plank", 4, 1, 45) })
        };

        var exerciseLookup = await context.Exercises
            .Where(e => e.UserId == null)
            .ToDictionaryAsync(e => e.Name, e => e.ExerciseId);

        var now2 = DateTime.UtcNow;
        var plans = new List<WorkoutPlan>();

        foreach (var template in templates)
        {
            var plan = new WorkoutPlan
            {
                WorkoutPlanId = Guid.NewGuid(),
                UserId = null,
                Name = template.Name,
                Focus = template.Focus,
                Note = template.Note,
                IsActive = true,
                CreatedAt = now2,
                UpdatedAt = now2
            };

            var order = 1;

            foreach (var item in template.Items)
            {
                plan.Items.Add(new WorkoutPlanItem
                {
                    WorkoutPlanItemId = Guid.NewGuid(),
                    WorkoutPlanId = plan.WorkoutPlanId,
                    OrderIndex = order++,
                    ExerciseId = exerciseLookup.GetValueOrDefault(item.Exercise),
                    ExerciseName = item.Exercise,
                    TargetSets = item.Sets,
                    TargetReps = item.Reps,
                    RestSeconds = item.RestSeconds
                });
            }

            plans.Add(plan);
        }

        context.WorkoutPlans.AddRange(plans);
        await context.SaveChangesAsync();
    }

    private static Exercise NewExercise(string name, string group, int sets, int reps, int rest, string? note, DateTime now) => new()
    {
        ExerciseId = Guid.NewGuid(),
        UserId = null,
        Name = name,
        MuscleGroup = group,
        DefaultSets = sets,
        DefaultReps = reps,
        DefaultRestSeconds = rest,
        Note = note,
        IsActive = true,
        CreatedAt = now,
        UpdatedAt = now
    };
}
