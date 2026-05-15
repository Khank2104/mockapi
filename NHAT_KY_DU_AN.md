# Nhật Ký Dự Án: QuanTro (Hệ Thống Quản Lý Trọ)

QuanTro là giải pháp quản lý trọ toàn diện dành cho các chủ trọ quy mô vừa và nhỏ. Hệ thống cung cấp luồng công việc xuyên suốt từ thiết lập cơ sở hạ tầng, tiếp nhận khách thuê, đến tự động hóa thanh toán và xử lý yêu cầu hỗ trợ theo thời gian thực.

---

## 1. Cấu Trúc Cơ Sở Dữ Liệu Chi Tiết

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

### Session 36: Automated Operations (Email & Contracts)

- **Email Notifications**: Tích hợp `IEmailService` vào `InvoiceService` để tự động gửi thông báo hóa đơn mới cho người thuê thông qua địa chỉ Email đã đăng ký.
- **Background Worker**: Xây dựng `InvoiceReminderWorker` (chạy ngầm theo giờ) quét cơ sở dữ liệu để tìm các hóa đơn quá hạn và tự động gửi Email nhắc nợ. Cơ chế chống spam được tích hợp bằng cách kiểm tra bảng `Notifications` để đảm bảo mỗi người dùng chỉ nhận tối đa 1 email nhắc nhở mỗi ngày.
- **Printable Contract**: Phát triển API `GetContractForPrintAsync` trong `ContractService` lấy toàn bộ thông tin chi tiết của hợp đồng. Tạo trang `PrintContract.cshtml` thiết kế theo chuẩn khổ giấy A4, hỗ trợ in ấn trực tiếp từ trình duyệt (`window.print()`).
- **Admin UI Update**: Bổ sung nút "In Hợp Đồng" vào từng Card Hợp Đồng trên giao diện Dashboard, giúp Admin dễ dàng tạo bản cứng có chữ ký cho quy trình lưu trữ.

### Session 17-20: Tối ưu hóa UI & Logic Hợp đồng

- **Form ghi chỉ số kép**: Gộp ghi điện và nước vào một modal duy nhất để tối ưu thao tác admin.
- **Xử lý vòng lặp Redirect**: Giải quyết xung đột giữa localStorage và JWT Cookies.
- **Cơ chế Soft Termination**: Thay đổi logic chấm dứt hợp đồng để bảo toàn lịch sử tài chính thay vì xóa vĩnh viễn.

### Session 21: Hoàn thiện Sơ đồ phòng & Khắc phục lỗi Runtime

- **Hỗ trợ khu trọ không chia tầng**: Cập nhật Backend và Frontend để hiển thị danh sách phòng trực tiếp.
- **Phòng chống lỗi classList of null**: Thực hiện Audit JS, thêm các kiểm tra an toàn (null check) cho toàn bộ thao tác DOM.

### Session 22: Tối ưu hóa Asset & Refactor Javascript

- **JS Refactoring**: Tách toàn bộ Javascript inline từ `_AdminLayout.cshtml` và `_TenantLayout.cshtml` ra các file riêng biệt (`admin-ui.js`, `admin-notifications.js`, `tenant-ui.js`, `tenant-notifications.js`) để tăng tốc độ tải trang và khả năng bảo trì.
- **Localization**: Chuyển đổi các thư viện CDN (Bootstrap Icons, Chart.js, SignalR) sang lưu trữ local trong `wwwroot/lib`, giúp hệ thống hoạt động ổn định không phụ thuộc internet quốc tế.
- **Dark Mode Hardening**: Rà soát và khắc phục triệt để các lỗi hiển thị (hidden text) trong chế độ Dark Mode tại module Sơ đồ phòng, Tổng quan và các Modal ghi chỉ số điện nước.
- **Error Handling**: Chuẩn hóa cơ chế bắt lỗi và hiển thị thông báo (Toast) trên toàn bộ hệ thống Admin và Tenant Portal.

