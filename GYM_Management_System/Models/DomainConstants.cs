namespace GYM_Management_System.Models;

/// <summary>
/// Tập trung toàn bộ hằng số nghiệp vụ (trạng thái, phương thức thanh toán, level lớp học...).
/// Dùng string thay vì enum để dữ liệu dễ đọc trong DB và không vỡ dữ liệu cũ khi thêm giá trị mới.
/// </summary>
public static class DomainConstants
{
    public static class UserStatus
    {
        public const string Active = "ACTIVE";
        public const string Suspended = "SUSPENDED";
        public const string Banned = "BANNED";
        public const string Deleted = "DELETED";
    }

    public static class MemberStatus
    {
        public const string Active = "ACTIVE";
        public const string Inactive = "INACTIVE";
        public const string Suspended = "SUSPENDED";
    }

    public static class SubscriptionStatus
    {
        public const string Active = "ACTIVE";
        public const string Expired = "EXPIRED";
        public const string Cancelled = "CANCELLED";
        public const string Frozen = "FROZEN";
    }

    public static class BookingStatus
    {
        public const string Booked = "BOOKED";
        public const string Attended = "ATTENDED";
        public const string Cancelled = "CANCELLED";
        public const string NoShow = "NO_SHOW";
    }

    public static class InvoiceStatus
    {
        public const string Unpaid = "UNPAID";
        public const string Partial = "PARTIAL";
        public const string Paid = "PAID";
        public const string Cancelled = "CANCELLED";
    }

    public static class PaymentStatus
    {
        public const string Unpaid = "UNPAID";
        public const string Partial = "PARTIAL";
        public const string Paid = "PAID";
        public const string Success = "SUCCESS";
        public const string Pending = "PENDING";
        public const string Failed = "FAILED";
        public const string Refunded = "REFUNDED";
    }

    public static class PaymentMethod
    {
        public const string Cash = "CASH";
        public const string Transfer = "TRANSFER";
        public const string Card = "CARD";
        public const string EWallet = "EWALLET";
    }

    public static class CheckInMethod
    {
        public const string Qr = "QR";
        public const string Code = "CODE";
        public const string Manual = "MANUAL";
    }

    public static class ClassLevel
    {
        public const string Beginner = "BEGINNER";
        public const string Intermediate = "INTERMEDIATE";
        public const string Advanced = "ADVANCED";
    }

    public static class InvoiceItemType
    {
        public const string Subscription = "SUBSCRIPTION";
        public const string Class = "CLASS";
        public const string PersonalTraining = "PERSONAL_TRAINING";
        public const string Product = "PRODUCT";
        public const string Service = "SERVICE";
    }

    public static class Gender
    {
        public const string Male = "MALE";
        public const string Female = "FEMALE";
        public const string Other = "OTHER";
    }

    public static class LogLevel
    {
        public const string Info = "INFO";
        public const string Warning = "WARNING";
        public const string Error = "ERROR";
    }

    /// <summary>Role Admin - toàn quyền hệ thống.</summary>
    public const string AdminRole = "Admin";

    /// <summary>Role chủ phòng gym / quản lý - bypass mọi permission (trừ nhóm ADMIN_).</summary>
    public const string ManagerRole = "Manager";

    /// <summary>Role lễ nhân viên lễ tân.</summary>
    public const string ReceptionistRole = "Receptionist";

    /// <summary>Role huấn luyện viên.</summary>
    public const string TrainerRole = "Trainer";
}
