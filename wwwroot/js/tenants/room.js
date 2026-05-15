/**
 * Tenant Room Management Module
 */
const RoomMgmt = (() => {
    const init = () => {
        loadRoomInfo();
    };

    const loadRoomInfo = async () => {
        const container = document.getElementById('room-info-content');
        if (!container) return;

        try {
            const response = await fetch('/api/TenantPortal/MyRoomInfo');
            const result = await response.json();
            
            if (result.success) {
                renderRoomData(result.data);
            } else {
                container.innerHTML = `<div class="alert alert-info rounded-4 p-4">${result.message}</div>`;
            }
        } catch (e) {
            container.innerHTML = `<div class="alert alert-danger rounded-4 p-4">Lỗi kết nối máy chủ.</div>`;
        }
    };

    const renderRoomData = (data) => {
        // Basic Info
        document.getElementById('room-code-display').innerText = data.roomCode;
        document.getElementById('motel-name-display').innerText = data.motelName;
        document.getElementById('room-rent-display').innerText = data.monthlyRent.toLocaleString('vi-VN') + ' đ';
        document.getElementById('checkin-date-display').innerText = new Date(data.checkInDate).toLocaleDateString('vi-VN');

        // Occupants
        const occupantList = document.getElementById('occupant-list');
        occupantList.innerHTML = data.occupants.map(o => `
            <div class="d-flex align-items-center gap-3 p-3 bg-secondary bg-opacity-10 rounded-4 mb-3 border-0">
                <div class="bg-body rounded-circle shadow-sm p-2">
                    <i class="bi bi-person-fill text-primary fs-5"></i>
                </div>
                <div class="flex-grow-1">
                    <div class="fw-bold text-body">${o.fullName}</div>
                    <div class="small text-muted">${o.occupantRole === 'Owner' ? 'Chủ hợp đồng' : 'Thành viên'}</div>
                </div>
                <div class="text-end">
                    <div class="small fw-bold text-primary">${o.phone || 'N/A'}</div>
                </div>
            </div>
        `).join('');

        // Services
        const serviceList = document.getElementById('service-list');
        serviceList.innerHTML = data.services.map(s => `
            <div class="d-flex justify-content-between align-items-center p-3 border-bottom border-light">
                <div class="d-flex align-items-center gap-3">
                    <div class="text-primary"><i class="bi bi-check-circle-fill"></i></div>
                    <div class="fw-bold text-body small text-uppercase">${s.serviceName}</div>
                </div>
                <div class="text-end">
                    <div class="fw-bold">${s.unitPrice.toLocaleString('vi-VN')} đ</div>
                    <div class="small text-muted">${s.calculationType === 'metered' ? 'Số (Điện/Nước)' : 'Tháng'}</div>
                </div>
            </div>
        `).join('');
    };

    return { init };
})();

document.addEventListener('DOMContentLoaded', RoomMgmt.init);
