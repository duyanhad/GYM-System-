namespace GYM_Management_System.Authorization;

/// <summary>
/// Tập trung toàn bộ mã Permission (RBAC) của hệ thống quản lý phòng gym.
/// Khi cần thêm quyền mới: thêm hằng số ở đây, KHÔNG cần sửa Handler hay Controller logic.
/// </summary>
public static class PermissionConstants
{
    // === HỘI VIÊN ===
    public const string MEMBER_VIEW = "MEMBER_VIEW";
    public const string MEMBER_CREATE = "MEMBER_CREATE";
    public const string MEMBER_UPDATE = "MEMBER_UPDATE";
    public const string MEMBER_DELETE = "MEMBER_DELETE";

    // === GÓI TẬP ===
    public const string MEMBERSHIP_PLAN_VIEW = "MEMBERSHIP_PLAN_VIEW";
    public const string MEMBERSHIP_PLAN_CREATE = "MEMBERSHIP_PLAN_CREATE";
    public const string MEMBERSHIP_PLAN_UPDATE = "MEMBERSHIP_PLAN_UPDATE";
    public const string MEMBERSHIP_PLAN_DELETE = "MEMBERSHIP_PLAN_DELETE";

    // === ĐĂNG KÝ GÓI TẬP ===
    public const string SUBSCRIPTION_VIEW = "SUBSCRIPTION_VIEW";
    public const string SUBSCRIPTION_CREATE = "SUBSCRIPTION_CREATE";
    public const string SUBSCRIPTION_UPDATE = "SUBSCRIPTION_UPDATE";
    public const string SUBSCRIPTION_FREEZE = "SUBSCRIPTION_FREEZE";
    public const string SUBSCRIPTION_CANCEL = "SUBSCRIPTION_CANCEL";

    // === HUẤN LUYỆN VIÊN ===
    public const string TRAINER_VIEW = "TRAINER_VIEW";
    public const string TRAINER_CREATE = "TRAINER_CREATE";
    public const string TRAINER_UPDATE = "TRAINER_UPDATE";
    public const string TRAINER_DELETE = "TRAINER_DELETE";

    // === LỚP HỌC & LỊCH HỌC ===
    public const string CLASS_VIEW = "CLASS_VIEW";
    public const string CLASS_CREATE = "CLASS_CREATE";
    public const string CLASS_UPDATE = "CLASS_UPDATE";
    public const string CLASS_DELETE = "CLASS_DELETE";
    public const string CLASS_SCHEDULE_MANAGE = "CLASS_SCHEDULE_MANAGE";

    // === ĐẶT LỊCH HỌC ===
    public const string BOOKING_VIEW = "BOOKING_VIEW";
    public const string BOOKING_CREATE = "BOOKING_CREATE";
    public const string BOOKING_UPDATE = "BOOKING_UPDATE";
    public const string BOOKING_CANCEL = "BOOKING_CANCEL";

    // === RA VÀO PHÒNG GYM ===
    public const string CHECKIN_VIEW = "CHECKIN_VIEW";
    public const string CHECKIN_CREATE = "CHECKIN_CREATE";
    public const string CHECKIN_UPDATE = "CHECKIN_UPDATE";

    // === HÓA ĐƠN & THANH TOÁN ===
    public const string INVOICE_VIEW = "INVOICE_VIEW";
    public const string INVOICE_CREATE = "INVOICE_CREATE";
    public const string INVOICE_UPDATE = "INVOICE_UPDATE";
    public const string INVOICE_CANCEL = "INVOICE_CANCEL";
    public const string PAYMENT_VIEW = "PAYMENT_VIEW";
    public const string PAYMENT_CREATE = "PAYMENT_CREATE";
    public const string PAYMENT_UPDATE = "PAYMENT_UPDATE";
    public const string PAYMENT_DELETE = "PAYMENT_DELETE";

    // === CHI NHÁNH ===
    public const string BRANCH_VIEW = "BRANCH_VIEW";
    public const string BRANCH_CREATE = "BRANCH_CREATE";
    public const string BRANCH_UPDATE = "BRANCH_UPDATE";
    public const string BRANCH_DELETE = "BRANCH_DELETE";

    // === BÁO CÁO / DASHBOARD ===
    public const string DASHBOARD_VIEW = "DASHBOARD_VIEW";
    public const string REPORT_VIEW = "REPORT_VIEW";

    // === QUẢN TRỊ HỆ THỐNG (chỉ Admin) ===
    public const string ADMIN_USER_VIEW = "ADMIN_USER_VIEW";
    public const string ADMIN_USER_CREATE = "ADMIN_USER_CREATE";
    public const string ADMIN_USER_UPDATE = "ADMIN_USER_UPDATE";
    public const string ADMIN_USER_DELETE = "ADMIN_USER_DELETE";
    public const string ADMIN_ROLE_VIEW = "ADMIN_ROLE_VIEW";
    public const string ADMIN_ROLE_CREATE = "ADMIN_ROLE_CREATE";
    public const string ADMIN_ROLE_UPDATE = "ADMIN_ROLE_UPDATE";
    public const string ADMIN_ROLE_DELETE = "ADMIN_ROLE_DELETE";
    public const string ADMIN_PERMISSION_ASSIGN = "ADMIN_PERMISSION_ASSIGN";
    public const string SYSTEM_MANAGE = "SYSTEM_MANAGE";

    // === NHẬT KÝ & THÔNG BÁO ===
    public const string SYSTEM_LOG_VIEW = "SYSTEM_LOG_VIEW";
    public const string SYSTEM_NOTIFICATION_VIEW = "SYSTEM_NOTIFICATION_VIEW";
    public const string SYSTEM_NOTIFICATION_CREATE = "SYSTEM_NOTIFICATION_CREATE";

    /// <summary>Loại claim dùng để lưu PermissionCode trong JWT.</summary>
    public const string PERMISSION_CLAIM_TYPE = "permission";
}
