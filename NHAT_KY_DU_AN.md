# Nhật Ký Dự Án: QuanTro (Hệ Thống Quản Lý Trọ)

QuanTro là giải pháp quản lý trọ toàn diện dành cho các chủ trọ quy mô vừa và nhỏ. Hệ thống cung cấp luồng công việc xuyên suốt từ thiết lập cơ sở hạ tầng, tiếp nhận khách thuê, đến tự động hóa thanh toán và xử lý yêu cầu hỗ trợ theo thời gian thực.

---

## 1. Cấu Trúc Cơ Sở Dữ Liệu Chi Tiết (Cập nhật 2026-05-07)

Hệ thống sử dụng thiết kế SQL Server CHUẨN HÓA cao để đảm bảo hiệu năng và tính toàn vẹn dữ liệu.

| Nhóm Chức Năng | Tên Bảng | Mô tả & Các trường quan trọng |
| :--- | :--- | :--- |
| **User & Auth** | `Roles` | Phân quyền truy cập (Superuser, Admin, Tenant). |
| | `Users` | Tài khoản, mật khẩu (BCrypt), Trạng thái, Ảnh đại diện. |
| **Hạ Tầng** | `Motels` | Thông tin khu trọ, địa chỉ, chủ sở hữu. |
| | `Floors` | Quản lý tầng trong từng khu trọ. |
| | `Rooms` | Thông tin phòng, mã phòng, trạng thái (Trống/Đã thuê). |
| **Giá & Dịch Vụ** | `RoomSettings` | Giá thuê, Tiền cọc, Số người tiêu chuẩn, Phụ thu. |
| | `Services` | Danh mục dịch vụ hệ thống (Điện, Nước, Wifi...). |
| | `RoomServiceSettings`| Cấu hình dịch vụ RIÊNG cho từng phòng (Bật/Tắt, Giá tùy chỉnh). |
| **Khách & Hợp Đồng** | `Tenants` | Hồ sơ chi tiết (CCCD, Địa chỉ thường trú, Ngày sinh, Giới tính). |
| | `RoomOccupants` | Danh sách người đang ở thực tế (Chủ phòng / Thành viên). |
| | `Contracts` | Hợp đồng pháp lý, ngày hiệu lực, giá chốt khi ký. |
| **Tài Chính** | `MeterReadings` | Chỉ số điện/nước (Số cũ, Số mới, Ngày ghi). |
| | `Invoices` | Hóa đơn tổng (Tiền phòng + Tiền dịch vụ). |
| | `InvoiceDetails` | Chi tiết từng khoản thu trong hóa đơn. |
| | `Payments` | Lịch sử giao dịch thanh toán. |
| **Tương Tác** | `Requests` | Yêu cầu sửa chữa, khiếu nại của Tenant. |
| | `Notifications` | Thông báo hệ thống và thông báo thời gian thực (SignalR). |

---

## 2. Lịch Sử Phát Triển & Các Cột Mốc

### Giai đoạn 1-13: Hạ tầng cốt lõi, API & Dashboard UX
- Triển khai toàn bộ Schema CSDL bằng EF Core.
- Xây dựng logic tính toán hóa đơn tự động và bảo mật RBAC (JWT, BCrypt, HttpOnly Cookies).
- Phát triển giao diện Dashboard theo phong cách Glassmorphism cao cấp.
- Xử lý triệt để lỗi tham chiếu vòng (circular reference) bằng DTO patterns.
- Tích hợp Serilog và Swagger/OpenAPI.

### Session 14-16: Luồng Nghiệp Vụ & Sơ Đồ Phòng
- **Sơ đồ phòng (Floor Map)**: Thiết kế lại module quản lý theo dạng lưới trực quan, đồng bộ trạng thái phòng theo thời gian thực.
- **Quản lý vòng đời hợp đồng**: Thêm quy trình chấm dứt hợp đồng thủ công và tự động giải phóng phòng.
- **Background Worker**: Triển khai `ContractExpirationService` chạy định kỳ hàng giờ để xử lý hợp đồng hết hạn.

### Session 17-20: Tối ưu hóa UI & Logic Hợp đồng
- **Form ghi chỉ số kép**: Gộp ghi điện và nước vào một modal duy nhất để tối ưu thao tác admin.
- **Xử lý vòng lặp Redirect**: Giải quyết xung đột giữa localStorage và JWT Cookies.
- **Cơ chế Soft Termination**: Thay đổi logic chấm dứt hợp đồng để bảo toàn lịch sử tài chính thay vì xóa vĩnh viễn.

