# GYM System - Backend (.NET 8 / ASP.NET Core Web API)

Backend quản lý phòng gym: hội viên - gói tập - lớp học - check-in - hóa đơn/thanh toán - phân quyền RBAC.
Cấu trúc và quy ước code được xây theo mẫu dự án `MEP_Management_System` (cùng "họ" với sơ đồ
`Controllers / Services / Models / DTOs / Middleware / Migrations / Program.cs / appsettings.json`).

## 1. Cấu trúc thư mục

```text
GYM System/
├── GYM_Management_System.sln
├── GYM_Management_System/                     # Project Web API chính
│   ├── Authorization/                         # RBAC: PermissionConstants, Policy, Handler, [HasPermission]
│   ├── Controllers/                           # Tầng API - chia nhóm theo nghiệp vụ
│   │   ├── AuthenticationController/          #   AuthController, AccountSettingsController
│   │   ├── MemberController/                  #   MembersController
│   │   ├── MembershipController/              #   MembershipPlansController, SubscriptionsController
│   │   ├── TrainerController/                 #   TrainersController
│   │   ├── ClassController/                   #   TrainingClassesController, ClassSchedulesController, ClassBookingsController
│   │   ├── AttendanceController/              #   CheckInsController
│   │   ├── BillingController/                 #   InvoicesController, PaymentsController
│   │   ├── BranchController/                  #   BranchesController
│   │   ├── DashboardController/               #   DashboardController
│   │   ├── AdminController/                   #   AdminUsersController, AdminRolesController, AdminPermissionsController
│   │   └── SystemController/                  #   SystemLogsController, SystemNotificationsController
│   ├── Services/                              # Tầng Business Logic (mỗi nghiệp vụ 1 cặp I*/Impl)
│   │   ├── AuthServices/                      #   IAuthService / AuthService
│   │   ├── MemberServices/                    #   IMemberService / MemberService
│   │   ├── MembershipServices/                #   IMembershipPlanService, ISubscriptionService
│   │   ├── TrainerServices/                   #   ITrainerService
│   │   ├── ClassServices/                     #   ITrainingClassService, IClassScheduleService, IClassBookingService
│   │   ├── AttendanceServices/                #   ICheckInService
│   │   ├── BillingServices/                   #   IInvoiceService, IPaymentService
│   │   ├── BranchServices/                    #   IBranchService
│   │   ├── DashboardServices/                 #   IDashboardService
│   │   ├── AdminServices/                     #   IAdminUserService, IAdminRoleService
│   │   ├── SystemServices/                    #   ISystemLogService, ISystemNotificationService
│   │   ├── Email/                             #   EmailService (OTP, link đặt lại mật khẩu)
│   │   ├── JwtHelper.cs                       #   Sinh / đọc JWT
│   │   ├── CodeGenerator.cs                   #   Sinh mã MEM/SUB/INV/PAY/OTP...
│   │   ├── CurrentUserAccessor.cs             #   Lấy user/branch/IP đang đăng nhập
│   │   ├── SignalRNotificationSender.cs       #   Đẩy thông báo realtime
│   │   └── VietnamTime.cs                     #   Quy đổi múi giờ VN (UTC+7)
│   ├── Models/                                # Entity + DbContext
│   │   ├── GymDbContext.cs                    #   Cấu hình bảng, index, quan hệ
│   │   ├── GymDbContextFactory.cs             #   Design-time factory cho `dotnet ef`
│   │   ├── DomainConstants.cs                 #   Trạng thái, phương thức thanh toán, level lớp...
│   │   └── *.cs                               #   User, Role, Permission, Branch, Member, MembershipPlan,
│   │                                          #   MemberSubscription, Trainer, TrainingClass, ClassSchedule,
│   │                                          #   ClassBooking, CheckIn, Invoice, InvoiceItem, Payment,
│   │                                          #   SystemLog, SystemNotification, OtpVerification, PasswordResetToken
│   ├── DTOs/                                  # Object truyền tải dữ liệu (Request/Response) theo nhóm
│   │   ├── ApiResponse.cs                     #   ApiResponse<T> + PaginatedResponse<T>
│   │   ├── AuthDTOs/ MemberDTOs/ MembershipDTOs/ TrainerDTOs/ ClassDTOs/
│   │   └── AttendanceDTOs/ BillingDTOs/ BranchDTOs/ DashboardDTOs/ AdminDTOs/ SystemDTOs/
│   ├── Middleware/                            # GlobalExceptionMiddleware, ActiveUserMiddleware
│   ├── Exceptions/                            # BusinessException (lỗi nghiệp vụ → HTTP 400)
│   ├── Hubs/                                  # GymHub (SignalR realtime)
│   ├── Migrations/                            # Migration EF Core (PostgreSQL)
│   ├── Seed/                                  # DatabaseSeeder (quyền, role, tài khoản, dữ liệu mẫu)
│   ├── Properties/launchSettings.json
│   ├── Program.cs                             # DI, JWT, RBAC, CORS, Swagger, migrate + seed
│   └── appsettings.json                       # Cấu hình DB, JWT, Email, CORS
└── GYM_Management_System.UnitTests/           # xUnit: CodeGenerator, RBAC policy, JwtHelper, Service
```

