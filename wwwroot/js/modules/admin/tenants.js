        let currentTenantPage = 1;
        let totalTenantPages = 1;

        async function loadTenantsData(page = 1, motelId = window._globalSelectedMotelId) {
            currentTenantPage = page;
            const container = document.getElementById('tenant-list-container');
            const search = document.getElementById('tenant-search-input').value;
            
            // UI indicator for selected motel
            const badge = document.getElementById('tenant-motel-badge');
            const nameSpan = document.getElementById('tenant-selected-motel-name');
            if (motelId && window._allMotelsCache.length > 0) {
                const motel = window._allMotelsCache.find(m => m.motelId == motelId);
                if (motel) {
                    nameSpan.innerText = motel.motelName;
                    badge.classList.remove('d-none');
                }
            } else {
                badge.classList.add('d-none');
            }

            try {
                const response = await fetch(`/api/TenantManagement/GetAllTenants?searchTerm=${encodeURIComponent(search)}&page=${page}&pageSize=12${motelId ? `&motelId=${motelId}` : ''}`);
                const result = await response.json();
                
                if (result.success) {
                    const { items, totalCount, totalPages, currentPage } = result.data;
                    totalTenantPages = totalPages;

                    if (items.length === 0) {
                        container.innerHTML = '<div class="col-12 text-center py-5 text-muted"><i class="bi bi-person-x fs-1 d-block mb-3 opacity-25"></i><p>Không tìm thấy khách thuê phù hợp.</p></div>';
                        updateTenantPaginationUI(0, 0, 0, 1, 1);
                        return;
                    }

                    container.innerHTML = items.map(t => {
                        const statusLabel = t.status === 'Staying' ? 'Đang ở' : t.status === 'Prospective' ? 'Chưa vào ở' : t.status;
                        const statusClass = (t.status === 'Staying' || t.status === 'Active') ? 'bg-success' : (t.status === 'Prospective' ? 'bg-warning' : 'bg-secondary');
                        
                        return `
                        <div class="col-md-6 col-lg-4 col-xl-3">
                            <div class="glass-card h-100 p-4 d-flex flex-column align-items-center text-center tenant-card position-relative">
                                <span class="badge ${statusClass} bg-opacity-10 ${statusClass.replace('bg-', 'text-')} rounded-pill px-3 py-1 position-absolute top-0 end-0 m-3 x-small fw-bold">
                                    <i class="bi bi-circle-fill me-1" style="font-size: 0.4rem;"></i>${statusLabel}
                                </span>
                                
                                <div class="mb-3 position-relative">
                                    <img src="https://ui-avatars.com/api/?name=${encodeURIComponent(t.fullName)}&background=6366f1&color=fff&size=100" 
                                         class="rounded-circle shadow-sm border border-2 border-white" style="width: 80px; height: 80px; object-fit: cover;">
                                    ${t.currentRoomCode !== 'N/A' ? '<div class="position-absolute bottom-0 end-0 bg-primary text-white rounded-circle d-flex align-items-center justify-content-center shadow-sm" style="width:24px; height:24px; border: 2px solid white;"><i class="bi bi-house-door-fill" style="font-size: 0.7rem;"></i></div>' : ''}
                                </div>
                                
                                <h5 class="fw-bold mb-1">${t.fullName}</h5>
                                <div class="badge bg-primary bg-opacity-10 text-primary rounded-pill px-3 py-1 mb-3 small">
                                    ${t.currentRoomCode !== 'N/A' ? 'Phòng ' + t.currentRoomCode : 'Chưa có phòng'}
                                </div>
                                
                                <div class="vstack gap-2 w-100 mb-4">
                                    <div class="d-flex align-items-center justify-content-center gap-2 small text-muted">
                                        <i class="bi bi-phone-fill text-primary"></i>
                                        <span>${t.phone || 'N/A'}</span>
                                    </div>
                                    <div class="d-flex align-items-center justify-content-center gap-2 small text-muted text-truncate">
                                        <i class="bi bi-envelope-at-fill text-primary"></i>
                                        <span class="text-truncate" title="${t.email || 'N/A'}">${t.email || 'N/A'}</span>
                                    </div>
                                </div>
                                
                                <button class="btn btn-premium w-100 rounded-pill mt-auto" onclick='viewTenantDetails(${JSON.stringify(t).replace(/'/g, "&apos;")})'>
                                    <i class="bi bi-info-circle me-1"></i>Xem chi tiết
                                </button>
                            </div>
                        </div>
                        `;
                    }).join('');

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
                loadTenantsData(next, window._globalSelectedMotelId);
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

            const balanceEl = document.getElementById('td-balance');
            const balanceVal = tenant.balance || 0;
            balanceEl.innerText = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(balanceVal);
            balanceEl.className = 'fw-bold ' + (balanceVal > 0 ? 'text-success' : (balanceVal < 0 ? 'text-danger' : 'text-muted'));

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
