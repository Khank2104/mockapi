        async function loadGlobalServicesData() {
            const container = document.getElementById('global-settings-fields');
            try {
                const response = await fetch('/api/MotelManagement/GetGlobalServices');
                const result = await response.json();
                
                if (result.success && result.data) {
                    if (result.data.length === 0) {
                        container.innerHTML = `
                            <div class="text-center py-5">
                                <div class="mb-3"><i class="bi bi-info-circle fs-1 text-muted opacity-25"></i></div>
                                <h5 class="text-muted">Chưa có dịch vụ nào</h5>
                                <p class="text-muted small">Bấm <strong>"Khởi tạo mẫu"</strong> để tạo nhanh các dịch vụ Điện, Nước...</p>
                            </div>`;
                        return;
                    }
                    
                    container.innerHTML = result.data.map(s => {
                        let unitLabel = s.unit || 'Phòng';
                        let iconClass = 'bi-box';
                        const lowerName = (s.serviceName || "").toLowerCase();
                        
                        if (lowerName.includes('điện')) {
                            unitLabel = '1 kWh';
                            iconClass = 'bi-lightning-charge';
                        } else if (lowerName.includes('nước')) {
                            unitLabel = '1 Khối';
                            iconClass = 'bi-droplet';
                        } else if (lowerName.includes('rác') || lowerName.includes('vệ sinh')) {
                            unitLabel = '1 Phòng';
                            iconClass = 'bi-trash3';
                        } else if (lowerName.includes('wifi') || lowerName.includes('internet')) {
                            unitLabel = '1 Phòng';
                            iconClass = 'bi-wifi';
                        }

                        const currentPrice = s.defaultPrice ? s.defaultPrice.toLocaleString() : "0";

                        return `
                            <div class="mb-4 p-3 rounded-4 bg-light bg-opacity-50 border border-white">
                                <div class="d-flex justify-content-between align-items-center mb-2">
                                    <label class="form-label small fw-bold text-uppercase mb-0">
                                        <i class="bi ${iconClass} me-2 text-primary"></i>${s.serviceName}
                                    </label>
                                    <span class="badge bg-white text-primary border border-primary border-opacity-10 rounded-pill px-3 py-2">
                                        Hiện tại: ${currentPrice}đ / ${unitLabel}
                                    </span>
                                </div>
                                <div class="input-group">
                                    <input type="number" class="form-control border-0 rounded-4 py-3 fw-bold service-price-input" 
                                           value="${s.defaultPrice || 0}" step="100" data-id="${s.serviceId}" placeholder="Nhập đơn giá mới...">
                                    <span class="input-group-text bg-transparent border-0 pe-3 fw-bold text-muted">VNĐ</span>
                                </div>
                            </div>
                        `;
                    }).join('');
                }
            } catch (e) { 
                console.error("Global services error", e);
                container.innerHTML = '<div class="alert alert-danger">Lỗi tải dữ liệu.</div>';
            }
        }


window.loadGlobalServicesData = loadGlobalServicesData;
        async function initializeDefaultServices() {
            if (!confirm("Hệ thống sẽ tạo sẵn các dịch vụ mẫu (Điện 3.500, Nước 25.000, Rác 50.000, Wifi 100.000). Tiếp tục?")) return;
            
            try {
                const response = await fetch('/api/MotelManagement/SeedDefaultServices', { method: 'POST' });
                const result = await response.json();
                if (result.success) {
                    showPremiumToast("Thành công", result.message, "success");
                    loadGlobalServicesData();
                } else {
                    showPremiumToast("Lỗi", result.message, "danger");
                }
            } catch (e) {
                showPremiumToast("Lỗi", "Không thể kết nối máy chủ.", "danger");
            }
        }


