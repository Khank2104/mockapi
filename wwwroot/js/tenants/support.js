/**
 * Tenant Support Request Module
 */
const SupportMgmt = (() => {
    const init = () => {
        loadRequests();
    };

    const loadRequests = async () => {
        const container = document.getElementById('request-list-container');
        if (!container) return;

        try {
            const response = await fetch('/api/TenantPortal/MyRequests');
            const result = await response.json();
            
            if (result.success) {
                if (result.data.length === 0) {
                    container.innerHTML = '<div class="text-center py-5 opacity-50">Chưa có yêu cầu nào.</div>';
                    return;
                }
                renderRequests(result.data);
            }
        } catch (e) {
            container.innerHTML = '<div class="alert alert-danger">Lỗi tải dữ liệu.</div>';
        }
    };

    const renderRequests = (data) => {
        const container = document.getElementById('request-list-container');
        container.innerHTML = data.map(r => `
            <div class="glass-card mb-3 p-3 border-0 shadow-sm">
                <div class="d-flex justify-content-between align-items-start mb-2">
                    <div>
                        <span class="badge ${getStatusBadgeClass(r.status)} rounded-pill mb-2">${r.status}</span>
                        <h6 class="fw-bold mb-1">${r.title}</h6>
                    </div>
                    <small class="text-muted">${new Date(r.createdAt).toLocaleDateString('vi-VN')}</small>
                </div>
                <p class="small text-muted mb-2">${r.description}</p>
                ${r.resolutionNote ? `
                    <div class="bg-success bg-opacity-10 p-2 rounded-3 mt-2">
                        <small class="text-success fw-bold d-block">Phản hồi từ quản lý:</small>
                        <small class="text-muted">${r.resolutionNote}</small>
                    </div>
                ` : ''}
            </div>
        `).join('');
    };

    const getStatusBadgeClass = (status) => {
        switch(status.toLowerCase()) {
            case 'pending': return 'bg-warning bg-opacity-10 text-warning';
            case 'inprogress': return 'bg-primary';
            case 'resolved': return 'bg-success';
            case 'rejected': return 'bg-danger';
            default: return 'bg-secondary';
        }
    };

    const submitRequest = async () => {
        const title = document.getElementById('req-title').value;
        const desc = document.getElementById('req-desc').value;
        const type = document.getElementById('req-type').value;

        if (!title || !desc) {
            showPremiumToast("Cảnh báo", "Vui lòng nhập đầy đủ thông tin.", "warning");
            return;
        }

        try {
            const response = await fetch('/api/TenantPortal/CreateRequest', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ title, description: desc, requestType: type })
            });
            const result = await response.json();
            if (result.success) {
                showPremiumToast("Thành công", "Yêu cầu của bạn đã được gửi đi.", "success");
                document.getElementById('addRequestForm').reset();
                bootstrap.Modal.getInstance(document.getElementById('addRequestModal')).hide();
                loadRequests();
            } else {
                showPremiumToast("Lỗi", result.message, "danger");
            }
        } catch (e) {
            showPremiumToast("Lỗi", "Không thể gửi yêu cầu.", "danger");
        }
    };

    return { init, submitRequest };
})();

document.addEventListener('DOMContentLoaded', SupportMgmt.init);
