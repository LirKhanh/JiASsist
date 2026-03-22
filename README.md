# JiASsist - Backend

Backend cho Project JiASsist.

## Công nghệ sử dụng 
- .NET 8 (ASP.NET Core Web API)
- C#
- PostgreSQL với `Npgsql` (chuỗi kết nối trong `ConnectionStrings:PostgresDb` của `appsettings.json`)
- JWT Bearer authentication (`Microsoft.AspNetCore.Authentication.JwtBearer` + `Microsoft.IdentityModel.Tokens`)
- Swagger / OpenAPI (`Swashbuckle.AspNetCore`)
- Custom middleware: `ExceptionMiddleware` (xử lý lỗi toàn cục)
- Helpers: `JwtHelper` (tạo/validate token), `PasswordHasher` (PBKDF2 hashing)
- CORS policy `AllowAngular` (origin: `http://localhost:4200`)

> Lưu ý: repository hiện sử dụng `NpgsqlConnection` được đăng ký trong DI để kết nối PostgreSQL. Nếu bạn muốn dùng EF Core, thêm package `Npgsql.EntityFrameworkCore.PostgreSQL` và cấu hình `DbContext`.

## Yêu cầu trước
- .NET 8 SDK
- Git
- PostgreSQL (máy chủ hoặc container) — kết nối mặc định trong `appsettings.json` là `Host=localhost;Port=5432;Database=JiASsist;Username=postgres;Password=admin123`
- (Tùy chọn) `dotnet-ef` nếu dùng Migrations: `dotnet tool install --global dotnet-ef`

## Cách clone
1. Clone repository:

```powershell
git clone https://github.com/LirKhanh/JiASsist.git
cd JiASsist
```

2. Mở project bằng Visual Studio 2022/2026 (mở file `.sln` nếu có) hoặc mở thư mục trong VS Code.

## Cài đặt package chính 
Chạy trong thư mục chứa `*.csproj` của API:

```powershell
dotnet add package Npgsql
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Swashbuckle.AspNetCore
# Nếu dùng EF Core:
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

## Cách cấu hình
- Mở `appsettings.json` và cập nhật:
  - `ConnectionStrings:PostgresDb` — chuỗi kết nối đến PostgreSQL
  - `Jwt` section: `Key`, `Issuer`, `Audience`, `ExpiresInMinutes`

Các lớp liên quan: `Helpers/JwtSettings.cs`, `Helpers/JwtHelper.cs`, `Helpers/PasswordHasher.cs`.

Nếu dự án thêm EF Core `DbContext` và migrations, áp dụng migrations:

```powershell
# vào thư mục chứa file .csproj của project API
dotnet ef database update
```

## Chạy ứng dụng
- Bằng Visual Studio: chọn project API làm startup và chạy (F5 hoặc Ctrl+F5).
- Bằng CLI:

```powershell
dotnet restore
dotnet build
dotnet run --project <PathToYourApiProject.csproj>
```

Ứng dụng mặc định sẽ chạy trên `https://localhost:5001` hoặc endpoint được chỉ định trong `launchSettings.json`.

## Ghi chú
- Đảm bảo thay `Jwt:Key` bằng một giá trị bí mật mạnh trước khi deploy.
- Cập nhật `ConnectionStrings` theo môi trường (dev/staging/production).
- Nếu cần liệt kê các package và phiên bản chính xác từ file `*.csproj`, cung cấp đường dẫn tới project file và tôi sẽ liệt kê giúp.
