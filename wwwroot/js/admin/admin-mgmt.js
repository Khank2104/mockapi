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
                            <td><span class="status-pill status-${a.status === 'Active' ? 'active' : 'locked'}">${a.status}</span></td>
                            <td>
                                <div class="d-flex gap-2">
                                    <button class="btn btn-sm btn-outline-primary rounded-pill px-3" onclick="openEditAdminModal(${a.id})">
                                        <i class="bi bi-pencil-square"></i>
                                    </button>
                                    <button class="btn btn-sm btn-outline-warning rounded-pill px-3" onclick="toggleAdminStatus(${a.id}, '${a.status}')">
                                        <i class="bi bi-${a.status === 'Active' ? 'lock' : 'unlock'}"></i>
                                    </button>
                                    <button class="btn btn-sm btn-outline-danger rounded-pill px-3" onclick="deleteAdmin(${a.id}, '${a.name}')">
                                        <i class="bi bi-trash"></i>
                                    </button>
                                </div>
                            </td>
                        </tr>
                    `).join('');
                }
            } catch (e) {}
        }

        async function toggleAdminStatus(id, currentStatus) {
            const newActive = currentStatus !== 'Active';
            try {
                const response = await fetch(`/api/AdminManagement/ToggleStatus/${id}?active=${newActive}`, { method: 'POST' });
                const result = await response.json();
                if (result.success) {
                    showPremiumToast("Thành công", result.message, "success");
                    loadAdminsData();
                }
            } catch (e) { console.error(e); }
        }

        async function openEditAdminModal(id) {
            try {
                const response = await fetch(`/api/AdminManagement/GetAdmin/${id}`);
                const result = await response.json();
                if (result.success) {
                    const a = result.data;
                    document.getElementById('edit-admin-id').value = a.userId;
                    document.getElementById('edit-admin-name').value = a.name;
                    document.getElementById('edit-admin-username').value = a.username;
                    document.getElementById('edit-admin-email').value = a.email;
                    document.getElementById('edit-admin-phone').value = a.phone || '';
                    document.getElementById('edit-admin-password').value = '';
                    
                    new bootstrap.Modal(document.getElementById('editAdminModal')).show();
                }
            } catch (e) { console.error(e); }
        }

        async function submitEditAdmin() {
            const id = document.getElementById('edit-admin-id').value;
            const form = document.getElementById('editAdminForm');
            const data = Object.fromEntries(new FormData(form).entries());

            try {
                const response = await fetch(`/api/AdminManagement/UpdateAdmin/${id}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(data)
                });
                const result = await response.json();
                if (result.success) {
                    showPremiumToast("Thành công", result.message, "success");
                    bootstrap.Modal.getInstance(document.getElementById('editAdminModal')).hide();
                    loadAdminsData();
                } else {
                    alert(result.message);
                }
            } catch (e) { console.error(e); }
        }

        async function deleteAdmin(id, name) {
            if (!confirm(`CẢNH BÁO: Bạn có chắc chắn muốn XÓA VĨNH VIỄN tài khoản Admin "${name}" không?\nHành động này không thể hoàn tác.`)) return;

            try {
                const response = await fetch(`/api/AdminManagement/DeleteAdmin/${id}`, { method: 'DELETE' });
                const result = await response.json();
                if (result.success) {
                    showPremiumToast("Đã xóa", result.message, "success");
                    loadAdminsData();
                } else {
                    showPremiumToast("Lỗi", result.message, "danger");
                }
            } catch (e) { console.error(e); }
        }

window.loadAdminsData = loadAdminsData;
window.toggleAdminStatus = toggleAdminStatus;
window.openEditAdminModal = openEditAdminModal;
window.submitEditAdmin = submitEditAdmin;
window.deleteAdmin = deleteAdmin;
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
