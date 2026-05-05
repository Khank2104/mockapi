        async function loadAdminsData() {
            const container = document.getElementById('admin-list-container');
            try {
                const response = await fetch('/api/AdminManagement/ListAdmins');
                const result = await response.json();
                if (result.success) {
                    if (result.data.length === 0) {
                        container.innerHTML = '<tr><td colspan="4" class="text-center py-5">Chưa có Admin nào.</td></tr>';
                        return;
                    }
                    container.innerHTML = result.data.map(a => `
                        <tr>
                            <td><div class="fw-bold">${a.name}</div></td>
                            <td><span class="badge bg-light text-dark">${a.username}</span></td>
                            <td><span class="status-pill status-${a.isActive ? 'active' : 'locked'}">${a.isActive ? 'Active' : 'Locked'}</span></td>
                            <td>
                                <button class="btn btn-sm btn-outline-danger rounded-pill" onclick="toggleAdminStatus(${a.userId}, ${a.isActive})">
                                    ${a.isActive ? 'Khóa' : 'Mở khóa'}
                                </button>
                            </td>
                        </tr>
                    `).join('');
                }
            } catch (e) {}
        }




window.loadAdminsData = loadAdminsData;
        function showAddAdminModal() {
            new bootstrap.Modal(document.getElementById('addAdminModal')).show();
        }


window.showAddAdminModal = showAddAdminModal;
        async function submitAddAdmin() {
            const form = document.getElementById('addAdminForm');
            const data = Object.fromEntries(new FormData(form).entries());
            
            try {
                const response = await fetch('/api/AdminManagement/CreateAdmin', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(data)
                });
                const result = await response.json();
                if (result.success) {
                    showPremiumToast("Thành công", "Đã tạo tài khoản Admin mới.", "success");
                    bootstrap.Modal.getInstance(document.getElementById('addAdminModal')).hide();
                    loadAdminsData();
                } else {
                    alert(result.message);
                }
            } catch (e) {
                alert("Lỗi khi gửi dữ liệu.");
            }
        }




window.submitAddAdmin = submitAddAdmin;
