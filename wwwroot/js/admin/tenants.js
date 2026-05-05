        async function loadTenantsData() {
            const container = document.getElementById('tenant-list-container');
            try {
                const response = await fetch('/api/TenantManagement/GetAllTenants');
                const result = await response.json();
                if (result.success) {
                    if (result.data.length === 0) {
                        container.innerHTML = '<tr><td colspan="5" class="text-center py-5">Chưa có khách thuê nào.</td></tr>';
                        return;
                    }
                    container.innerHTML = result.data.map(t => `
                        <tr>
                            <td>
                                <div class="d-flex align-items-center gap-3">
                                    <img src="https://ui-avatars.com/api/?name=${encodeURIComponent(t.fullName)}&background=6366f1&color=fff" class="rounded-circle" style="width: 38px; height: 38px;">
                                    <div>
                                        <div class="fw-bold">${t.fullName}</div>
                                        <div class="x-small text-muted">CCCD: ${t.idCard || 'Chưa cập nhật'}</div>
                                    </div>
                                </div>
                            </td>
                            <td><span class="badge bg-primary bg-opacity-10 text-primary px-3 py-2 rounded-pill">${t.currentRoomCode !== 'N/A' ? 'Phòng ' + t.currentRoomCode : 'Chưa có phòng'}</span></td>
                            <td>
                                <div class="small"><i class="bi bi-phone text-primary me-1"></i>${t.phone || 'N/A'}</div>
                                <div class="x-small text-muted"><i class="bi bi-envelope me-1"></i>${t.email || 'N/A'}</div>
                            </td>
                            <td>
                                <span class="status-pill ${t.status === 'Active' || t.status === 'Staying' ? 'status-active' : 'status-locked'}">
                                    <i class="bi bi-circle-fill me-1" style="font-size: 0.4rem;"></i>
                                    ${t.status === 'Staying' ? 'Đang ở' : t.status === 'Prospective' ? 'Chưa vào ở' : t.status}
                                </span>
                            </td>
                            <td>
                                <button class="btn btn-sm btn-premium px-3 rounded-pill" onclick='viewTenantDetails(${JSON.stringify(t).replace(/'/g, "&apos;")})'>Chi tiết</button>
                            </td>
                        </tr>
                    `).join('');
                }
            } catch (e) {}
        }


window.loadTenantsData = loadTenantsData;
        function viewTenantDetails(tenant) {
            document.getElementById('td-avatar').src = `https://ui-avatars.com/api/?name=${encodeURIComponent(tenant.fullName)}&background=6366f1&color=fff&size=128`;
            document.getElementById('td-fullname').innerText = tenant.fullName;
            document.getElementById('td-idcard').innerText = tenant.idCard || 'Chưa cập nhật';
            document.getElementById('td-phone').innerText = tenant.phone || 'N/A';
            document.getElementById('td-email').innerText = tenant.email || 'N/A';
            document.getElementById('td-room').innerText = tenant.currentRoomCode !== 'N/A' ? `Phòng ${tenant.currentRoomCode}` : 'Chưa xếp phòng';

            const statusEl = document.getElementById('td-status');
            if (tenant.status === 'Staying') {
                statusEl.className = 'badge rounded-pill px-3 py-2 mt-1 bg-success bg-opacity-10 text-success';
                statusEl.innerText = 'Đang ở';
            } else if (tenant.status === 'Prospective') {
                statusEl.className = 'badge rounded-pill px-3 py-2 mt-1 bg-warning bg-opacity-10 text-warning';
                statusEl.innerText = 'Chưa vào ở';
            } else {
                statusEl.className = 'badge rounded-pill px-3 py-2 mt-1 bg-primary bg-opacity-10 text-primary';
                statusEl.innerText = tenant.status;
            }

            const modal = new bootstrap.Modal(document.getElementById('tenantDetailsModal'));
            modal.show();
        }


window.viewTenantDetails = viewTenantDetails;
        function showAddTenantModal() {
            // Reset form before opening
            const form = document.getElementById('addTenantForm');
            if (form) form.reset();
            new bootstrap.Modal(document.getElementById('addTenantModal')).show();
        }


window.showAddTenantModal = showAddTenantModal;
        async function submitAddTenant() {
            const form = document.getElementById('addTenantForm');
            const formData = new FormData(form);
            
            // Map data explicitly to ensure correct structure
            const data = {
                name: formData.get("Name"),
                username: formData.get("Username"),
                email: formData.get("Email"),
                phoneNumber: formData.get("PhoneNumber"),
                password: formData.get("Password") || "123456"
            };

            if (!data.name || !data.username || !data.phoneNumber) {
                showPremiumToast("Lỗi", "Vui lòng nhập đầy đủ Họ tên, Tên đăng nhập và SĐT.", "warning");
                return;
            }

            try {
                const response = await fetch('/api/TenantManagement/CreateTenant', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(data)
                });
                const result = await response.json();
                if (result.success) {
                    showPremiumToast("Thành công", "Đã tạo tài khoản khách thuê mới.", "success");
                    bootstrap.Modal.getInstance(document.getElementById('addTenantModal')).hide();
                    form.reset();
                    loadTenantsData();
                } else {
                    showPremiumToast("Lỗi", result.message, "danger");
                }
            } catch (e) {
                alert("Lỗi khi gửi dữ liệu.");
            }
        }


window.submitAddTenant = submitAddTenant;
