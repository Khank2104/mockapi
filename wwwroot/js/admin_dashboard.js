const API_PROXY_URL = "/api/UserProxy";
let currentUser = null;

document.addEventListener('DOMContentLoaded', () => {
    currentUser = JSON.parse(localStorage.getItem('currentUser'));
    if (!currentUser || (currentUser.role !== 'admin' && currentUser.role !== 'superuser')) {
        window.location.href = '/Account/Login';
        return;
    }
    setupDashboard();
});

function setupDashboard() {
    document.getElementById('welcomeText').innerText = `Chào mừng, ${currentUser.name}!`;
    const tbody = document.getElementById('userTableBody');
    if (tbody) {
        tbody.innerHTML = `
            <tr>
                <td colspan="4" class="text-center py-5">
                    <div class="alert alert-info d-inline-block">
                        <i class="bi bi-info-circle-fill me-2"></i>
                        Hệ thống đang chuyển đổi sang mô hình Quản lý Phòng trọ chuyên biệt. 
                        <br/>Vui lòng sử dụng các phân hệ AdminManagement / TenantManagement mới.
                    </div>
                </td>
            </tr>`;
    }
    
    // Disable old action buttons
    const addUserBtn = document.getElementById('addUserBtn');
    if (addUserBtn) {
        addUserBtn.disabled = true;
        addUserBtn.innerText = "Chức năng đã chuyển dời";
    }
}

function logout() {
    localStorage.removeItem('currentUser');
    window.location.href = '/Account/Login';
}