- **Xử lý phiên làm việc**: Tự động chuyển hướng về trang Login khi API trả về lỗi 401/403.

### [2026-05-05] - Session 22: Tự động hóa quản lý Hợp đồng (Background Task)
- **Cơ chế 7 ngày chờ**: Hợp đồng hết hạn sẽ chuyển sang trạng thái "Waiting" trong 7 ngày trước khi đóng vĩnh viễn, đảm bảo không mất dữ liệu hóa đơn cuối cùng.

### [2026-05-05] - Session 23: Nâng cấp Hóa đơn & Xuất Excel
- **Logic tiền trọ linh hoạt (Prorated)**: Tự động tính 0%/50%/100% tiền phòng dựa trên ngày vào ở thực tế trong tháng.
- **Xuất Excel**: Tích hợp `ClosedXML` để tải hóa đơn .xlsx chuyên nghiệp.

### [2026-05-05] - Session 24-25: Tái cấu trúc & Quản lý người ở
- **Modularization**: Chia nhỏ `Index.cshtml` và các JS module để dễ bảo trì.
- **Thêm người ở chung**: Xây dựng modal `addOccupantModal` và logic tính phụ thu (Extra Occupant Fee).

### [2026-05-07] - Session 26: Tái cấu trúc Tenant Portal & Thông báo Toàn cục
- **Module hóa Portal**: Tách trang Hồ sơ cá nhân thành 3 phần độc lập và xây dựng 3 trang chức năng mới (Phòng ở, Hóa đơn, Hỗ trợ).
- **Hệ thống Toast**: Triển khai `showPremiumToast` cho phản hồi thời gian thực trên toàn ứng dụng.
- **Fix Build**: Đồng bộ hóa tên trường dữ liệu (`DefaultPrice`, `UnitPrice`) và xử lý cảnh báo Null Reference.

### [2026-05-07] - Session 27: Hoàn thiện Dashboard Tenant & Xử lý lỗi Logic
- **Tenant Dashboard UX**: Chuyển đổi trang chủ Tenant thành bảng điều khiển hiện đại với SideBar và Widget tóm tắt dữ liệu.
- **Fix Logic API**: Sửa lỗi sai Route và lỗi phân quyền (403) khi khách thuê xem chi tiết hóa đơn.
- **Hardening**: Nâng cấp cơ chế lấy ID người dùng từ Token và xóa bỏ hoàn toàn các cảnh báo Build (Null Reference).

### [2026-05-08] - Session 28: Đánh giá chuyên sâu Hệ thống Bảo mật JWT (Security Audit)
- **Phân tích hiện trạng**: Đánh giá mức độ áp dụng JWT trong toàn bộ project (API, MVC, SignalR).
- **Kết quả**: JWT đã được triển khai chuyên nghiệp thông qua HttpOnly Cookies, kết hợp với Policy-based Authorization (`SuperuserOnly`, `TenantOnly`).
- **Lộ trình nâng cấp**: Xác định các điểm cần hoàn thiện sau khi dự án chạy ổn định bao gồm: Refresh Token, cơ chế thu hồi Token (Blacklist), và bảo mật chống CSRF.

### [2026-05-09] - Session 29: QR Payment Integration & Stabilization
- **Cổng thanh toán QR**: Tích hợp luồng tạo mã VietQR động và upload minh chứng thanh toán an toàn.
- **Đồng bộ hóa dữ liệu**: Chuẩn hóa chuẩn API camelCase và khắc phục triệt để các lỗi 404/Undefined trong module Hợp đồng và Hóa đơn.
- **Thông báo cá nhân hóa**: Bổ sung chuông thông báo và danh sách thông báo cho cả Admin và Tenant, bảo mật dựa trên định danh JWT.
- **Tinh chỉnh UX**: Triển khai cơ chế chuyển đổi trạng thái tự động (Pending) và ẩn hiện nút thao tác thông minh theo vòng đời hóa đơn.