window.initializeDefaultServices = initializeDefaultServices;
        async function saveAllGlobalSettings() {
            const btn = document.getElementById('btnSaveGlobalSettings');
            const btnText = btn.querySelector('.btn-text');
            const spinner = btn.querySelector('.spinner-border');
            const inputs = document.querySelectorAll('.service-price-input');
            
            if (inputs.length === 0) {
                showPremiumToast("Thông tin", "Vui lòng thêm dịch vụ trước khi cập nhật giá.", "info");
                return;
            }

            // Loading state
            btn.disabled = true;
            btnText.innerText = "Đang xử lý...";
            spinner.classList.remove('d-none');
            
            let successCount = 0;
            let failCount = 0;

            try {
                for (const input of inputs) {
                    const id = input.getAttribute('data-id');
                    const price = parseFloat(input.value);
                    
                    if (isNaN(price)) continue;

                    const response = await fetch(`/api/MotelManagement/UpdateGlobalService/${id}`, {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ defaultPrice: price })
                    });
                    const result = await response.json();
                    if (result.success) successCount++;
                    else failCount++;
                }

                if (successCount > 0) {
                    showPremiumToast("Thành công", `Đã cập nhật đơn giá cho ${successCount} dịch vụ.`, "success");
                    loadGlobalServicesData();
                }
                if (failCount > 0) {
                    showPremiumToast("Lỗi", `Có ${failCount} dịch vụ không thể cập nhật.`, "danger");
                }
            } catch (e) {
                showPremiumToast("Lỗi", "Không thể hoàn tất việc lưu cấu hình.", "danger");
            } finally {
                btn.disabled = false;
                btnText.innerText = "Lưu cấu hình toàn bộ";
                spinner.classList.add('d-none');
            }
        }


window.saveAllGlobalSettings = saveAllGlobalSettings;
        function showAddGlobalServiceModal() {
            new bootstrap.Modal(document.getElementById('addGlobalServiceModal')).show();
        }


window.showAddGlobalServiceModal = showAddGlobalServiceModal;
        async function submitAddGlobalService() {
            const form = document.getElementById('addGlobalServiceForm');
            const formData = new FormData(form);
            
            // Map keys explicitly to avoid undefined/case-sensitive issues
            const data = {
                ServiceName: formData.get("ServiceName"),
                ServiceCode: formData.get("ServiceCode"),
                Unit: formData.get("Unit"),
                CalculationType: formData.get("CalculationType"),
                DefaultPrice: parseFloat(formData.get("DefaultPrice") || "0")
            };

            // Basic validation
            if (!data.ServiceName || !data.ServiceCode) {
                showPremiumToast("Lỗi", "Vui lòng nhập đầy đủ Tên và Mã dịch vụ.", "warning");
                return;
            }

            try {
                const response = await fetch('/api/MotelManagement/CreateGlobalService', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(data)
                });
                const result = await response.json();
                if (result.success) {
                    showPremiumToast("Thành công", "Đã khởi tạo dịch vụ hệ thống.", "success");
                    bootstrap.Modal.getInstance(document.getElementById('addGlobalServiceModal')).hide();
                    form.reset();
                    loadGlobalServicesData();
                } else {
                    showPremiumToast("Lỗi", result.message, "danger");
                }
            } catch (e) {
                showPremiumToast("Lỗi", "Không thể kết nối đến máy chủ.", "danger");
            }
        }

        // --- Contracts Logic ---

window.submitAddGlobalService = submitAddGlobalService;

async function submitEditGlobalService() {
    const form = document.getElementById('editGlobalServiceForm');
    const id = document.getElementById('edit-service-id').value;
    const price = parseFloat(document.getElementById('edit-service-price').value);
    const btn = document.getElementById('btnSubmitGlobalService');

    if (isNaN(price)) return showPremiumToast('L?i', 'Gi� kh�ng h?p l?', 'danger');
    btn.disabled = true;
    try {
        const response = await fetch('/api/MotelManagement/UpdateGlobalService/' + id, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ defaultPrice: price })
        });
        const result = await response.json();
        if (result.success) {
            showPremiumToast('Th�nh c�ng', '�� c?p nh?t gi� v� g?i th�ng b�o', 'success');
            bootstrap.Modal.getInstance(document.getElementById('editGlobalServiceModal')).hide();
            loadGlobalServicesData();
        } else {
            showPremiumToast('L?i', result.message, 'danger');
        }
    } catch(e) {} finally {
        btn.disabled = false;
    }
}

window.submitEditGlobalService = submitEditGlobalService;

