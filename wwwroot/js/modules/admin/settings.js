const API_PROXY_URL = '/api/UserProxy';

// Lấy thông tin user hiện tại từ localStorage
function getCurrentUser() {
    const json = localStorage.getItem('currentUser');
    return json ? JSON.parse(json) : null;
}

// Load trạng thái OTP hiện tại của user
async function loadOtpStatus() {
    const user = getCurrentUser();
    if (!user) return;

    try {
        const response = await fetch(`${API_PROXY_URL}/GetSettings`);
        const result = await response.json();
        if (result.success) {
            const toggle = document.getElementById('otpToggle');
            toggle.checked = result.data.otpEnabled;
            updateOtpLabel(result.data.otpEnabled);
        }
    } catch (err) {
        console.error('Lỗi tải cài đặt:', err);
    }
}

// Cập nhật nhãn trạng thái OTP
function updateOtpLabel(isEnabled) {
    const msg = document.getElementById('otpStatusMsg');
    if (isEnabled) {
        msg.innerHTML = '<i class="bi bi-shield-check text-success me-1"></i><span class="text-success">OTP đang <b>BẬT</b> — Tài khoản được bảo vệ bằng xác thực 2 lớp.</span>';
    } else {
        msg.innerHTML = '<i class="bi bi-shield-x text-warning me-1"></i><span class="text-warning">OTP đang <b>TẮT</b> — Đăng nhập không cần xác thực OTP.</span>';
    }
    msg.style.display = 'block';
}

// Xử lý toggle OTP
document.getElementById('otpToggle')?.addEventListener('change', async function () {
    const user = getCurrentUser();
    if (!user) return;

    const isEnabled = this.checked;
    this.disabled = true;

    try {
        const response = await fetch(`${API_PROXY_URL}/ToggleOTP`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ otpEnabled: isEnabled })
        });

        const result = await response.json();

        if (result.success) {
            updateOtpLabel(isEnabled);
        } else {
            // Hoàn tác toggle nếu lỗi
            this.checked = !isEnabled;
            alert(result.message || 'Có lỗi xảy ra khi cập nhật cài đặt.');
        }
    } catch (err) {
        this.checked = !isEnabled;
        alert('Lỗi kết nối tới server.');
    } finally {
        this.disabled = false;
    }
});

// Khởi tạo trang
document.addEventListener('DOMContentLoaded', loadOtpStatus);
