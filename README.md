# 🚀 Employee Management System
### Fullstack .NET 8 Blazor WASM & Web API

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)
![Blazor](https://img.shields.io/badge/Blazor-WASM-512BD4?style=flat&logo=blazor)
![License](https://img.shields.io/badge/License-Educational-green)

> **🇬🇧 English:** A comprehensive Employee Management System built with Clean Architecture principles.  
> **🇻🇳 Tiếng Việt:** Hệ thống quản lý nhân sự toàn diện được xây dựng dựa trên nguyên lý Clean Architecture.

---

## 🏗 Project Architecture / Kiến trúc dự án

This project follows the **Clean Architecture** pattern to separate concerns and ensure scalability.  
Dự án tuân thủ mô hình **Clean Architecture** để phân tách các mối quan tâm và đảm bảo khả năng mở rộng.

### 🧩 System Design (Sơ đồ hệ thống)


```mermaid
graph TD
    User[User / Client Browser] <-->|HTTPS / JSON| Client[Client (Blazor WASM)]
    Client <-->|Call Services| ClientLib[Client Library]
    ClientLib <-->|API Requests| Server[Server (Web API)]
    Server <-->|Dependency Injection| ServerLib[Server Library]
    ServerLib <-->|EF Core| DB[(SQL Server Database)]
    
    subgraph Shared [Base Library]
        Models[Entities / DTOs]
    end
    
    Client -.-> Shared
    Server -.-> Shared
    ClientLib -.-> Shared
    ServerLib -.-> Shared
```

📂 Folder Structure & Content / Cấu trúc thư mục & Nội dung

Below is the detailed breakdown of the solution structure.
Dưới đây là chi tiết về cấu trúc của solution.

Plaintext

📦 EmployeeManagementSystem
 ┣ 📂 Server                  # Web API Host
 ┣ 📂 Client                  # Blazor WebAssembly UI
 ┣ 📂 BaseLibrary             # Shared Models & DTOs
 ┣ 📂 ServerLibrary           # Business Logic & Data Access (Backend)
 ┗ 📂 ClientLibrary           # HTTP Services (Frontend)

📝 Detailed Explanation / Giải thích chi tiết

Component / Thành phần

🇬🇧 Description (English)

🇻🇳 Mô tả (Tiếng Việt)

1. BaseLibrary

Shared Kernel. Contains Entities (Employee, Department...) and DTOs used by both Client and Server to ensure type safety.

Thư viện dùng chung. Chứa các Entities (Nhân viên, Phòng ban...) và DTOs được dùng bởi cả Client và Server để đảm bảo đồng bộ kiểu dữ liệu.

   └─ Entities/Database models (e.g., Employee.cs).

Các model đại diện cho bảng CSDL.

   └─ DTOs/Data Transfer Objects for API responses.

Đối tượng chuyển đổi dữ liệu cho phản hồi API.

2. Server

API Entry Point. Includes Controllers and Program.cs configuration. It depends on ServerLibrary.

Đầu vào API. Bao gồm các Controller và cấu hình Program.cs. Dự án này phụ thuộc vào ServerLibrary.

   └─ Controllers/API Endpoints (e.g., AuthenticationController, EmployeeController).

Các điểm cuối API để nhận request.

   └─ appsettings.json

Configuration (Connection Strings, JWT Key).

Cấu hình chuỗi kết nối và khóa bảo mật JWT.

3. ServerLibrary

Backend Logic. Handles Data Access (EF Core), Migrations, and Repositories.

Logic phía Backend. Xử lý truy cập dữ liệu (EF Core), Migrations và các Repository.

   └─ Data/AppDbContext.cs - Database Context.

Ngữ cảnh cơ sở dữ liệu (EF Core Context).

   └─ Repositories/Implementation of Interface (CRUD logic).

Triển khai các Interface (xử lý thêm/sửa/xóa).

4. Client

Frontend UI. The Blazor WASM application containing Pages, Layouts, and Components.

Giao diện người dùng. Ứng dụng Blazor WASM chứa các Trang, Layout và Component.

   └─ Pages/UI Screens (e.g., EmployeeList.razor, Login.razor).

Các màn hình giao diện chính.

   └─ Layout/MainLayout, NavMenu.

Bố cục chung và menu điều hướng.

5. ClientLibrary

Frontend Services. Handles HTTP calls to the API and manages Authentication State.

Dịch vụ phía Frontend. Xử lý gọi HTTP đến API và quản lý trạng thái đăng nhập.

   └─ Services/IUserAccountService, ClientServices.

Các service gọi API.

   └─ Helpers/CustomAuthenticationStateProvider.

Xử lý trạng thái xác thực tùy chỉnh (JWT).

🛠 Tech Stack / Công nghệ sử dụng

Category

Technology

Usage

Core

Framework chính (Core Framework)

Frontend

Blazor WebAssembly

Single Page Application (SPA)

UI Library

Syncfusion Blazor

DataGrid, Charts, PDF Export

Backend

ASP.NET Core Web API

RESTful API

Database

SQL Server / EF Core

Database & ORM

Auth

JWT (JSON Web Token)

Authentication & Authorization

🚀 Features / Tính năng chính

✅ Authentication: Custom JWT Auth, Login/Register, Refresh Token. (Đăng nhập/Đăng ký, bảo mật JWT)

✅ Employee Management: CRUD Operations with Image Upload. (Quản lý nhân viên: Thêm/Sửa/Xóa có ảnh)

✅ Advanced UI: Cascading Dropdowns (General Dept -> Dept -> Branch). (Dropdown phân cấp)

✅ Reporting: Export to Excel/PDF, Printing capability. (Xuất báo cáo Excel/PDF, In ấn)

✅ Tracking: Vacation, Overtime, Health, and Sanctions management. (Theo dõi nghỉ phép, tăng ca, sức khỏe, kỷ luật)

⚖️ Credits & Acknowledgements / Nguồn tham khảo

This project is created for educational purposes, following the tutorial by Netcode-Hub.

Dự án này được thực hiện cho mục đích học tập, dựa theo hướng dẫn của Netcode-Hub.

🎥 Tutorial: Build a Complete Employee Management System

👨‍💻 Author: Frederick (Netcode-Hub)

🔗 Channel: Netcode-Hub YouTube

Please support the original author by watching the video and subscribing to their channel. > Vui lòng ủng hộ tác giả gốc bằng cách xem video và đăng ký kênh của họ.

### 💡 Tại sao file này chuyên nghiệp?

1.  **Sử dụng Mermaid Diagram:** GitHub hỗ trợ render biểu đồ `mermaid` trực tiếp. Khi bạn push file này lên GitHub, phần code `graph TD...` sẽ tự động biến thành một biểu đồ hình khối (Flowchart) rất đẹp mắt minh họa luồng đi của dữ liệu.
2.  **Cây thư mục (ASCII Tree):** Giúp người xem hình dung ngay quy mô dự án mà không cần bấm vào từng folder.
3.  **Bảng (Table) so sánh:** Cung cấp cái nhìn song ngữ rõ ràng, người Việt đọc hiểu ngay, người nước ngoài cũng hiểu logic.
4.  **Badges (Huy hiệu):** Các icon `.NET 8`, `License` ở đầu file tạo cảm giác dự án được bảo trì tốt và hiện đại.