### Session 21: Hoàn thiện Sơ đồ phòng & Khắc phục lỗi Runtime
- **Hỗ trợ khu trọ không chia tầng**: Cập nhật Backend và Frontend để hiển thị danh sách phòng trực tiếp.
- **Phòng chống lỗi classList of null**: Thực hiện Audit JS, thêm các kiểm tra an toàn (null check) cho toàn bộ thao tác DOM.
- **Xử lý phiên làm việc**: Tự động chuyển hướng về trang Login khi API trả về lỗi 401/403.

### [2026-05-05] - Session 22: Tự động hóa quản lý Hợp đồng (Background Task)
- **Cơ chế 7 ngày chờ**: Hợp đồng hết hạn sẽ chuyển sang trạng thái "Waiting" trong 7 ngày trước khi đóng vĩnh viễn, đảm bảo không mất dữ liệu hóa đơn cuối cùng.

### [2026-05-05] - Session 23: Nâng cấp Hóa đơn & Xuất Excel
- **Logic tiền trọ linh hoạt (Prorated)**: Tự động tính 0%/50%/100% tiền phòng dựa trên ngày vào ở thực tế trong tháng.
- **Xuất Excel**: Tích hợp `ClosedXML` để tải hóa đơn .xlsx chuyên nghiệp.

### [2026-05-05] - Session 24-25: Tái cấu trúc & Quản lý người ở
- **Modularization**: Chia nhỏ `Index.cshtml` và các JS module để dễ bảo trì.
- **Thêm người ở chung**: Xây dựng modal `addOccupantModal` và logic tính phụ thu (Extra Occupant Fee).

### [2026-05-07] - Session 26: Tái cấu trúc Tenant Portal & Thông báo Toàn cục (Mới nhất)
- **Module hóa Portal**: Tách trang Hồ sơ cá nhân thành 3 phần độc lập và xây dựng 3 trang chức năng mới (Phòng ở, Hóa đơn, Hỗ trợ).
- **Hệ thống Toast**: Triển khai `showPremiumToast` cho phản hồi thời gian thực trên toàn ứng dụng.
- **Fix Build**: Đồng bộ hóa tên trường dữ liệu (`DefaultPrice`, `UnitPrice`) và xử lý cảnh báo Null Reference.

### [2026-05-07] - Session 27: Hoàn thiện Dashboard Tenant & Xử lý lỗi Logic
- **Tenant Dashboard UX**: Chuyển đổi trang chủ Tenant thành bảng điều khiển hiện đại với SideBar và Widget tóm tắt dữ liệu.
- **Fix Logic API**: Sửa lỗi sai Route và lỗi phân quyền (403) khi khách thuê xem chi tiết hóa đơn.
- **Hardening**: Nâng cấp cơ chế lấy ID người dùng từ Token và xóa bỏ hoàn toàn các cảnh báo Build (Null Reference).

### [2026-05-08] - Session 28: Đánh giá chuyên sâu Hệ thống Bảo mật JWT (Security Audit)
**Tiếng Việt:**
- **Phân tích hiện trạng**: Đánh giá mức độ áp dụng JWT trong toàn bộ project (API, MVC, SignalR).
- **Kết quả**: JWT đã được triển khai chuyên nghiệp thông qua HttpOnly Cookies, kết hợp với Policy-based Authorization (`SuperuserOnly`, `TenantOnly`).
- **Lộ trình nâng cấp**: Xác định các điểm cần hoàn thiện sau khi dự án chạy ổn định bao gồm: Refresh Token, cơ chế thu hồi Token (Blacklist), và bảo mật chống CSRF.

**English:**
- **Current State Analysis**: Audited JWT implementation across API, MVC, and SignalR components.
- **Key Findings**: JWT is successfully implemented using HttpOnly Cookies combined with robust Policy-based Authorization.
- **Security Roadmap**: Identified post-release security enhancements: Refresh Token logic, Token Revocation (Blacklisting), and CSRF protection measures.