### Khác biệt nhỏ so với sơ đồ thư mục ban đầu

| Sơ đồ gốc | Trong dự án | Lý do |
|---|---|---|
| `Repositories/`, `Data/` | Gộp vào `Models/` (DbContext) + `Seed/` | Giống MEP: service dùng EF Core DbContext trực tiếp, không thêm tầng repository trung gian |
| `Mappings/` (AutoMapper) | Bỏ AutoMapper, mỗi service có hàm `MapToDto` tường minh | Mapping được compiler kiểm tra, dễ đọc/sửa hơn khi mới bắt đầu. Muốn dùng AutoMapper chỉ cần thêm package + `Profile` rồi đổi `MapToDto` |
| `DTOs/` | `DTOs/<Nhóm>DTOs/` | Chia nhóm như MEP để tránh 1 thư mục quá lớn |

## 2. Yêu cầu môi trường

- .NET SDK 8.0 trở lên (`dotnet --list-sdks`)
- PostgreSQL 14+ nếu chạy production (không bắt buộc cho dev)
- `dotnet-ef` nếu cần tạo migration: `dotnet tool install --global dotnet-ef`

## 3. Chạy dự án

### Dev nhanh bằng SQLite (không cần cài database)

`appsettings.Development.json` đã cấu hình sẵn `DatabaseProvider = SQLite`,
chạy là có ngay file `gym_dev.db` + dữ liệu mẫu:

```powershell
cd "GYM_Management_System"
dotnet run
# Swagger: http://localhost:5100/swagger
```

### Chạy với PostgreSQL (giống môi trường thật)

1. Sửa `ConnectionStrings:DefaultConnection` trong `appsettings.json` (hoặc dùng
   [user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)).
2. Đặt `DatabaseProvider = PostgreSQL`.
3. Tạo database rồi chạy migration:

```powershell
dotnet ef database update
dotnet run
```

Ứng dụng tự động `Migrate()` + `SeedAsync()` khi khởi động nên không cần tạo bảng thủ công.

### Lệnh hữu ích

```powershell
dotnet build                       # build toàn solution
dotnet test                        # chạy unit test
dotnet ef migrations add <Ten>     # tạo migration mới (dùng provider PostgreSQL)
dotnet ef migrations remove        # xoá migration vừa tạo
```

## 4. Tài khoản mặc định (do DatabaseSeeder tạo)

| Username | Password | Role | Quyền |
|---|---|---|---|
| `admin` | `Admin@123` | Admin | Toàn quyền hệ thống |
| `manager` | `Manager@123` | Manager | Toàn quyền nghiệp vụ phòng gym (trừ nhóm `ADMIN_*`) |
| `reception` | `Reception@123` | Receptionist | Hội viên, gói tập, thu tiền, check-in |
| `pt01` | `Trainer@123` | Trainer | Xem lớp/lịch dạy, học viên, check-in |

> Đổi mật khẩu ngay khi triển khai thật và thay `Jwt:Key` trong `appsettings.json`.

## 5. Nhóm API chính

