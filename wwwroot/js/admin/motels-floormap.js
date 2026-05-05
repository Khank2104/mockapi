console.log("motels-floormap.js execution started");
let _allMotels = [];
let _currentFmpRoomId = null;
const MANDATORY_CODES = ['ELECTRIC', 'WATER', 'WIFI', 'TRASH', 'CLEAN', 'ELECTRICITY'];

function showAddMotelModal() {
    new bootstrap.Modal(document.getElementById('addMotelModal')).show();
}


window.showAddMotelModal = showAddMotelModal;
async function submitAddMotel() {
    const form = document.getElementById('addMotelForm');
    const data = Object.fromEntries(new FormData(form).entries());
    data.UseFloorManagement = document.getElementById('useFloors').checked;

    try {
        const response = await fetch('/api/MotelManagement/CreateMotel', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        const result = await response.json();
        if (result.success) {
            showPremiumToast("Thành công", "Khu trọ mới đã được tạo. Hãy thiết lập phòng/tầng.", "success");
            bootstrap.Modal.getInstance(document.getElementById('addMotelModal')).hide();
            loadMotelsData();
            openMotelSetup(result.data.motelId, result.data.motelName, result.data.useFloorManagement);
        } else {
            alert(result.message);
        }
    } catch (e) {
        alert("Lỗi kết nối Server.");
    }
}


window.submitAddMotel = submitAddMotel;
async function loadMotelsData() {
    const container = document.getElementById('floormap-container');
    if (container) container.innerHTML = '<div class="text-center py-5"><div class="spinner-border text-primary" role="status"></div><p class="mt-2 text-muted">Đang tải sơ đồ...</p></div>';

    // Populate motel select
    try {
        const response = await fetch('/api/MotelManagement/MyMotels');

        if (response.status === 401 || response.status === 403) {
            console.error("Session expired or unauthorized");
            showPremiumToast("Hết phiên làm việc", "Vui lòng đăng nhập lại.", "danger");
            setTimeout(() => window.location.href = '/Account/Login', 2000);
            return;
        }

        if (!response.ok) {
            throw new Error(`Server returned ${response.status}: ${response.statusText}`);
        }

        const result = await response.json();
        if (result.success && result.data) {
            _allMotels = result.data;
            const sel = document.getElementById('floormap-motel-select');
            if (!sel) return;
            sel.innerHTML = '<option value="">-- Chọn khu trọ --</option>' +
                _allMotels.map(m => `<option value="${m.motelId}">${m.motelName}</option>`).join('');

            // Auto-select if only 1, otherwise reset container state
            if (_allMotels && _allMotels.length === 1) {
                sel.value = _allMotels[0].motelId;
                loadFloormapForMotel();
            } else {
                loadFloormapForMotel(); // This will clear the spinner and show "Select Motel" message
            }
        } else {
            console.error("Failed to load motels:", result.message);
            const container = document.getElementById('floormap-container');
            if (container) container.innerHTML = `<div class="text-center py-5 text-danger"><i class="bi bi-exclamation-triangle fs-1 d-block mb-3"></i><p>Không thể tải dữ liệu: ${result.message || 'Lỗi server'}</p></div>`;
        }
    } catch (e) {
        console.error("loadMotelsData Error Detail:", e);
        document.getElementById('floormap-container').innerHTML = `<div class="text-center py-5 text-danger"><i class="bi bi-exclamation-triangle fs-1 d-block mb-3"></i><p>Lỗi kết nối Server: ${e.message}</p></div>`;
    }
}


window.loadMotelsData = loadMotelsData;

function loadFloormapForMotel() {
    const motelId = parseInt(document.getElementById('floormap-motel-select').value);
    const container = document.getElementById('floormap-container');
    closeFmpPanel();

    if (!motelId) {
        container.innerHTML = '<div class="text-center py-5 text-muted"><i class="bi bi-houses fs-1 d-block mb-3 opacity-25"></i><p>Chọn khu trọ để xem sơ đồ</p></div>';
        return;
    }

    if (!_allMotels || _allMotels.length === 0) return;
    const motel = _allMotels.find(m => m.motelId === motelId);
    if (!motel) return;

    if (!motel.useFloorManagement) {
        if (!motel.rooms || motel.rooms.length === 0) {
            container.innerHTML = `
        <div class="empty-state">
            <i class="bi bi-building"></i>
            <h3>Chưa có phòng nào</h3>
            <p class="text-muted mb-4">Hãy thiết lập phòng trong mục Thiết lập Khu trọ.</p>
            <button class="btn btn-premium" onclick="openMotelSetup(${motel.motelId}, '${motel.motelName}', false)">
                <i class="bi bi-gear me-2"></i>Thiết lập ngay
            </button>
        </div>`;
        } else {
            let html = '<div class="row g-3">';
            motel.rooms.forEach(r => {
                const statusColor = r.status === 'Occupied' ? '#22c55e' : r.status === 'Maintenance' ? '#f59e0b' : '#6366f1';
                const statusLabel = r.status === 'Occupied' ? 'Đang ở' : r.status === 'Maintenance' ? 'Bảo trì' : 'Trống';
                const statusClass = r.status === 'Occupied' ? 'room-card-occupied' : r.status === 'Maintenance' ? 'room-card-maintenance' : 'room-card-vacant';
                html += `
            <div class="col-6 col-md-4 col-lg-3 col-xl-2">
                <div class="room-card ${statusClass}" onclick="openFmpRoom(${r.roomId}, '${r.roomCode}', '${statusLabel}')" title="Phòng ${r.roomCode}">
                    <div class="room-card-icon"><i class="bi bi-door-closed-fill"></i></div>
                    <div class="room-card-code">${r.roomCode}</div>
                    <div class="room-card-status" style="color:${statusColor}">
                        <i class="bi bi-circle-fill me-1" style="font-size:0.4rem"></i>${statusLabel}
                    </div>
                    ${r.status === 'Occupied' ? '<div class="room-card-badge"><i class="bi bi-person-fill"></i></div>' : ''}
                </div>
            </div>`;
            });
            html += '</div>';
            container.innerHTML = html;
        }
        return;
    }

    if (!motel.floors || motel.floors.length === 0) {
        container.innerHTML = `
<div class="empty-state">
<i class="bi bi-building"></i>
<h3>Chưa có tầng nào</h3>
<p class="text-muted mb-4">Hãy thiết lập tầng và phòng trong mục Thiết lập Khu trọ.</p>
<button class="btn btn-premium" onclick="openMotelSetup(${motel.motelId}, '${motel.motelName}', ${motel.useFloorManagement})">
<i class="bi bi-gear me-2"></i>Thiết lập ngay
</button>
</div>`;
        return;
    }

    // Build floor tabs + room grids
    let html = '<div class="mb-3">';
    html += '<div class="d-flex gap-2 flex-wrap mb-4" id="floor-tabs">';
    motel.floors.forEach((f, i) => {
        html += `<button class="btn rounded-pill px-4 fw-bold ${i === 0 ? 'btn-premium' : 'btn-outline-secondary'}" onclick="switchFloorTab(${f.floorId}, this)">${f.floorName || 'Tầng ' + f.floorNumber} <span class="badge ${i === 0 ? 'bg-white text-primary' : 'bg-primary bg-opacity-10 text-primary'} ms-2">${f.rooms.length}</span></button>`;
    });
    html += '</div>';

    motel.floors.forEach((f, i) => {
        const rooms = f.rooms.slice(0, 10); // max 10/floor
        html += `<div class="floor-panel" id="floor-panel-${f.floorId}" style="display:${i === 0 ? 'block' : 'none'}">`;
        html += `<div class="d-flex align-items-center gap-3 mb-3">`;
        html += `<h5 class="fw-bold mb-0">${f.floorName || 'Tầng ' + f.floorNumber}</h5>`;
        html += `<span class="text-muted small">${f.rooms.length} phòng${f.rooms.length > 10 ? ' (hiển thị 10 đầu)' : ''}</span>`;
        html += '</div>';
        html += '<div class="row g-3">';
        if (rooms.length === 0) {
            html += '<div class="col-12 text-center py-4 text-muted small">Tầng này chưa có phòng nào.</div>';
        } else {
            rooms.forEach(r => {
                const statusColor = r.status === 'Occupied' ? '#22c55e' : r.status === 'Maintenance' ? '#f59e0b' : '#6366f1';
                const statusLabel = r.status === 'Occupied' ? 'Đang ở' : r.status === 'Maintenance' ? 'Bảo trì' : 'Trống';
                const statusClass = r.status === 'Occupied' ? 'room-card-occupied' : r.status === 'Maintenance' ? 'room-card-maintenance' : 'room-card-vacant';
                html += `
<div class="col-6 col-md-4 col-lg-3 col-xl-2">
<div class="room-card ${statusClass}" onclick="openFmpRoom(${r.roomId}, '${r.roomCode}', '${statusLabel}')" title="Phòng ${r.roomCode}">
<div class="room-card-icon"><i class="bi bi-door-closed-fill"></i></div>
<div class="room-card-code">${r.roomCode}</div>
<div class="room-card-status" style="color:${statusColor}">
<i class="bi bi-circle-fill me-1" style="font-size:0.4rem"></i>${statusLabel}
</div>
${r.status === 'Occupied' ? '<div class="room-card-badge"><i class="bi bi-person-fill"></i></div>' : ''}
</div>
</div>`;
            });
        }
        html += '</div></div>';
    });
    html += '</div>';
    container.innerHTML = html;
}


window.loadFloormapForMotel = loadFloormapForMotel;
function switchFloorTab(floorId, btn) {
    document.querySelectorAll('.floor-panel').forEach(p => p.style.display = 'none');
    document.querySelectorAll('#floor-tabs button').forEach(b => {
        b.className = 'btn rounded-pill px-4 fw-bold btn-outline-secondary';
        const badge = b.querySelector('.badge');
        if (badge) badge.className = 'badge bg-primary bg-opacity-10 text-primary ms-2';
    });
    const panel = document.getElementById(`floor-panel-${floorId}`);
    if (panel) panel.style.display = 'block';
    btn.className = 'btn rounded-pill px-4 fw-bold btn-premium';
    const btnBadge = btn.querySelector('.badge');
    if (btnBadge) btnBadge.className = 'badge bg-white text-primary ms-2';
}


window.switchFloorTab = switchFloorTab;
async function openFmpRoom(roomId, roomCode, statusLabel) {
    _currentFmpRoomId = roomId;
    document.getElementById('fmp-room-code').innerText = roomCode;
    document.getElementById('fmp-room-status').innerText = statusLabel;
    document.getElementById('fmp-rent').innerText = 'Đang tải...';
    document.getElementById('fmp-deposit').innerText = '--';
    document.getElementById('fmp-occupants-list').innerHTML = '<div class="text-center py-3 text-muted x-small">Đang tải...</div>';
    document.getElementById('fmp-services-list').innerHTML = '<div class="text-muted small text-center py-3">Đang tải...</div>';
    const roomPanel = document.getElementById('floormap-room-panel');
    if (roomPanel) {
        roomPanel.classList.remove('d-none');
        roomPanel.scrollIntoView({ behavior: 'smooth' });
    }

    try {
        const [settingRes, serviceRes, occupantRes] = await Promise.all([
            fetch(`/api/MotelManagement/GetRoomSettings/${roomId}`),
            fetch(`/api/MotelManagement/GetRoomServices/${roomId}`),
            fetch(`/api/MotelManagement/GetRoomOccupants/${roomId}`)
        ]);
        const settingResult = await settingRes.json();
        const serviceResult = await serviceRes.json();
        const occupantResult = await occupantRes.json();

        // Financial Info
        if (settingResult.success && settingResult.data) {
            const s = settingResult.data;
            document.getElementById('fmp-rent').innerText = s.baseRent ? s.baseRent.toLocaleString() + 'đ' : 'Chưa thiết lập';
            document.getElementById('fmp-deposit').innerText = s.depositAmount ? s.depositAmount.toLocaleString() + 'đ' : 'Chưa thiết lập';

            const editBtn = document.getElementById('fmp-edit-contract-btn');
            if (editBtn) {
                if (statusLabel.includes('Đang ở')) {
                    editBtn.classList.remove('d-none');
                } else {
                    editBtn.classList.add('d-none');
                }
            }

            // Services
            if (serviceResult.success && serviceResult.data) {
                const services = serviceResult.data;
                const listEl = document.getElementById('fmp-services-list');
                listEl.innerHTML = services.filter(s => s.isActive).map(s => {
                    const icon = getServiceIcon(s.serviceCode);
                    return `
<div class="d-flex align-items-center justify-content-between p-2 mb-1 border-bottom border-light">
<div class="d-flex align-items-center gap-2">
<i class="bi ${icon} text-primary small"></i>
<span class="small fw-bold">${s.serviceName}</span>
</div>
<span class="badge bg-success bg-opacity-10 text-success x-small" style="font-size:0.6rem">ĐANG DÙNG</span>
</div>`;
                }).join('') || '<div class="text-center py-3 text-muted x-small">Chưa có dịch vụ đang dùng.</div>';
            }

            // Occupants
            if (occupantResult.success && occupantResult.data) {
                const occs = occupantResult.data;
                const listEl = document.getElementById('fmp-occupants-list');
                listEl.innerHTML = occs.map(o => `
            <div class="d-flex align-items-center justify-content-between p-2 rounded-3 bg-white border x-small">
            <div class="d-flex align-items-center gap-2">
            <div class="bg-primary bg-opacity-10 text-primary rounded-circle d-flex align-items-center justify-content-center" style="width:24px;height:24px;font-size:0.7rem">
            <i class="bi bi-person-fill"></i>
            </div>
            <div>
            <div class="fw-bold">${o.tenantName}</div>
            <div class="text-muted" style="font-size:0.6rem">${o.role === 'Owner' ? 'Đại diện' : 'Thành viên'}</div>
            </div>
            </div>
            <button class="btn btn-sm btn-link text-danger p-0" onclick="removeOccupantFromFmp(${o.roomOccupantId}, '${o.tenantName}')">
            <i class="bi bi-x-circle"></i>
            </button>
            </div>
            `).join('') || '<div class="text-center py-3 text-muted x-small">Chưa có người ở.</div>';
            }
        }
    } catch (e) {
        showPremiumToast("Lỗi", "Không thể đồng bộ dữ liệu phòng.", "danger");
    }
}


window.openFmpRoom = openFmpRoom;
function closeFmpPanel() {
    _currentFmpRoomId = null;
    const roomPanel = document.getElementById('floormap-room-panel');
    if (roomPanel) roomPanel.classList.add('d-none');
    const saveRow = document.getElementById('fmp-save-row');
    if (saveRow) saveRow.classList.add('d-none');
}


window.closeFmpPanel = closeFmpPanel;
async function saveFmpServices() {
    // Collect checked optional services
    const checked = [...document.querySelectorAll('.fmp-svc-check')].map(cb => ({
        serviceId: parseInt(cb.dataset.svcid),
        enabled: cb.checked
    }));
    showPremiumToast('Thông tin', 'Cấu hình dịch vụ đã được ghi nhớ (kết nối dịch vụ tùy chọn sẽ được tích hợp tiếp theo).', 'info');
}



window.saveFmpServices = saveFmpServices;
function openMotelSetup(motelId, motelName, useFloorManagement) {
    document.getElementById('setup-motel-name').innerText = motelName;
    document.getElementById('setup-motelId').value = motelId;
    document.getElementById('setup-room-motelId').value = motelId;

    const floorCard = document.getElementById('setup-floor-card');
    const roomCard = document.getElementById('setup-room-card');
    const floorSelectContainer = document.getElementById('setup-floor-select-container');

    if (!useFloorManagement) {
        if (floorCard) floorCard.classList.add('d-none');
        if (roomCard) {
            roomCard.classList.remove('col-lg-8');
            roomCard.classList.add('col-lg-12');
        }
        if (floorSelectContainer) floorSelectContainer.classList.add('d-none');
    } else {
        if (floorCard) floorCard.classList.remove('d-none');
        if (roomCard) {
            roomCard.classList.remove('col-lg-12');
            roomCard.classList.add('col-lg-8');
        }
        if (floorSelectContainer) floorSelectContainer.classList.remove('d-none');
        loadSetupFloors(motelId);
    }

    loadSetupRooms(motelId);
    switchModule('motel-setup');
}


window.openMotelSetup = openMotelSetup;
async function submitSetupFloor() {
    const form = document.getElementById('setupFloorForm');
    if (!form.reportValidity()) return;
    const data = Object.fromEntries(new FormData(form).entries());
    data.MotelId = parseInt(data.MotelId);
    data.FloorNumber = parseInt(data.FloorNumber);
    data.Status = 'Active';

    try {
        const response = await fetch('/api/MotelManagement/CreateFloor', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        const result = await response.json();
        if (result.success) {
            showPremiumToast("Thành công", "Đã thêm tầng mới.", "success");
            form.reset();
            document.getElementById('setup-motelId').value = data.MotelId;
            loadSetupFloors(data.MotelId);
            loadMotelsData();
        } else {
            alert(result.message);
        }
    } catch (e) { alert("Lỗi kết nối Server"); }
}


window.submitSetupFloor = submitSetupFloor;
async function submitSetupRoom() {
    const form = document.getElementById('setupRoomForm');
    if (!form.reportValidity()) return;
    const data = Object.fromEntries(new FormData(form).entries());
    data.MotelId = parseInt(data.MotelId);
    data.FloorId = data.FloorId ? parseInt(data.FloorId) : null;
    data.Area = parseFloat(data.Area);
    data.Status = 'Vacant';

    try {
        const response = await fetch('/api/MotelManagement/CreateRoom', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        const result = await response.json();
        if (result.success) {
            showPremiumToast("Thành công", "Đã thêm phòng mới.", "success");
            form.reset();
            document.getElementById('setup-room-motelId').value = data.MotelId;
            loadSetupRooms(data.MotelId);
            loadMotelsData();
        } else {
            alert(result.message);
        }
    } catch (e) { alert("Lỗi kết nối Server"); }
}


window.submitSetupRoom = submitSetupRoom;
async function loadSetupFloors(motelId) {
    try {
        const response = await fetch('/api/MotelManagement/MyMotels');
        const result = await response.json();
        if (result.success) {
            const motel = result.data.find(m => m.motelId == motelId);
            if (motel) {
                const select = document.getElementById('setup-floor-select');
                if (motel.floors.length === 0) {
                    select.innerHTML = '<option value="">Chưa có tầng nào</option>';
                } else {
                    select.innerHTML = motel.floors.map(f => `<option value="${f.floorId}">${f.floorName}</option>`).join('');
                }
            }
        }
    } catch (e) { }
}


window.loadSetupFloors = loadSetupFloors;
async function loadSetupRooms(motelId) {
    try {
        const response = await fetch('/api/MotelManagement/MyMotels');
        const result = await response.json();
        if (result.success) {
            const motel = result.data.find(m => m.motelId == motelId);
            if (motel) {
                const tbody = document.getElementById('setup-rooms-list');
                let rows = '';
                motel.floors.forEach(f => {
                    f.rooms.forEach(r => {
                        rows += `
<tr>
<td>${f.floorName || 'N/A'}</td>
<td class="fw-bold">${r.roomCode}</td>
<td>${r.area} m2</td>
<td>
<button class="btn btn-sm btn-outline-primary rounded-pill" onclick="openRoomDetails(${r.roomId}, '${r.roomCode}')">
<i class="bi bi-gear-fill me-1"></i> Thiết lập phí dịch vụ
</button>
</td>
</tr>
`;
                    });
                });
                if (!rows) tbody.innerHTML = '<tr><td colspan="4" class="text-center py-3 text-muted">Chưa có phòng nào.</td></tr>';
                else tbody.innerHTML = rows;
            }
        }
    } catch (e) { }
}


window.loadSetupRooms = loadSetupRooms;
console.log("motels-floormap.js execution finished");
