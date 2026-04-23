const API_PROXY_URL = "/api/UserProxy";

document.addEventListener('DOMContentLoaded', () => {
    checkAuth();
});

document.getElementById('loginForm')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;
    const errorDiv = document.getElementById('loginError');

    try {
        const response = await fetch(`${API_PROXY_URL}/Login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });

        const result = await response.json();

        if (result.success) {
            if (result.message === "OTP_REQUIRED") {
                // BẬT OTP: Chuyển sang phần nhập OTP
                document.getElementById('credentialsSection').style.display = 'none';
                document.getElementById('otpSection').style.display = 'block';
                errorDiv.style.display = 'none';
            } else {
                // TẮT OTP: Đăng nhập thẳng
                localStorage.setItem('tokenExpiry', Date.now() + 6 * 60 * 60 * 1000); // 6 tiếng
                localStorage.setItem('currentUser', JSON.stringify(result.data.user));
                const user = result.data.user;
                if (user.role === 'admin' || user.role === 'superuser') {
                    window.location.href = '/Admin';
                } else {
                    window.location.href = '/';
                }
            }
        } else {
            // Sai mật khẩu hoặc lỗi khác
            errorDiv.innerText = result.message || "Sai tên đăng nhập hoặc mật khẩu!";
            errorDiv.style.display = 'block';
        }
    } catch (error) {
        errorDiv.innerText = "Lỗi kết nối tới Server.";
        errorDiv.style.display = 'block';
    }
});

document.getElementById('verifyOtpBtn')?.addEventListener('click', async () => {
    const username = document.getElementById('username').value;
    const otp = document.getElementById('otp').value;
    const errorDiv = document.getElementById('loginError');

    try {
        const response = await fetch(`${API_PROXY_URL}/VerifyOTP`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, otp })
        });

        const result = await response.json();

        if (result.success) {
            localStorage.setItem('tokenExpiry', Date.now() + 6 * 60 * 60 * 1000); // 6 tiếng
            localStorage.setItem('currentUser', JSON.stringify(result.data.user));
            const user = result.data.user;
            if (user.role === 'admin' || user.role === 'superuser') {
                window.location.href = '/Admin';
            } else {
                window.location.href = '/';
            }
        } else {
            errorDiv.innerText = result.message || "Mã OTP không hợp lệ!";
            errorDiv.style.display = 'block';
        }
    } catch (error) {
        errorDiv.innerText = "Lỗi xác thực OTP.";
        errorDiv.style.display = 'block';
    }
});

// ── ĐĂNG KÝ BƯỚC 1: Gửi thông tin và nhận OTP ──
document.getElementById('registerForm')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const name = document.getElementById('regName').value;
    const username = document.getElementById('regUsername').value;
    const email = document.getElementById('regEmail').value;
    const password = document.getElementById('regPassword').value;
    const confirmPassword = document.getElementById('regConfirmPassword').value;
    const errorDiv = document.getElementById('regError');
    const btn = document.getElementById('registerSubmitBtn');

    if (password !== confirmPassword) {
        errorDiv.innerText = "Mật khẩu xác nhận không khớp!";
        errorDiv.style.display = 'block';
        return;
    }

    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Đang gửi OTP...';

    try {
        const response = await fetch(`${API_PROXY_URL}/Register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name, username, email, password })
        });

        const result = await response.json();

        if (result.success && result.message === "OTP_REQUIRED") {
            // Ẩn form đăng ký, hiện form OTP
            document.getElementById('registerSection').style.display = 'none';
            document.getElementById('otpRegisterSection').style.display = 'block';
            document.getElementById('registerSubtitle').innerText = 'Nhập mã OTP đã được gửi đến email của bạn';
            errorDiv.style.display = 'none';
        } else {
            errorDiv.innerText = result.message || "Lỗi khi đăng ký.";
            errorDiv.style.display = 'block';
            btn.disabled = false;
            btn.innerHTML = '<i class="bi bi-send me-2"></i>Gửi mã xác thực OTP';
        }
    } catch (error) {
        errorDiv.innerText = "Lỗi kết nối tới Server.";
        errorDiv.style.display = 'block';
        btn.disabled = false;
        btn.innerHTML = '<i class="bi bi-send me-2"></i>Gửi mã xác thực OTP';
    }
});

// ── ĐĂNG KÝ BƯỚC 2: Xác thực OTP → Lưu tài khoản vào Database ──
document.getElementById('verifyRegisterOtpBtn')?.addEventListener('click', async () => {
    const email = document.getElementById('regEmail').value;
    const otp = document.getElementById('regOtp').value;
    const errorDiv = document.getElementById('regOtpError');
    const successDiv = document.getElementById('regOtpSuccess');
    const btn = document.getElementById('verifyRegisterOtpBtn');

    if (!otp || otp.length !== 6) {
        errorDiv.innerText = "Vui lòng nhập đủ 6 chữ số OTP.";
        errorDiv.style.display = 'block';
        return;
    }

    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Đang xác thực...';

    try {
        const response = await fetch(`${API_PROXY_URL}/VerifyRegisterOTP`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, otp })
        });

        const result = await response.json();

        if (result.success) {
            successDiv.style.display = 'block';
            errorDiv.style.display = 'none';
            btn.style.display = 'none';
            setTimeout(() => window.location.href = '/Account/Login', 2500);
        } else {
            errorDiv.innerText = result.message || "Mã OTP không hợp lệ!";
            errorDiv.style.display = 'block';
            btn.disabled = false;
            btn.innerHTML = '<i class="bi bi-check2-circle me-2"></i>Xác nhận & Tạo tài khoản';
        }
    } catch (error) {
        errorDiv.innerText = "Lỗi kết nối tới Server.";
        errorDiv.style.display = 'block';
        btn.disabled = false;
        btn.innerHTML = '<i class="bi bi-check2-circle me-2"></i>Xác nhận & Tạo tài khoản';
    }
});


function checkAuth() {
    const userJson = localStorage.getItem('currentUser');
    const path = window.location.pathname.toLowerCase();

    if (!userJson) {
        if (path !== '/account/login' && path !== '/account/register') {
            window.location.href = '/Account/Login';
        }
        return;
    }

    const user = JSON.parse(userJson);
    
    // Nếu đã đăng nhập mà cố vào trang login/register
    if (path === '/account/login' || path === '/account/register') {
        if (user.role === 'admin' || user.role === 'superuser') {
            window.location.href = '/Admin';
        } else {
            window.location.href = '/';
        }
        return;
    }

    // Kiểm tra quyền truy cập trang Admin
    if (path.startsWith('/admin') && user.role !== 'admin' && user.role !== 'superuser') {
        window.location.href = '/';
    }
}

