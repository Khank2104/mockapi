        window._globalSelectedMotelId = null;
        window._allMotelsCache = [];

        async function switchModule(moduleId, navElement = null) {
            // Check if we are on the admin dashboard page
            if (window.location.pathname.toLowerCase() !== '/admin') {
                window.location.href = `/Admin?module=${moduleId}`;
                return;
            }

            document.querySelectorAll('.module-section').forEach(el => el.classList.add('d-none'));
            const target = document.getElementById(`module-${moduleId}`);
            if (target) {
                target.classList.remove('d-none');
                target.classList.add('animate-fade-in');
            }

            if (navElement) {
                document.querySelectorAll('.nav-link').forEach(el => el.classList.remove('active'));
                navElement.classList.add('active');
            }

            const titles = {
                'overview': 'Tổng quan hệ thống',
                'motels': 'Sơ đồ Phòng & Tầng',
                'tenants': 'Quản lý Khách thuê',
                'billing': 'Hóa đơn & Điện nước',
                'requests': 'Yêu cầu hỗ trợ',
                'admin-mgmt': 'Quản lý Admin',
                'global-services': 'Phí dịch vụ chung',
                'contracts': 'Quản lý Hợp đồng',
                'motel-setup': 'Thiết lập Khu trọ',
                'room-details': 'Chi tiết Phòng'
            };
            const pageTitleEl = document.getElementById('page-title');
            if (pageTitleEl && titles[moduleId]) {
                pageTitleEl.innerText = titles[moduleId];
            }

            // --- Unified Motel Selection Pattern ---
            const motelDependentModules = ['motels', 'tenants', 'billing', 'contracts'];
            
            if (motelDependentModules.includes(moduleId)) {
                // Ensure motels cache is loaded
                if (!window._allMotelsCache || window._allMotelsCache.length === 0) {
                    try {
                        const response = await fetch('/api/MotelManagement/MyMotels');
                        const result = await response.json();
                        if (result.success) window._allMotelsCache = result.data;
                    } catch (e) { console.error("Error pre-loading motels cache", e); }
                }

                if (!window._globalSelectedMotelId) {
                    renderGlobalMotelSelector(moduleId);
                    return;
                }
            }

            // Trigger data loading based on module
            if (moduleId === 'motels') {
                if (typeof loadMotelsData === 'function') loadMotelsData();
            }
            if (moduleId === 'tenants') loadTenantsData(1, window._globalSelectedMotelId);
            if (moduleId === 'billing') {
                if (typeof loadMotelsForBillingFilter === 'function') loadMotelsForBillingFilter();
                loadBillingData(1, window._globalSelectedMotelId);
            }
            if (moduleId === 'requests') loadAllRequestsData();
            if (moduleId === 'admin-mgmt') loadAdminsData();
            if (moduleId === 'global-services') loadGlobalServicesData();
            if (moduleId === 'contracts') {
                if (typeof loadMotelsForContractFilter === 'function') loadMotelsForContractFilter();
                loadContractsData(1, window._globalSelectedMotelId);
            }
            if (moduleId === 'overview') loadOverviewData();
        }

        async function renderGlobalMotelSelector(moduleId) {
            const containerIdMap = {
                'motels': 'floormap-container',
                'tenants': 'tenant-list-container',
                'billing': 'billing-list-container',
                'contracts': 'contracts-list-container'
            };
            const targetId = containerIdMap[moduleId];
            const container = document.getElementById(targetId);
            if (!container) return;

            // Clear any active panels if it's floormap
            if (moduleId === 'motels' && typeof window.closeFmpPanel === 'function') window.closeFmpPanel();

            container.innerHTML = '<div class="col-12 text-center py-5"><div class="spinner-border text-primary" role="status"></div><p class="mt-2 text-muted">Đang tải danh sách khu trọ...</p></div>';

            try {
                if (window._allMotelsCache.length === 0) {
                    const res = await fetch('/api/MotelManagement/MyMotels');
                    const result = await res.json();
                    if (result.success) window._allMotelsCache = result.data;
                }

                if (window._allMotelsCache.length === 0) {
                    container.innerHTML = '<div class="col-12 empty-state"><i class="bi bi-houses"></i><h3>Chưa có khu trọ</h3><p>Bạn cần tạo khu trọ trước khi quản lý dữ liệu.</p></div>';
                    return;
                }

                let summaryHtml = '';
                // Removed global contract count fetch to prevent potential hangs and simplify UI

                let html = summaryHtml + `
                    <div class="col-12 text-center mb-5 animate-fade-in">
                        <h3 class="fw-bold"><i class="bi bi-building-check me-2 text-primary"></i>Vui lòng chọn Khu trọ</h3>
                        <p class="text-muted">Bạn cần chọn một khu trọ để bắt đầu quản lý dữ liệu chi tiết</p>
                    </div>
                    <div class="row g-4 justify-content-center">`;
                
                window._allMotelsCache.forEach(m => {
                    let totalRooms = (m.rooms ? m.rooms.length : 0);
                    if (m.floors && m.floors.length > 0) {
                        totalRooms = m.floors.reduce((acc, f) => acc + (f.rooms ? f.rooms.length : 0), 0);
                    }

                    html += `
                    <div class="col-md-6 col-lg-4 animate-fade-in">
                        <div class="glass-card h-100 p-4 d-flex flex-column align-items-center text-center motel-selection-card hover-lift" 
                             onclick="selectGlobalMotel('${moduleId}', ${m.motelId})" style="cursor:pointer; border: 2px solid transparent;">
                            <div class="bg-primary bg-opacity-10 text-primary rounded-circle d-flex align-items-center justify-content-center mb-3" style="width:64px; height:64px; font-size:1.8rem">
                                <i class="bi bi-building"></i>
                            </div>
                            <h5 class="fw-bold mb-2">${m.motelName}</h5>
                            <p class="text-muted small mb-4" style="min-height: 40px; display: flex; align-items: center; justify-content: center;">
                                <i class="bi bi-geo-alt-fill me-1 text-primary"></i>${m.address || 'Chưa cập nhật địa chỉ'}
                            </p>
                            <div class="mt-auto w-100 d-flex gap-2 justify-content-center flex-wrap">
                                <span class="badge bg-primary bg-opacity-10 text-primary rounded-pill px-3 py-2">
                                    <i class="bi bi-door-closed me-1"></i>${totalRooms} Phòng
                                </span>
                                <span class="badge ${m.useFloorManagement ? 'bg-info' : 'bg-secondary'} bg-opacity-10 ${m.useFloorManagement ? 'text-info' : 'text-secondary'} rounded-pill px-3 py-2">
                                    <i class="bi bi-layers me-1"></i>${m.useFloorManagement ? 'Phân tầng' : 'Không tầng'}
                                </span>
                            </div>
                        </div>
                    </div>`;
                });
                html += '</div>';
                
                // Final check if container still exists before injecting
                const finalContainer = document.getElementById(targetId);
                if (finalContainer) finalContainer.innerHTML = html;
            } catch (e) {
                console.error("Error loading motels for selection:", e);
                container.innerHTML = '<div class="col-12 alert alert-soft-danger text-center py-5"><i class="bi bi-exclamation-triangle fs-2 d-block mb-3"></i>Không thể tải danh sách khu trọ. Vui lòng thử lại.</div>';
            }

        }

        function selectGlobalMotel(moduleId, motelId) {
            window._globalSelectedMotelId = motelId;
            switchModule(moduleId); // Re-run with selected motel
        }

        function changeGlobalMotel(moduleId) {
            window._globalSelectedMotelId = null;
            switchModule(moduleId);
        }

        function resetGlobalMotel() {
            window._globalSelectedMotelId = null;
        }

        window.selectGlobalMotel = selectGlobalMotel;
        window.changeGlobalMotel = changeGlobalMotel;
        window.resetGlobalMotel = resetGlobalMotel;

        // --- Utility: Toast Notification ---

