<<<<<<< HEAD
# JobPortal

JobPortal là website hỗ trợ tìm kiếm việc làm được xây dựng bằng ASP.NET Core MVC, Entity Framework Core và SQL Server. Dự án hỗ trợ các vai trò `Admin`, `Employer`, `Candidate`, đồng thời tích hợp AI để tư vấn nghề nghiệp và phân tích CV.

## Liên kết

- Website đang chạy: [https://c25web.site](https://c25web.site)
- Báo cáo LaTeX trên Overleaf: [https://www.overleaf.com/read/mtbdbhnwxpfh#4b1095](https://www.overleaf.com/read/mtbdbhnwxpfh#4b1095)

## Tính năng chính

- Đăng ký, đăng nhập và phân quyền theo vai trò người dùng
- Đăng tin tuyển dụng, duyệt tin, lưu việc làm và ứng tuyển
- Quản lý CV và hồ sơ ứng tuyển
- Tư vấn nghề nghiệp bằng AI

## Công nghệ sử dụng

- ASP.NET Core MVC
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity
- Bootstrap
- Gemini API

## Cấu trúc thư mục

```text
JobPortal/
|-- Areas/
|-- Controllers/
|-- Data/
|-- database/
|-- Migrations/
|-- Models/
|-- Properties/
|-- scripts/
|-- Services/
|-- Views/
|-- wwwroot/
|-- appsettings.json
|-- appsettings.Local.example.json
|-- appsettings.Hosting.example.json
|-- JobPortal.csproj
|-- JobPortal.sln
|-- Program.cs
|-- README.md
`-- Web.config
```

## Yêu cầu môi trường

- .NET SDK 10
- SQL Server hoặc SQL Server Express
- SQL Server Management Studio để quản lý cơ sở dữ liệu

## Hướng dẫn chạy trên máy khác hoặc máy local mới

### 1. Cài SQL Server

Bạn có thể dùng một trong hai lựa chọn sau:

- SQL Server Express
- LocalDB

Ví dụ connection string:

- SQL Server Express

```text
Server=localhost\SQLEXPRESS;Database=JobPortalDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True;Connect Timeout=60;
```

- LocalDB

```text
Server=(localdb)\MSSQLLocalDB;Database=JobPortalDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True;Connect Timeout=60;
```

Sau khi cài xong, hãy tạo một database rỗng tên `JobPortalDb`.

### 2. Tạo file cấu hình local

Chạy lệnh:

```powershell
Copy-Item appsettings.Local.example.json appsettings.Local.json
```

Sau đó mở `appsettings.Local.json` và sửa các giá trị sau:

- `ConnectionStrings:DefaultConnection`: trỏ tới SQL Server trên máy của bạn
- `GeminiAI:ApiKey`: nhập API key của bạn
- `AppSetup:ApplyMigrationsOnStartup`: để `true` nếu muốn ứng dụng tự tạo hoặc cập nhật bảng
- `AppSetup:SeedDemoDataOnStartup`: để `true` nếu muốn thêm dữ liệu demo

### 3. Chạy dự án

```powershell
dotnet restore
dotnet build
dotnet run
```

Nếu muốn cập nhật database bằng tay thay vì để ứng dụng tự migrate khi khởi động:

```powershell
dotnet tool install --global dotnet-ef
dotnet ef database update
```

Sau khi database đã ổn định, bạn có thể tắt migrate và seed tự động:

```json
"AppSetup": {
  "ApplyMigrationsOnStartup": false,
  "SeedDemoDataOnStartup": false
}
```

## Tài khoản demo

- Admin: `admin@gmail.com` / `123456`
- Employer: `employer@gmail.com` / `123456`

## Hướng dẫn triển khai lên SmarterASP.NET

### 1. Tạo website và database trên host

Trong control panel SmarterASP.NET, bạn cần:

1. Tạo website
2. Tạo SQL Server database
3. Lưu lại các thông tin sau:
   - Tên máy chủ SQL
   - Tên database
   - Tên đăng nhập
   - Mật khẩu

### 2. Cấu hình file `appsettings.json`

Bạn có thể tham khảo file `appsettings.Hosting.example.json` với mẫu như sau:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SMARTERASP_SQL_HOST;Database=YOUR_SMARTERASP_DB;User Id=YOUR_SMARTERASP_DB_USER;Password=YOUR_SMARTERASP_DB_PASSWORD;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True;Connect Timeout=60;"
  }
}
```

Trước khi upload lên host, hãy sửa đúng thông tin database thật trong `appsettings.json`.

### 3. Publish dự án

Không upload toàn bộ source code lên host. Hãy publish theo dạng framework-dependent để nhẹ hơn và phù hợp giới hạn dung lượng host.

```powershell
dotnet publish -c Release /p:PublishProfile=SmarterAspFolder
```

Thư mục publish:

```text
bin\Release\net10.0\publish\smarterasp\
```

Các file như `appsettings.Local.json`, `database/*`, `scripts/*`, `README.md` đã được cấu hình để không đi lên host khi publish.

### 4. Upload lên host

Upload toàn bộ nội dung bên trong thư mục publish vào thư mục gốc của site, ví dụ:

```text
h:\root\home\YOUR_ACCOUNT\www\site1
```

Nếu host vẫn còn file `default.asp`, hãy xóa file đó trước khi upload.

Sau khi upload xong, thư mục gốc của website cần có tối thiểu:

- `JobPortal.dll`
- `appsettings.json`
- `Web.config`
- `wwwroot`

### 5. Khởi động lần đầu

Ở lần chạy đầu tiên:

- Ứng dụng sẽ tự migrate database nếu `AppSetup:ApplyMigrationsOnStartup = true`
- Ứng dụng sẽ tự seed dữ liệu demo nếu `AppSetup:SeedDemoDataOnStartup = true`

Sau khi hệ thống đã tạo database thành công, nên đổi lại:

```json
"AppSetup": {
  "ApplyMigrationsOnStartup": false,
  "SeedDemoDataOnStartup": false
}
```

để website ổn định hơn trong các lần khởi động tiếp theo.

## Lưu ý khi chạy local

Nếu bạn gặp lỗi kiểu `SSL pre-login handshake`, nguyên nhân thường là do máy local đang cố kết nối trực tiếp tới SQL Server của host qua Internet.

Cách xử lý đúng:

- Không dùng connection string database trên host để chạy local
- Tạo SQL Server ngay trên máy của bạn
- Sửa `appsettings.Local.json` để trỏ về SQL Server local
- Chạy lại `dotnet run`

Bạn có thể thử thêm các tham số sau nếu thật sự cần kết nối tới SQL host:

- `Encrypt=False`
- `TrustServerCertificate=True`
- `Connect Timeout=60`

Tuy nhiên cách ổn định nhất vẫn là mỗi máy chạy local sẽ dùng SQL Server local của chính máy đó.

## Ghi chú

- `Web.config` đã được cấu hình để chạy trên IIS và SmarterASP.NET
- `Program.cs` hỗ trợ bật hoặc tắt migrate và seed dữ liệu bằng `AppSetup`
- Không commit `appsettings.Local.json` hoặc các khóa bảo mật thật lên GitHub
=======
# BACKEND-JobPortal
>>>>>>> 9f8553e9e2f6e0a25f8c6935fc290a0d396a1948
