        // --- Core Navigation (Must be defined first) ---
        async function switchModule(moduleId, navElement = null) {
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
            if (titles[moduleId]) document.getElementById('page-title').innerText = titles[moduleId];

            // Trigger data loading based on module
            if (moduleId === 'motels') loadMotelsData();
            if (moduleId === 'tenants') loadTenantsData(1);
            if (moduleId === 'billing') {
                if (typeof loadMotelsForBillingFilter === 'function') loadMotelsForBillingFilter();
                loadBillingData(1);
            }
            if (moduleId === 'requests') loadAllRequestsData();
            if (moduleId === 'admin-mgmt') loadAdminsData();
            if (moduleId === 'global-services') loadGlobalServicesData();
            if (moduleId === 'contracts') {
                if (typeof loadMotelsForContractFilter === 'function') loadMotelsForContractFilter();
                loadContractsData(1);
            }
            if (moduleId === 'overview') loadOverviewData();
        }

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
