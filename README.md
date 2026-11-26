# HTĐ – Quản lý nhân sự (SQL Server)

Ứng dụng ASP.NET Core MVC (.NET 9), Entity Framework Core 9 và Microsoft SQL Server.
Bản này đã thay provider MySQL/Pomelo bằng Microsoft.EntityFrameworkCore.SqlServer.

## Chạy trên Windows bằng Visual Studio

### 1. Chuẩn bị

- Visual Studio có workload **ASP.NET and web development** và hỗ trợ .NET 9 (Visual Studio 2022 từ 17.12 hoặc bản mới hơn hỗ trợ SDK này).
- **.NET 9 SDK**. Kiểm tra bằng `dotnet --list-sdks`.
- SQL Server đang chạy: LocalDB, SQL Server Express hoặc SQL Server Developer.
- SQL Server Management Studio (SSMS) là công cụ quản trị, không thay thế việc cài database engine.
- Có mạng để Visual Studio khôi phục NuGet lần đầu.

### 2. Chọn đúng máy chủ SQL Server

Mở SSMS, xem tên server bạn đang kết nối thành công và sửa `ConnectionStrings:DefaultConnection` trong `appsettings.json`.
Chỉ thay giá trị kết nối, giữ các phần khác. Trong JSON, dấu gạch chéo ngược phải viết thành `\\`.

**LocalDB — cấu hình mặc định:**

```json
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=HumaHrDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

**SQL Server Express:**

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=HumaHrDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

**SQL Server instance mặc định trên máy hiện tại:**

```json
"DefaultConnection": "Server=.;Database=HumaHrDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

**Instance có tên riêng:** nếu SSMS dùng `TENMAY\TENINSTANCE`, cấu hình:

```json
"DefaultConnection": "Server=TENMAY\\TENINSTANCE;Database=HumaHrDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

Các ví dụ trên dùng Windows Authentication, tức tài khoản Windows đang chạy Visual Studio.
Nếu sử dụng SQL Server Authentication, bật chế độ xác thực tương ứng trên server và dùng tài khoản đã được cấp quyền:

```json
"DefaultConnection": "Server=TEN_SERVER;Database=HumaHrDb;User Id=TEN_DANG_NHAP;Password=MAT_KHAU;TrustServerCertificate=True;"
```

`TrustServerCertificate=True` được đặt để chạy ở môi trường học tập trên máy cục bộ. Máy chủ triển khai thực cần cấu hình chứng chỉ phù hợp.

### 3. Mở và chạy

1. Giải nén dự án vào ví dụ `C:\DuAn\HumaHr`.
2. Mở **HumaHr.sln** bằng Visual Studio.
3. Sửa chuỗi kết nối như trên. Tài khoản kết nối phải được phép tạo database mới hoặc quản lý các bảng trong database đã được cấp riêng.
4. Chờ khôi phục NuGet; chọn project HumaHr làm Startup Project nếu cần.
5. Chọn profile **http**, bấm **Ctrl + F5** hoặc **F5**.
6. Mở `http://localhost:5080`; nếu Output hiển thị cổng khác thì mở đúng địa chỉ `Now listening on`.

Có thể chạy bằng Terminal trong thư mục chứa HumaHr.csproj:

```powershell
dotnet restore
dotnet build
dotnet run --launch-profile http
```

### 4. Tạo dữ liệu và đăng nhập

Lần chạy đầu, ứng dụng dùng `EnsureCreated()` để tạo database và đủ 11 bảng từ EF Core model, thiết lập cấu hình lịch/lương Id = 1, tạo tài khoản quản trị và dữ liệu minh họa.
Không cần chạy script SQL trước và không cần dùng `Update-Database`.

**Tài khoản quản trị mặc định của database mới:**

- Email: `admin@gmail.com`
- Mật khẩu: `HumaHr@2026`

Có thể sửa `AdminAccount` trong cấu hình trước lần chạy đầu. Đổi mật khẩu trên giao diện sau khi đăng nhập.
Thay `AdminAccount:Password` sau khi tài khoản đã tồn tại không đặt lại mật khẩu đã lưu.
Các hồ sơ nhân viên demo chưa được cấp tài khoản; Admin vào Nhân viên → sửa hồ sơ → chọn cấp tài khoản để tạo.

Trong SSMS, Refresh mục Databases → **HumaHrDb** → Tables để xem các bảng.

## Các lỗi thường gặp

