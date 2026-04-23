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
            localStorage.setItem('currentUser', JSON.stringify(result.data));
            const user = result.data;
            if (user.role === 'admin' || user.role === 'superuser') {
                window.location.href = '/Admin';
            } else {
                window.location.href = '/';
            }
        } else {
            errorDiv.innerText = result.message || "Sai tên đăng nhập hoặc mật khẩu!";
            errorDiv.style.display = 'block';
        }
    } catch (error) {
        errorDiv.innerText = "Lỗi kết nối tới Server.";
        errorDiv.style.display = 'block';
    }
});

document.getElementById('registerForm')?.addEventListener('submit', async (e) => {
    e.preventDefault();
    // Correcting IDs from Register.cshtml: regName, regUsername, regEmail, regPassword, regConfirmPassword, regError, regSuccess
    const name = document.getElementById('regName').value;
    const username = document.getElementById('regUsername').value;
    const email = document.getElementById('regEmail').value;
    const password = document.getElementById('regPassword').value;
    const confirmPassword = document.getElementById('regConfirmPassword').value;
    const errorDiv = document.getElementById('regError');
    const successDiv = document.getElementById('regSuccess');

    if (password !== confirmPassword) {
        errorDiv.innerText = "Mật khẩu xác nhận không khớp!";
        errorDiv.style.display = 'block';
        return;
    }

    try {
        const response = await fetch(`${API_PROXY_URL}/Register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name, username, email, password, avatar: "" })
        });

        const result = await response.json();

        if (result.success) {
            successDiv.style.display = 'block';
            errorDiv.style.display = 'none';
            setTimeout(() => window.location.href = '/Account/Login', 2000);
        } else {
            errorDiv.innerText = result.message || "Lỗi khi đăng ký.";
            errorDiv.style.display = 'block';
        }
    } catch (error) {
        errorDiv.innerText = "Lỗi kết nối.";
        errorDiv.style.display = 'block';
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

