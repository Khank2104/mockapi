# Nhật Ký Dự Án: QuanTro (Hệ Thống Quản Lý Trọ)

QuanTro là giải pháp quản lý trọ toàn diện dành cho các chủ trọ quy mô vừa và nhỏ. Hệ thống cung cấp luồng công việc xuyên suốt từ thiết lập cơ sở hạ tầng, tiếp nhận khách thuê, đến tự động hóa thanh toán và xử lý yêu cầu hỗ trợ theo thời gian thực.

---

## 1. Cấu Trúc Cơ Sở Dữ Liệu Chi Tiết

Hệ thống được xây dựng trên nền tảng SQL Server mạnh mẽ với hơn 16 bảng liên kết chặt chẽ.

### 1.1. Quản lý Người Dùng & Phân Quyền
- **Roles**: Phân quyền (superuser, admin, tenant).
- **Users**: Xác thực, bảo mật, thông tin cá nhân cơ bản.

### 1.2. Nhóm Cơ Sở Hạ Tầng
- **Motels**: Thông tin khu trọ.
- **Floors**: Quản lý tầng trong khu trọ.
- **Rooms**: Quản lý phòng trọ (trạng thái, diện tích).

### 1.3. Nhóm Kinh Doanh & Bảng Giá
- **RoomSettings**: Giá thuê cơ bản, tiền cọc, số người tiêu chuẩn, phụ thu.
- **Services**: Danh mục dịch vụ chung (Điện, Nước, Rác...).
- **RoomServiceSettings**: (Đã loại bỏ để đồng bộ giá dịch vụ toàn hệ thống).

### 1.4. Nhóm Khách Thuê & Hợp Đồng
- **Tenants**: Hồ sơ khách thuê (CCCD, Số điện thoại...).
- **RoomOccupants**: Theo dõi người đang lưu trú tại phòng.
- **Contracts**: Hợp đồng thuê nhà có tính pháp lý (Ngày bắt đầu/kết thúc, tiền cọc, giá thuê).

### 1.5. Nhóm Tài Chính & Hóa Đơn
- **MeterReadings**: Ghi nhận chỉ số điện, nước tiêu thụ hàng tháng.
- **Invoices**: Hóa đơn thanh toán hàng tháng (Tổng tiền phòng, dịch vụ, phụ thu).
- **Payments**: Lịch sử thanh toán.

### 1.6. Nhóm Tương Tác
- **Requests**: Yêu cầu hỗ trợ, sửa chữa từ khách thuê.
- **Notifications**: Cảnh báo và thông báo hệ thống (SignalR).

---

## 2. Nhật Ký Cập Nhật Tính Năng Mới Nhất

### Session 14-16: Hoàn Thiện Core Tính Năng & Sơ Đồ Phòng
- **Sơ đồ Phòng & Tầng (Floor Map)**:
  - Thiết kế lại giao diện quản lý phòng thành sơ đồ trực quan.
  - Tự động hiển thị trạng thái phòng theo hợp đồng thực tế (Đang ở/Trống).
  - Tích hợp Panel chi tiết bên phải để xem nhanh giá tiền và dịch vụ bắt buộc.
- **Tính năng Chấm Dứt Hợp Đồng (Hard Delete)**:
  - Khi chấm dứt hợp đồng, hệ thống sẽ tự động dọn dẹp sạch sẽ toàn bộ hóa đơn, người ở, hợp đồng, tài khoản liên quan để giải phóng phòng về trạng thái Trống.
- **Quản Lý Hợp Đồng Hết Hạn Tự Động**:
  - Tạo `ContractExpirationService` chạy ngầm mỗi giờ. Hợp đồng hết hạn sẽ có 7 ngày gia hạn, quá hạn sẽ tự động xóa sạch dữ liệu và trả phòng.
- **Quản Lý Hóa Đơn & Ghi Chỉ Số Điện Nước**:
  - Tích hợp form ghi chỉ số kép (Điện & Nước) vào chung một Modal.
  - Tự động hiển thị chỉ số cũ của tháng trước, admin chỉ cần nhập chỉ số mới.
  - Tính năng phát hành hóa đơn tự động và khách thuê (Tenant) chỉ có thể xem hóa đơn của chính mình trên Portal.

### Session 17: Tối ưu UI/UX Ghi Chỉ Số Điện Nước (Mới nhất)
- **Cải tiến Giao diện Ghi Chỉ Số**: 
  - Khách hàng yêu cầu gộp 2 form ghi chỉ số điện và nước lại với nhau.
  - Cập nhật `recordMeterModal` để hiển thị song song 2 cột Điện và Nước. 
  - Cho phép admin xem số cũ, nhập số mới và lưu cả 2 chỉ số cùng một lúc (sử dụng `Promise.all` gửi 2 request song song), tiết kiệm thao tác tối đa.

---

## 3. Trạng Thái Dự Án Hiện Tại

**Ngày cập nhật**: 2026-05-02
**Thực hiện bởi**: Antigravity (AI)
**Trạng thái**: ✅ Ổn định — Toàn bộ luồng nghiệp vụ cốt lõi đã hoàn thành.

**Tính năng ĐÃ HOÀN THÀNH:**
- [x] Đăng nhập / Đăng xuất (JWT Cookie, Phân quyền)
- [x] Quản lý Khu trọ, Tầng, Phòng
- [x] Sơ đồ Phòng & Tầng (Trực quan, thời gian thực)
- [x] Tạo tài khoản Khách thuê (tách biệt tài khoản và hợp đồng)
- [x] Quản lý Hợp đồng (Wizard 3 bước chuyên nghiệp)
- [x] Cấu hình Dịch vụ (Global Services)
- [x] Ghi chỉ số điện nước kép (Form tối ưu)
- [x] Tính tiền & Phát hành Hóa đơn tự động
- [x] Cổng thông tin Khách thuê (Tenant Portal) xem hóa đơn/yêu cầu
- [x] Tự động dọn dẹp hợp đồng hết hạn/chấm dứt.

**Các tính năng chuẩn bị phát triển (Nếu cần):**
- [ ] Tích hợp thanh toán online (VNPay, MoMo)
- [ ] Báo cáo & Thống kê doanh thu chi tiết
- [ ] Ứng dụng Mobile

*Tài liệu này sẽ liên tục được cập nhật theo tiến trình của dự án.*