| Thông báo / tình huống | Cách xử lý |
| --- | --- |
| Không tìm thấy SDK / NETSDK1045 | Cài .NET 9 SDK và dùng Visual Studio hỗ trợ .NET 9; đóng rồi mở lại Visual Studio. |
| Lỗi network / instance-specific, error 26 hoặc 40 | Kiểm tra tên server bằng SSMS và bảo đảm dịch vụ instance đang chạy. Nếu chưa có LocalDB, dùng đúng instance SQL Server đã cài. |
| Login failed | Với Trusted_Connection, cấp quyền cho tài khoản Windows hiện tại; nếu dùng SQL Authentication, kiểm tra login và chế độ xác thực của server. |
| CREATE DATABASE permission denied | Nhờ tài khoản quản trị SQL tạo một database trống riêng và cấp quyền tạo bảng/đọc/ghi, hoặc cấp quyền tạo database cho tài khoản phù hợp. |
| Invalid object name / thiếu cột | Database đích có sẵn bảng hoặc không đúng phiên bản. Chọn một tên database mới chưa sử dụng để chạy demo; không xóa dữ liệu đang có. |
| Address already in use | Dừng phiên HumaHr khác hoặc đổi applicationUrl trong Properties/launchSettings.json. |
| NuGet không tải được | Kiểm tra mạng và nguồn https://api.nuget.org/v3/index.json trong Tools → NuGet Package Manager → Package Manager Settings. |

## Phạm vi chuyển đổi dữ liệu

- Bản SQL Server dùng database mới, không tự đọc hoặc di chuyển dữ liệu MySQL cũ.
- Cấu hình Unicode `nvarchar` giữ nội dung tiếng Việt; các chỉ mục duy nhất có giới hạn độ dài phù hợp.
- Liên kết EmployeeId của tài khoản dùng unique filtered index, cho phép tài khoản Admin không liên kết hồ sơ.
- `EnsureCreated()` chỉ khởi tạo khi database chưa có bảng, không nâng cấp lược đồ có sẵn. Khi phát triển lâu dài cần chuẩn bị EF Core migrations riêng; không trộn Update-Database với database tạo bằng EnsureCreated.
- Không có thao tác tự xóa database hoặc dữ liệu cũ.

## Script cơ sở dữ liệu (tùy chọn)

`database/HumaHrDb.Schema.sql` được sinh trực tiếp từ EF Core model SQL Server. Chỉ dùng khi cần tạo bảng thủ công trong **database trống** đã chọn trong SSMS. Script tạo bảng và cấu hình lịch/lương; tài khoản quản trị và nhân viên demo được tạo khi chạy ứng dụng.
Thông thường chỉ cần chạy ứng dụng để tự khởi tạo, không chạy thêm script.

## Gửi sang máy khác

Gửi file `HumaHr_SQLServer_Windows.zip` (nếu được tạo kèm), giải nén rồi làm theo các bước trên.
Nếu tự nén: giữ solution, csproj, Program.cs, appsettings*.json, Controllers, Models, Data, Services, Views, wwwroot, Properties, database và README.md. Bỏ bin/obj vì máy Windows sẽ tự biên dịch lại.
File ZIP mã nguồn không bao gồm database đã nhập của người dùng.

## Kiểm tra và giới hạn

Có thể biên dịch và kiểm tra mô hình SQL Server / sinh DDL trên macOS mà không cần kết nối máy chủ.
Chạy thực tế, khởi tạo database, đăng nhập và lưu dữ liệu phải được xác nhận trên máy Windows có SQL Server.

Tài liệu chính thức:
- https://learn.microsoft.com/ef/core/providers/sql-server/
- https://learn.microsoft.com/ef/core/managing-schemas/ensure-created

## Bổ sung công demo tháng 10/2026 để thử tính lương

**Cách nhanh trên máy Windows đang chạy:** mở `database/SeedDemoAttendance_2026_10.sql` bằng SSMS, kết nối đúng server chứa HumaHrDb và nhấn **Execute / F5**. Chạy ứng dụng ít nhất một lần trước để có bảng và hồ sơ mẫu. Sau đó tải lại trang Bảng lương, chọn tháng 10/2026, nhập các khoản và xác nhận rà soát thuế → Tính và lưu bản nháp → kiểm tra → Công bố.

**Cách dùng mã nguồn mới:** khi chạy profile `http` ở môi trường Development, `DemoAttendanceSeed` trong `appsettings.Development.json` bật bổ sung công tháng `2026-10`. Hàm mới chạy độc lập với điều kiện “đã có nhân viên” của Initialize. Tắt bằng `"Enabled": false` khi không cần.

Seed chỉ áp dụng 8 hồ sơ demo NV001–NV008 có email tương ứng trong SeedData. Chỉ thêm ngày làm việc còn thiếu theo lịch cấu hình, loại trừ ngày lễ, giới hạn trong thời gian làm việc của nhân viên. Không sửa bản ghi công sẵn có, không thêm cho nhân viên đã có phiếu lương Đã công bố trong kỳ và không ghi đè ngày có đơn nghỉ chưa bị từ chối. Phiếu nháp đã tính bị ảnh hưởng được đánh dấu Cần tính lại. Chạy lại không thêm trùng. Với lịch thứ Hai–thứ Sáu mặc định, chưa có ngày nghỉ/công, 8 hồ sơ demo sẽ có tổng cộng 176 dòng mới (22 ngày/người).

Đây là **dữ liệu giả lập Có mặt**, giờ vào/ra theo cấu hình và không có tăng ca; không thay thế dữ liệu chấm công thực. Những ngày có đơn nghỉ được bỏ qua cần được xử lý qua quy trình nghỉ phép trước khi tính lương. Seed không tự tạo hoặc công bố phiếu lương.