### [2026-05-11] - Session 30: Đồng bộ hóa Giao diện Dashboard Admin & Sửa lỗi Layout
- **Module Hợp đồng — Cải tạo HTML Layout**: Thay thế layout `<table>` cũ bằng Bootstrap `row g-4` card grid, khớp với pattern đã được thiết lập.
- **Sửa lỗi gốc rễ**: Sửa Null Reference crash trong `contracts.js`. Áp dụng kiểm tra null-safety.
- **Tái cấu trúc**: Sửa hàm `renderGlobalMotelSelector` nhắm mục tiêu vào các list container riêng của từng module.
- **Đồng bộ Sơ đồ phòng**: Tự động lấy `window._globalSelectedMotelId` để tải lưới phòng ngay lập tức.
- **Pre-loading Cache**: Tải danh sách khu trọ trước khi chuyển trang để ngăn ngừa hiển thị dữ liệu trống.

### [2026-05-12] - Session 31: Hoàn thiện CRUD & Tối ưu hóa UI Thiết lập Khu trọ (Motel Setup)
- **CRUD Toàn diện Khu trọ**: Triển khai đầy đủ các API và giao diện Thêm, Sửa, Xóa cho Khu trọ, Tầng và Phòng trực tiếp từ Frontend. Đồng bộ hóa thay đổi tự động (DOM) mà không cần tải lại trang.
- **Quy tắc Xóa An toàn (Safe Deletion)**: Áp dụng cơ chế kiểm tra backend; chặn thao tác xóa Tầng/Phòng nếu có phòng đang có người ở (Occupied) nhằm bảo vệ tính toàn vẹn của dữ liệu hợp đồng.
- **Trải nghiệm Thẻ (Tabs UX)**: Cải tổ hoàn toàn giao diện Thiết lập Khu trọ (`_MotelSetupModule.cshtml`). Tái cấu trúc form Khai báo Tầng và Khai báo Phòng thành bố cục Thẻ (Tabs), giúp tối ưu không gian hiển thị và tránh tình trạng cuộn chuột liên tục.
- **Phân trang & Lọc (Pagination & Filters)**: Xây dựng thuật toán Javascript phân trang danh sách phòng (10 dòng/trang), kèm thanh tìm kiếm mã phòng và bộ lọc theo Tầng.
- **Tạo Phòng Hàng Loạt (Bulk Add)**: Bổ sung tính năng tạo hàng loạt tự động sinh mã phòng liên tiếp (VD: 101, 102, 103...), giảm thiểu đáng kể thời gian nhập liệu ban đầu cho các khu trọ lớn.
- **Đồng bộ hóa Trải nghiệm**: Gỡ bỏ nút "Sửa HĐ" ở Sơ đồ phòng để định tuyến mọi luồng xử lý hợp đồng về khu vực Quản lý Hợp đồng riêng biệt. Tinh chỉnh CSS để giao diện Tab hiển thị sắc nét trên nền Glassmorphism.

### [2026-05-12] - Session 32: Tối ưu hóa Luồng Nghiệp vụ & Xử lý Xung đột Logic
- **Thanh lý Hợp đồng & Lưu trữ**: Cập nhật logic chấm dứt hợp đồng. Hệ thống sẽ xóa tài khoản đăng nhập (User) của khách thuê để chặn truy cập, nhưng giữ lại toàn bộ hồ sơ (Tenant), Hợp đồng và Hóa đơn trong DB để đối soát (Archive).
- **Hủy & Sửa Hóa đơn**: Bổ sung tính năng "Hủy hóa đơn" đối với các hóa đơn chưa thanh toán. Điều này cho phép Admin xóa hóa đơn sai, sửa lại chỉ số điện nước và phát hành lại hóa đơn mới.
- **Xóa Chỉ số sai**: Cho phép xóa chỉ số điện/nước nếu hóa đơn kỳ đó chưa được phát hành, giúp sửa lỗi nhập liệu nhanh chóng.
- **Duyệt Thanh toán một phần**: Cập nhật quy trình duyệt QR Payment. Admin có thể nhập "Số tiền thực nhận" từ ảnh minh chứng. Hệ thống sẽ ghi nhận đúng số tiền đó và chuyển trạng thái hóa đơn sang "Thanh toán một phần" (Partially Paid) nếu chưa đủ, thay vì mặc định chốt 100% như trước.

