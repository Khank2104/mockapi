/**
 * Tenant Room Management Module
 */
const RoomMgmt = (() => {
    const init = () => {
        loadRoomInfo();
    };

    const loadRoomInfo = async () => {
        try {
            console.log("Fetching room info...");
            const response = await fetch('/api/TenantPortal/MyRoomInfo');
            const result = await response.json();
            
            if (result.success && result.data) {
                console.log("Room data received:", result.data);
                renderRoomData(result.data);
            } else {
                console.warn("Room info fetch failed:", result.message);
                const container = document.getElementById('room-info-content');
                if (container) {
                    container.innerHTML = `
                        <div class="col-12 text-center py-5">
                            <div class="glass-card p-5 border-0 shadow-premium rounded-4">
                                <i class="bi bi-info-circle fs-1 text-primary opacity-50 mb-3"></i>
                                <h4 class="fw-bold">${result.message || 'Chưa có thông tin phòng'}</h4>
                                <p class="text-muted">Có vẻ như bạn chưa có hợp đồng thuê phòng nào đang hoạt động trên hệ thống.</p>
                            </div>
                        </div>`;
                }
            }
        } catch (e) {
            console.error("Room info connection error:", e);
        }
    };

    const renderRoomData = (data) => {
        // Safe innerText updates
        const updateText = (id, text) => {
            const el = document.getElementById(id);
            if (el) el.innerText = text;
        };

        updateText('room-code-display', data.roomCode);
        updateText('motel-name-display', data.motelName);
        updateText('room-rent-display', (data.monthlyRent || 0).toLocaleString('vi-VN') + ' đ');
        updateText('checkin-date-display', data.checkInDate ? new Date(data.checkInDate).toLocaleDateString('vi-VN') : 'N/A');
        updateText('occupant-count', `${(data.occupants || []).length}/${data.standardOccupants || '--'}`);

        // Occupants
        const occupantList = document.getElementById('occupant-list');
        if (occupantList) {
            if (!data.occupants || data.occupants.length === 0) {
                occupantList.innerHTML = '<div class="text-center py-4 text-muted small">Chưa có thông tin cư dân</div>';
            } else {
                occupantList.innerHTML = data.occupants.map(o => `
                    <div class="d-flex align-items-center gap-3 p-3 rounded-4 mb-3 border animate-fade-in"
                         style="background: var(--bg-secondary) !important; border-color: var(--border-light) !important;">
                        <img src="https://ui-avatars.com/api/?name=${encodeURIComponent(o.fullName)}&background=6366f1&color=fff" class="rounded-circle shadow-sm" style="width:42px; height:42px; border: 2px solid var(--bg-primary);">
                        <div class="flex-grow-1">
                            <div class="fw-bold text-main small">${o.fullName}</div>
                            <div class="xx-small text-muted text-uppercase fw-bold letter-spacing-1">${o.occupantRole === 'Owner' || o.isPrimary ? 'Đại diện' : 'Thành viên'}</div>
                        </div>
                        <div class="text-end">
                            <div class="xx-small fw-extrabold text-primary">${o.phone || ''}</div>
                        </div>
                    </div>
                `).join('');
            }
        }

        // Services
        const serviceList = document.getElementById('service-list');
        if (serviceList) {
            if (!data.services || data.services.length === 0) {
                serviceList.innerHTML = '<div class="text-white opacity-50 xx-small">Không có dịch vụ đi kèm</div>';
            } else {
                serviceList.innerHTML = data.services.map(s => `
                    <div class="p-2 px-3 rounded-pill border border-white border-opacity-15 d-flex align-items-center gap-2"
                         style="background: rgba(255,255,255,0.08); backdrop-filter: blur(8px); transition: all 0.2s;"
                         title="${s.serviceName}">
                        <i class="bi bi-check-circle-fill text-warning xx-small"></i>
                        <span class="fw-bold text-white xx-small text-uppercase letter-spacing-1" style="font-size: 0.65rem;">${s.serviceName}:</span>
                        <span class="fw-extrabold text-white xx-small" style="font-size: 0.65rem;">${(s.unitPrice || 0).toLocaleString('vi-VN')}đ/${s.calculationType === 'metered' ? 'Số' : 'Tháng'}</span>
                    </div>
                `).join('');
            }
        }
    };

    return { init };
})();

document.addEventListener('DOMContentLoaded', RoomMgmt.init);