| Nhóm | Endpoint |
|---|---|
| Xác thực | `POST /api/auth/login`, `POST /api/auth/forgot-password/start`, `POST /api/auth/forgot-password/verify` |
| Tài khoản | `GET /api/account/me`, `PUT /api/account/profile`, `POST /api/account/change-password` |
| Hội viên | `GET/POST /api/members`, `GET/PUT/DELETE /api/members/{id}`, `PATCH /api/members/{id}/status`, `GET /api/members/{id}/subscriptions` |
| Gói tập | `GET/POST /api/membership-plans`, `PUT/DELETE /api/membership-plans/{id}`, `PATCH .../active` |
| Đăng ký gói | `GET/POST /api/subscriptions`, `POST /api/subscriptions/{id}/renew|freeze|unfreeze|cancel`, `GET /api/subscriptions/expiring` |
| Huấn luyện viên | `GET/POST /api/trainers`, `PUT/DELETE /api/trainers/{id}`, `PATCH .../active` |
| Lớp học | `GET/POST /api/training-classes`, `PUT/DELETE /api/training-classes/{id}` |
| Lịch học | `GET/POST /api/class-schedules`, `GET /api/class-schedules/timetable`, `PUT/DELETE /api/class-schedules/{id}` |
| Đăng ký lớp | `GET/POST /api/class-bookings`, `PATCH .../status`, `POST .../cancel` |
| Ra vào | `GET/POST /api/check-ins`, `POST /api/check-ins/check-out`, `GET /api/check-ins/stats` |
| Hóa đơn | `GET/POST /api/invoices`, `GET /api/invoices/{id}`, `POST /api/invoices/{id}/cancel`, `GET /api/invoices/revenue-summary` |
| Thanh toán | `GET/POST /api/payments`, `POST /api/payments/{id}/refund`, `DELETE /api/payments/{id}` |
| Chi nhánh | `GET/POST /api/branches`, `PUT/DELETE /api/branches/{id}` |
| Dashboard | `GET /api/dashboard/overview`, `GET /api/dashboard/expiring-subscriptions` |
| Quản trị | `GET/POST /api/admin/users`, `PATCH /api/admin/users/{id}/status`, `POST /api/admin/users/{id}/reset-password`, `PUT /api/admin/users/{id}/permissions`, `GET/POST/PUT/DELETE /api/admin/roles`, `GET /api/admin/permissions` |
| Hệ thống | `GET/POST /api/system-logs`, `GET /api/notifications`, `POST /api/notifications`, `PATCH /api/notifications/{id}/read` |
| Realtime | SignalR hub: `/r/gymHub` (gửi token qua `?access_token=`) |

Mọi response đều theo format thống nhất:

```json
{ "success": true, "message": "Thông báo", "data": { }, "errors": [] }
```

## 6. Quy tắc phân quyền (RBAC)

1. Mỗi chức năng gắn `[HasPermission(PermissionConstants.XXX)]`; policy được sinh tự động từ
   `PermissionConstants` (thêm quyền mới chỉ cần thêm 1 hằng số).
2. `Admin` → toàn quyền; `Manager` → toàn quyền nghiệp vụ, **không** có quyền nhóm `ADMIN_*`.
3. Các role khác kiểm tra theo thứ tự: chặn riêng (`IsGranted = false`) → cấp riêng (`true`) →
   quyền theo role (`RolePermissions`).
4. Token JWT chứa cả `role` và `permission[]` để FE ẩn/hiện menu theo quyền.

## 7. Ghi chú kỹ thuật

- Thời gian lưu trong DB là **UTC**; `VietnamTime` dùng để tính "hôm nay / đầu tháng" theo giờ VN.
- `GlobalExceptionMiddleware` chuyển `BusinessException` → 400, `KeyNotFoundException` → 404,
  `UnauthorizedAccessException` → 401, còn lại → 500 (luôn theo format `ApiResponse`).
- `ActiveUserMiddleware` chặn ngay tài khoản bị khóa/tạm ngưng dù token còn hạn.
- SQLite không hỗ trợ `SUM` trên `decimal` và `ORDER BY TimeSpan`, nên `GymDbContext` tự chuyển
  `decimal` → `double` và service sắp xếp theo giờ ở phía client **chỉ khi chạy SQLite**;
  PostgreSQL vẫn dùng `decimal(18,2)`.