### [2026-05-15] - Session 33: Đồng bộ Hồ sơ Cá nhân & Tái thiết kế Giao diện Premium
- **Sửa lỗi mất Số điện thoại**: Khắc phục triệt để lỗi mất SĐT khi cập nhật hồ sơ nhờ việc bổ sung trường nhập liệu vào tab Identity và đồng bộ hóa payload.
- **Đồng bộ 2 chiều (Bi-directional Sync)**: Nâng cấp `UserService` và `TenantService` để tự động cập nhật thông tin chéo giữa bảng `Users` và `Tenants`, đảm bảo dữ liệu luôn đồng nhất.
- **Tái thiết kế Header**: Đại tu giao diện Header trên cả Admin và Tenant Dashboard. Áp dụng Glassmorphism, sửa lỗi căn lề và thêm Badge Profile cao cấp.
- **Hệ thống Thông báo (Toasts) & Loading**: Tích hợp `showPremiumToast` và trạng thái `Loading` cho các nút bấm, giúp người dùng nhận biết kết quả thao tác ngay lập tức.
- **Bảo mật dữ liệu (Null-safety)**: Cập nhật cơ chế cập nhật từng phần (Partial Update) tại Backend, ngăn chặn việc ghi đè dữ liệu cũ bằng giá trị rỗng/null.

### [2026-05-15] - Session 34: Bảo mật Hệ thống JWT (Refresh Token & Revocation)
- **Refresh Token Rotation**: Triển khai cơ chế sinh Refresh Token 64-byte an toàn, lưu trữ Database.
- **Bảo mật CSRF**: Áp dụng Cookie `SameSite=Strict` cho Refresh Token và cấu hình Path riêng biệt.
- **Thu hồi Token (Revocation)**: Tích hợp logic thu hồi token (đặt null trong DB) khi người dùng chủ động Logout.
- **Nâng cấp thời gian sống (Lifespan)**: Giảm hạn sử dụng Access Token từ 6 tiếng xuống 15 phút để giảm thiểu rủi ro bị đánh cắp.
- **Real-time Status Validation (Token Delay Fix)**: Cập nhật `OnTokenValidated` trong Program.cs để truy vấn trạng thái User ngay lập tức. Nếu Admin khóa tài khoản, thẻ JWT của khách thuê sẽ bị từ chối (Fail) tức thì thay vì phải chờ hết hạn.

### [2026-05-15] - Session 35: Biểu đồ Thống kê Doanh thu (Revenue Chart)
- **Backend API**: Tạo mới API `/api/Invoice/RevenueChart` trong `InvoiceService` truy xuất dữ liệu 6 tháng gần nhất, tự động tính toán tổng hóa đơn phát hành (Dự kiến thu) và số tiền đã đóng (Thực thu).
- **Frontend Dashboard**: Tích hợp `Chart.js` dạng Bar Chart, thiết kế cột kéo song song (Expected vs Collected). Đặt canvas biểu đồ trực tiếp vào trang Tổng quan Admin để phân tích xu hướng thu chi một cách sinh động, giao diện Glassmorphism trong suốt, thân thiện.

---

## 3. Trạng Thái Dự Án Hiện Tại

**Ngày cập nhật**: 2026-05-15
**Thực hiện bởi**: Antigravity (AI)
**Trạng thái**: ✅ **Ổn định** — Hoàn thiện trải nghiệm người dùng (UX) và cơ chế đồng bộ dữ liệu hồ sơ.