window.switchModule = switchModule;
        function showPremiumToast(title, message, type = 'primary') {
            const container = document.getElementById('toast-container');
            if (!container) {
                const div = document.createElement('div');
                div.id = 'toast-container';
                div.className = 'toast-container position-fixed bottom-0 end-0 p-3';
                div.style.zIndex = '9999';
                document.body.appendChild(div);
            }
            
            const id = 'toast-' + Date.now();
            const icon = type === 'success' ? 'bi-check-circle-fill' : (type === 'danger' ? 'bi-exclamation-triangle-fill' : 'bi-info-circle-fill');
            const colorClass = type === 'success' ? 'text-success' : (type === 'danger' ? 'text-danger' : 'text-primary');
            
            const html = `
                <div id="${id}" class="toast glass-card border-0 shadow-premium" role="alert" aria-live="assertive" aria-atomic="true">
                    <div class="toast-header border-0 bg-transparent">
                        <i class="bi ${icon} ${colorClass} me-2"></i>
                        <strong class="me-auto">${title}</strong>
                        <small class="text-muted">Vừa xong</small>
                        <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
                    </div>
                    <div class="toast-body small">
                        ${message}
                    </div>
                </div>`;
            
            document.getElementById('toast-container').insertAdjacentHTML('beforeend', html);
            const toastEl = document.getElementById(id);
            const toast = new bootstrap.Toast(toastEl, { delay: 4000 });
            toast.show();
            
            toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
        }
        // --- Global Services Logic ---

window.showPremiumToast = showPremiumToast;
        function getServiceIcon(code) {
            const c = (code || '').toLowerCase();
            if (c.includes('electric')) return 'bi-lightning-charge-fill';
            if (c.includes('water')) return 'bi-droplet-fill';
            if (c.includes('wifi') || c.includes('internet')) return 'bi-wifi';
            if (c.includes('trash') || c.includes('clean') || c.includes('rac')) return 'bi-trash-fill';
            if (c.includes('park') || c.includes('xe')) return 'bi-bicycle';
            return 'bi-gear-fill';
        }


window.getServiceIcon = getServiceIcon;

// --- Initialize correct module on load ---
document.addEventListener('DOMContentLoaded', () => {
    if (window.location.pathname.toLowerCase() === '/admin') {
        const urlParams = new URLSearchParams(window.location.search);
        const moduleParam = urlParams.get('module') || 'overview';
        
        // Find the nav element for this module to set it active
        const navEl = document.querySelector(`.nav-link[onclick*="'${moduleParam}'"]`);
        switchModule(moduleParam, navEl);
    }
});