### [2026-05-09] - Session 29: QR Payment Integration & Stabilization
**Vietnamese:**
- **Cổng thanh toán QR**: Tích hợp luồng tạo mã VietQR động và upload minh chứng thanh toán an toàn.
- **Đồng bộ hóa dữ liệu**: Chuẩn hóa chuẩn API camelCase và khắc phục triệt để các lỗi 404/Undefined trong module Hợp đồng và Hóa đơn.
- **Thông báo cá nhân hóa**: Bổ sung chuông thông báo và danh sách thông báo cho cả Admin và Tenant, bảo mật dựa trên định danh JWT.
- **Tinh chỉnh UX**: Triển khai cơ chế chuyển đổi trạng thái tự động (Pending) và ẩn hiện nút thao tác thông minh theo vòng đời hóa đơn.

**English:**
- **QR Payment Portal**: Integrated VietQR dynamic generation and secure payment proof upload flow.
- **Data Synchronization**: Enforced camelCase API standards and fixed 404/Undefined errors across Contract and Billing modules.
- **Personalized Notifications**: Added notification bell and list to both Admin and Tenant layouts, secured by JWT identity.
- **UX Refinements**: Implemented automatic state transitions (Pending status) and refined action button visibility based on invoice lifecycle.

---

## 3. Trạng Thái Dự Án Hiện Tại

**Ngày cập nhật**: 2026-05-09
**Thực hiện bởi**: Antigravity (AI)
**Trạng thái**: ✅ **Hoàn thiện Logic & Audit Bảo mật** — Hệ thống đã sẵn sàng cho giai đoạn tối ưu hóa chuyên sâu.

**Tính năng ĐÃ HOÀN THÀNH:**
- [x] Xác thực & Phân quyền (JWT Cookie, Role-based)
* [x] Quản lý Cơ sở hạ tầng (Khu trọ, Tầng, Phòng)
* [x] Sơ đồ phòng trực quan (Real-time Sync)
* [x] Wizard ký hợp đồng 3 bước
* [x] Ghi chỉ số Điện/Nước thông minh
* [x] Tính tiền & Xuất hóa đơn Excel
* [x] Tenant Portal (Hồ sơ, Phòng ở, Hóa đơn, Yêu cầu hỗ trợ)
* [x] Hệ thống thông báo SignalR & Toast phản hồi nhanh

---

## 4. Nhật Ký Sửa Lỗi & Tái Cấu Trúc (Refactoring)

*(Chi tiết các bước Refactor từ DRY Principle, Tách Module JS, Tách DTO, đến Audit Database Constraints đã được thực hiện và lưu trữ trong lịch sử phiên bản)*

*Tài liệu này sẽ liên tục được cập nhật theo tiến trình của dự án.*

---

## 5. Tóm tắt vai trò của Antigravity (AI) trong dự án / Summary of Antigravity's Role

**Vietnamese:**
- **Kiến trúc cốt lõi**: Thiết kế và triển khai mô hình DTO kết hợp với Kiến trúc hướng dịch vụ (SOA), đảm bảo hệ thống có thể mở rộng và bảo mật tốt từ phía Backend.
- **Trải nghiệm Người dùng (UX/UI)**: Xây dựng toàn bộ giao diện Dashboard phong cách Glassmorphism và Cổng thông tin Tenant từ con số 0, ưu tiên tính thẩm mỹ và sự tiện dụng.
- **Kỹ thuật Bảo mật**: Thực hiện Audit và thắt chặt hệ thống xác thực JWT, đảm bảo nguyên tắc Zero-trust và phân quyền Role-based mạnh mẽ.
- **Đồng bộ thời gian thực**: Tích hợp SignalR để cập nhật tức thì mọi thay đổi trong hệ thống, từ thông báo hết hạn hợp đồng đến các cập nhật thanh toán.
- **Tự động hóa vận hành**: Tự động hóa các quy trình phức tạp như tính toán hóa đơn, vòng đời hợp đồng và quy trình xác thực thanh toán QR, giúp giảm thiểu tối đa sai sót thủ công.

**English:**
- **Architectural Backbone**: Designed and implemented the core DTO pattern and Service-Oriented Architecture (SOA) to ensure a scalable and secure backend.
- **Premium UX/UI**: Created the modern Glassmorphism dashboard and the intuitive Tenant Portal from scratch, prioritizing aesthetics and usability.
- **Security Engineering**: Audited and hardened the JWT-based authentication system, ensuring zero-trust principles and robust role-based access control.
- **Real-time Synchronization**: Integrated SignalR for instant system-wide updates, from contract expirations to payment notifications.
- **Operational Automation**: Automated the complex billing, contract lifecycle, and QR payment verification workflows to minimize manual administrative overhead.