**Tính năng ĐÃ HOÀN THÀNH:**
- [x] Xác thực & Phân quyền (JWT Cookie, Role-based)
- [x] Quản lý Cơ sở hạ tầng (Khu trọ, Tầng, Phòng)
- [x] Sơ đồ phòng trực quan (Real-time Sync)
- [x] Wizard ký hợp đồng 3 bước
- [x] Ghi chỉ số Điện/Nước thông minh
- [x] Tính tiền & Xuất hóa đơn Excel
- [x] Tenant Portal (Hồ sơ, Phòng ở, Hóa đơn, Yêu cầu hỗ trợ)
- [x] Hệ thống thông báo SignalR & Toast phản hồi nhanh
- [x] Cổng thanh toán QR (VietQR + Upload minh chứng)
- [x] Giao diện Card Grid đồng nhất toàn bộ Dashboard Admin
- [x] Tối ưu hóa UI Thiết lập Khu trọ (Tabbed Layout, Pagination, Bulk Add)
- [x] Đồng bộ dữ liệu Hồ sơ cá nhân & Redesign Header Premium
- [x] Trạng thái Loading & Thông báo tương tác thời gian thực

- [x] Cơ chế Refresh Token & Thu hồi JWT
- [x] Biểu đồ phân tích doanh thu nâng cao

**Cần làm tiếp:**
- [ ] Tích hợp cổng thanh toán trực tuyến (VNPay, Momo) - Đã có VietQR, tạm gác lại.
- [ ] Ứng dụng Mobile (Flutter/React Native)

---

### Hoàn tất Tái cấu trúc (Refactoring)
- **Tách Module JS & DTO**: Áp dụng triệt để DRY Principle.
- **Audit Database Constraints**: Hoàn thiện ràng buộc dữ liệu.

*Tài liệu này sẽ liên tục được cập nhật theo tiến trình của dự án.*

---

## 5. Tóm tắt vai trò của Antigravity (AI) trong dự án

Với tư cách là một Trợ lý AI lập trình (Agentic AI), Antigravity đã đóng vai trò cốt lõi trong việc thiết kế, phát triển và tối ưu hóa hệ thống QuanTro từ những dòng code đầu tiên. Dưới đây là tóm tắt chi tiết những đóng góp chính:

### 🚀 Những Thành Tựu & Tính Năng Đã Xây Dựng
- **Hệ thống Quản lý Toàn diện**: Hoàn thiện luồng nghiệp vụ quản lý Khu trọ, Tầng, Phòng, Khách thuê và Dịch vụ với các ràng buộc dữ liệu chặt chẽ.
- **Tự Động Hóa Nghiệp Vụ**: Xây dựng thuật toán tính toán hóa đơn phức tạp (chia tỷ lệ tiền phòng theo ngày ở thực tế), tự động quản lý vòng đời hợp đồng và chuyển đổi trạng thái phòng.
- **Tenant Portal & Admin Dashboard**: Phát triển thành công cổng thông tin cho cả Người quản lý (Admin) và Khách thuê (Tenant) với khả năng thao tác độc lập, an toàn.
- **Thanh toán VietQR**: Tích hợp luồng tạo mã QR động và tải lên minh chứng thanh toán, tự động hóa quy trình đối soát.
- **Tối ưu hóa Luồng Nghiệp vụ**: Giải quyết các xung đột logic giữa hóa đơn, chỉ số và thanh toán, giúp hệ thống vận hành linh hoạt và thực tế hơn.

