        let currentTenantPage = 1;
        let totalTenantPages = 1;

        async function loadTenantsData(page = 1) {
            currentTenantPage = page;
            const container = document.getElementById('tenant-list-container');
            const search = document.getElementById('tenant-search-input').value;
            
            try {
                const response = await fetch(`/api/TenantManagement/GetAllTenants?searchTerm=${encodeURIComponent(search)}&page=${page}&pageSize=10`);
                const result = await response.json();
                
                if (result.success) {
                    const { items, totalCount, totalPages, currentPage } = result.data;
                    totalTenantPages = totalPages;

                    if (items.length === 0) {
                        container.innerHTML = '<tr><td colspan="5" class="text-center py-5">Không tìm thấy khách thuê phù hợp.</td></tr>';
                        updateTenantPaginationUI(0, 0, 0, 1, 1);
                        return;
                    }

                    container.innerHTML = items.map(t => `
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

                    const start = (currentPage - 1) * 10 + 1;
                    const end = start + items.length - 1;
                    updateTenantPaginationUI(start, end, totalCount, currentPage, totalPages);
                }
            } catch (e) {
                console.error('loadTenantsData error:', e);
            }
        }

        function updateTenantPaginationUI(start, end, total, current, totalPages) {
            document.getElementById('tenant-range').innerText = `${start}-${end}`;
            document.getElementById('tenant-total').innerText = total;
            document.getElementById('tenant-current-page').innerText = current;
            
            document.getElementById('tenant-prev-btn').classList.toggle('disabled', current <= 1);
            document.getElementById('tenant-next-btn').classList.toggle('disabled', current >= totalPages);
        }

        function changeTenantPage(delta) {
            const next = currentTenantPage + delta;
            if (next >= 1 && next <= totalTenantPages) {
                loadTenantsData(next);
            }
        }

window.loadTenantsData = loadTenantsData;
window.changeTenantPage = changeTenantPage;
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