### 🛠 Công Nghệ Đã Sử Dụng
- **Backend**: C# .NET 8 (ASP.NET Core MVC & Web API), Entity Framework Core (EF Core) cho ORM, LINQ.
- **Cơ sở dữ liệu**: SQL Server với kiến trúc chuẩn hóa cao.
- **Frontend**: Vanilla Javascript (tách module), HTML5, CSS3, Bootstrap 5.
- **Bảo mật**: JSON Web Tokens (JWT) lưu trữ qua HttpOnly Cookies, BCrypt (mã hóa mật khẩu), Role-Based Access Control (RBAC).
- **Tiện ích khác**: SignalR (WebSockets cho Real-time Notifications), Serilog (Ghi log), ClosedXML (Xuất file Excel).

### ⚡ Các Biện Pháp Tối Ưu Hóa (Optimization)
- **Tối ưu Cấu trúc Mã (Refactoring)**:
  - Chia nhỏ một file `Index.cshtml` khổng lồ thành hơn 15 Partial Views riêng biệt để dễ bảo trì.
  - Tách các hàm JS nội tuyến (inline) thành 10 module Javascript riêng biệt theo chuẩn SoC (Separation of Concerns).
  - Phân rã `MotelEntities.cs` thành 15 model độc lập, áp dụng triệt để pattern DTO để tránh tham chiếu vòng (Circular Reference) và bảo mật dữ liệu nhạy cảm.
- **Tối ưu Giao diện (UX/UI)**: 
  - Áp dụng triết lý thiết kế **Glassmorphism** kết hợp với **Card Grid Layout** để tạo cảm giác hiện đại, sang trọng (Premium).
  - Tối ưu không gian hiển thị bằng cách dùng dạng Thẻ (Tabs), Modal và tự động cập nhật DOM (Real-time sync) không cần tải lại trang.
- **Tối ưu Hiệu năng & An toàn Hệ thống**:
  - Áp dụng các ràng buộc Unique Index ở tầng Database để chống trùng lặp dữ liệu (ví dụ: cấm tạo 2 phòng cùng mã trong 1 khu trọ).
  - Xây dựng hệ thống `ContractExpirationService` chạy ngầm (Background Worker) giúp dọn dẹp và cập nhật trạng thái hợp đồng một cách tự động, không gây nghẽn luồng chính.
  - Bổ sung phòng thủ Null-safety toàn diện trên Javascript để ngăn ngừa lỗi crash ứng dụng âm thầm.

---

## 6. Walkthrough: Quy Trình Nghiệp Vụ Chính

Dưới đây là tóm tắt các luồng xử lý chính trong hệ thống QuanTro:

### A. Thiết lập Cơ sở hạ tầng
1. **Khai báo Khu trọ**: Admin nhập thông tin tên, địa chỉ khu trọ.
2. **Khai báo Tầng & Phòng**: Sử dụng tính năng "Thêm hàng loạt" để nhanh chóng tạo sơ đồ phòng.
3. **Cấu hình Dịch vụ**: Chốt giá điện, nước, wifi... cho từng phòng.

### B. Quản lý Khách thuê & Hợp đồng
1. **Tiếp nhận khách**: Đăng ký tài khoản cho khách thuê.
2. **Ký hợp đồng**: Sử dụng Wizard 3 bước để chọn phòng, dịch vụ và chốt ngày vào ở. Hệ thống tự động tính tiền cọc và tiền phòng tháng đầu (prorated).
3. **Quản lý người ở**: Thêm các thành viên cùng phòng nếu cần.

### C. Vận hành hàng tháng
1. **Ghi chỉ số**: Ghi số điện/nước vào cuối tháng thông qua Form ghi kép.
2. **Phát hành hóa đơn**: Hệ thống tự động tổng hợp tiền phòng + tiền dịch vụ + phụ thu để tạo hóa đơn.
3. **Thanh toán**: Khách thuê quét mã VietQR và gửi minh chứng. Admin kiểm tra và duyệt thanh toán.

### D. Chấm dứt & Lưu trữ
1. **Thanh lý hợp đồng**: Khi khách dời đi, hệ thống giải phóng phòng và khóa tài khoản khách nhưng vẫn giữ lại lịch sử hóa đơn để đối soát.

