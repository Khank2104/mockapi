console.log("motels-floormap.js execution started");
let _allMotels = [];
let _currentFmpRoomId = null;
let _currentSetupRooms = [];
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

            // Auto-select if global ID is set or if only 1
            if (window._globalSelectedMotelId) {
                sel.value = window._globalSelectedMotelId;
            } else if (_allMotels && _allMotels.length === 1) {
                sel.value = _allMotels[0].motelId;
                window._globalSelectedMotelId = sel.value;
            }
            loadFloormapForMotel();
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
        const setupBtn = document.getElementById('btn-open-setup');
        if (setupBtn) setupBtn.classList.add('d-none');
        return;
    }

    if (!_allMotels || _allMotels.length === 0) return;
    const motel = _allMotels.find(m => m.motelId === motelId);
    if (!motel) return;

    const setupBtn = document.getElementById('btn-open-setup');
    if (setupBtn) setupBtn.classList.remove('d-none');

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
            let html = '<div class="row g-3 justify-content-center">';
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

    document.getElementById('setup-bulk-motelId').value = motelId;

    if (!useFloorManagement) {
        const floorTabC = document.getElementById('tab-floor-container');
        if (floorTabC) floorTabC.classList.add('d-none');
        const floorSelC = document.getElementById('setup-floor-select-container');
        if (floorSelC) floorSelC.classList.add('d-none');
        const bulkFloorSelC = document.getElementById('setup-bulk-floor-select-container');
        if (bulkFloorSelC) bulkFloorSelC.classList.add('d-none');
        const fFilter = document.getElementById('filter-room-floor');
        if (fFilter) fFilter.classList.add('d-none');
        
        // Auto switch to room tab
        const roomTab = document.getElementById('room-tab');
        if (roomTab) roomTab.click();
    } else {
        const floorTabC = document.getElementById('tab-floor-container');
        if (floorTabC) floorTabC.classList.remove('d-none');
        const floorSelC = document.getElementById('setup-floor-select-container');
        if (floorSelC) floorSelC.classList.remove('d-none');
        const bulkFloorSelC = document.getElementById('setup-bulk-floor-select-container');
        if (bulkFloorSelC) bulkFloorSelC.classList.remove('d-none');
        const fFilter = document.getElementById('filter-room-floor');
        if (fFilter) fFilter.classList.remove('d-none');
        
        const floorTab = document.getElementById('floor-tab');
        if (floorTab) floorTab.click();

        loadSetupFloors(motelId);
    }

    loadSetupRooms(motelId);
    switchModule('motel-setup');
}


window.openMotelSetup = openMotelSetup;

function openCurrentMotelSetup() {
    const motelId = parseInt(document.getElementById('floormap-motel-select').value);
    if (!motelId) return;
    const motel = _allMotels.find(m => m.motelId === motelId);
    if (motel) {
        openMotelSetup(motel.motelId, motel.motelName, motel.useFloorManagement);
    }
}
window.openCurrentMotelSetup = openCurrentMotelSetup;

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
                // Populate select
                const select = document.getElementById('setup-floor-select');
                const bulkSelect = document.getElementById('setup-bulk-floor-select');
                const editSelect = document.getElementById('edit-room-floor-select');
                const filterSelect = document.getElementById('filter-room-floor');
                
                if (motel.floors.length === 0) {
                    select.innerHTML = '<option value="">Chưa có tầng nào</option>';
                    if(bulkSelect) bulkSelect.innerHTML = '<option value="">Chưa có tầng nào</option>';
                    if(editSelect) editSelect.innerHTML = '<option value="">Chưa có tầng nào</option>';
                    if(filterSelect) filterSelect.innerHTML = '<option value="all">Tất cả tầng</option>';
                } else {
                    const options = motel.floors.map(f => `<option value="${f.floorId}">${f.floorName}</option>`).join('');
                    select.innerHTML = options;
                    if(bulkSelect) bulkSelect.innerHTML = options;
                    if(editSelect) editSelect.innerHTML = options;
                    if(filterSelect) filterSelect.innerHTML = '<option value="all">Tất cả tầng</option>' + options;
                }

                // Populate table
                const tbody = document.getElementById('setup-floors-list');
                if (tbody) {
                    let rows = '';
                    motel.floors.forEach(f => {
                        rows += `
<tr>
    <td class="fw-bold">${f.floorName}</td>
    <td>
        <button class="btn btn-sm btn-outline-primary rounded-circle me-1" onclick="openEditFloor(${f.floorId}, '${f.floorName}', ${f.floorNumber})" title="Sửa tầng">
            <i class="bi bi-pencil-fill"></i>
        </button>
        <button class="btn btn-sm btn-outline-danger rounded-circle" onclick="deleteFloor(${f.floorId})" title="Xóa tầng">
            <i class="bi bi-trash-fill"></i>
        </button>
    </td>
</tr>`;
                    });
                    tbody.innerHTML = rows || '<tr><td colspan="2" class="text-center py-3 text-muted">Chưa có tầng nào.</td></tr>';
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
                
                // Combine rooms from floors and unassigned rooms (if floor management is off)
                const allRooms = [];
                if (motel.floors) {
                    motel.floors.forEach(f => {
                        if(f.rooms) f.rooms.forEach(r => allRooms.push({...r, floorName: f.floorName, floorId: f.floorId}));
                    });
                }
                if (motel.rooms) {
                    motel.rooms.forEach(r => allRooms.push({...r, floorName: 'Không', floorId: null}));
                }

                _currentSetupRooms = allRooms;
                renderRoomTable(1);
            }
        }
    } catch (e) { }
}

window.loadSetupRooms = loadSetupRooms;

function toggleRoomAddType() {
    const isSingle = document.getElementById('addSingleRoom').checked;
    if (isSingle) {
        document.getElementById('setupRoomForm').classList.remove('d-none');
        document.getElementById('setupBulkRoomForm').classList.add('d-none');
    } else {
        document.getElementById('setupRoomForm').classList.add('d-none');
        document.getElementById('setupBulkRoomForm').classList.remove('d-none');
    }
}
window.toggleRoomAddType = toggleRoomAddType;

function renderRoomTable(page = 1) {
    const tbody = document.getElementById('setup-rooms-list');
    if (!tbody) return;

    const floorFilter = document.getElementById('filter-room-floor') ? document.getElementById('filter-room-floor').value : 'all';
    const searchQuery = document.getElementById('filter-room-search') ? document.getElementById('filter-room-search').value.toLowerCase() : '';

    let filtered = _currentSetupRooms.filter(r => {
        const matchFloor = floorFilter === 'all' || r.floorId == floorFilter;
        const matchSearch = r.roomCode.toLowerCase().includes(searchQuery);
        return matchFloor && matchSearch;
    });

    // Sort by floorId, then naturally by roomCode
    filtered.sort((a, b) => {
        if(a.floorId !== b.floorId) return (a.floorId || 0) - (b.floorId || 0);
        return a.roomCode.localeCompare(b.roomCode, undefined, {numeric: true, sensitivity: 'base'});
    });

    const itemsPerPage = 10;
    const totalPages = Math.ceil(filtered.length / itemsPerPage) || 1;
    if (page > totalPages) page = totalPages;
    if (page < 1) page = 1;

    const startIdx = (page - 1) * itemsPerPage;
    const endIdx = startIdx + itemsPerPage;
    const currentItems = filtered.slice(startIdx, endIdx);

    const infoEl = document.getElementById('room-pagination-info');
    if (infoEl) infoEl.innerText = `Hiển thị ${currentItems.length > 0 ? startIdx + 1 : 0} - ${Math.min(endIdx, filtered.length)} / ${filtered.length} phòng`;

    let rows = '';
    currentItems.forEach(r => {
        rows += `
<tr>
<td>${r.floorName || 'N/A'}</td>
<td class="fw-bold">${r.roomCode}</td>
<td>${r.area} m2</td>
<td class="text-end">
<button class="btn btn-sm btn-outline-primary rounded-circle me-1" onclick="openEditRoom(${r.roomId}, '${r.roomCode}', ${r.area}, ${r.floorId})" title="Sửa phòng">
    <i class="bi bi-pencil-fill"></i>
</button>
<button class="btn btn-sm btn-outline-secondary rounded-pill me-1" onclick="openRoomDetails(${r.roomId}, '${r.roomCode}')">
    <i class="bi bi-gear-fill me-1"></i> Phí dịch vụ
</button>
<button class="btn btn-sm btn-outline-danger rounded-circle" onclick="deleteRoom(${r.roomId})" title="Xóa phòng">
    <i class="bi bi-trash-fill"></i>
</button>
</td>
</tr>`;
    });

    if (!rows) tbody.innerHTML = '<tr><td colspan="4" class="text-center py-3 text-muted">Không tìm thấy phòng nào.</td></tr>';
    else tbody.innerHTML = rows;

    // Render pagination nav
    let navHtml = '';
    navHtml += `<li class="page-item ${page === 1 ? 'disabled' : ''}"><a class="page-link" href="#" onclick="event.preventDefault(); renderRoomTable(${page - 1})">Trước</a></li>`;
    for (let i = 1; i <= totalPages; i++) {
        if (totalPages > 7 && i > 2 && i < totalPages - 1 && Math.abs(i - page) > 1) {
            if (i === 3 || i === totalPages - 2) navHtml += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
            continue;
        }
        navHtml += `<li class="page-item ${i === page ? 'active' : ''}"><a class="page-link" href="#" onclick="event.preventDefault(); renderRoomTable(${i})">${i}</a></li>`;
    }
    navHtml += `<li class="page-item ${page === totalPages ? 'disabled' : ''}"><a class="page-link" href="#" onclick="event.preventDefault(); renderRoomTable(${page + 1})">Sau</a></li>`;
    
    const navEl = document.getElementById('room-pagination-nav');
    if (navEl) navEl.innerHTML = navHtml;
}
window.renderRoomTable = renderRoomTable;

async function submitSetupBulkRoom() {
    const form = document.getElementById('setupBulkRoomForm');
    if (!form.reportValidity()) return;
    
    const btn = document.getElementById('btn-bulk-add');
    const loading = document.getElementById('room-table-loading');
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang tạo...';
    if(loading) loading.classList.remove('d-none');
    
    const data = Object.fromEntries(new FormData(form).entries());
    const motelId = parseInt(data.MotelId);
    const floorId = data.FloorId ? parseInt(data.FloorId) : null;
    const quantity = parseInt(data.Quantity);
    const area = parseFloat(data.Area);
    let startCode = data.StartCode.trim();

    // extract numeric suffix if any
    let prefix = startCode;
    let startNum = 1;
    const match = startCode.match(/^(.*?)(\d+)$/);
    if(match) {
        prefix = match[1];
        startNum = parseInt(match[2]);
    } else {
        startNum = 1;
    }

    try {
        let successCount = 0;
        for (let i = 0; i < quantity; i++) {
            let roomCode = '';
            if (match) {
                const len = match[2].length;
                roomCode = prefix + String(startNum + i).padStart(len, '0');
            } else {
                roomCode = prefix + (startNum + i);
            }
            
            const payload = {
                MotelId: motelId,
                FloorId: floorId,
                RoomCode: roomCode,
                Area: area,
                Status: 'Vacant'
            };

            const response = await fetch('/api/MotelManagement/CreateRoom', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            const result = await response.json();
            if (result.success) successCount++;
        }
        
        showPremiumToast("Hoàn tất", `Đã tạo thành công ${successCount}/${quantity} phòng.`, "success");
        form.reset();
        loadSetupRooms(motelId);
        loadMotelsData();
    } catch (e) { 
        alert("Lỗi kết nối Server trong quá trình tạo phòng."); 
    } finally {
        btn.disabled = false;
        btn.innerHTML = '<i class="bi bi-magic me-1"></i> Tạo Hàng Loạt';
        if(loading) loading.classList.add('d-none');
    }
}
window.submitSetupBulkRoom = submitSetupBulkRoom;

// === Edit/Delete Motel ===
function openEditMotel() {
    const motelId = parseInt(document.getElementById('setup-motelId').value);
    if(!motelId || !_allMotels) return;
    const motel = _allMotels.find(m => m.motelId === motelId);
    if(!motel) return;

    document.getElementById('edit-motel-id').value = motel.motelId;
    document.getElementById('edit-motel-name').value = motel.motelName;
    document.getElementById('edit-motel-address').value = motel.address;
    document.getElementById('edit-motel-description').value = motel.description || '';
    document.getElementById('edit-motel-useFloors').checked = motel.useFloorManagement;

    new bootstrap.Modal(document.getElementById('editMotelModal')).show();
}

window.openEditMotel = openEditMotel;

async function submitEditMotel() {
    const form = document.getElementById('editMotelForm');
    if (!form.reportValidity()) return;
    
    const motelId = document.getElementById('edit-motel-id').value;
    const data = {
        MotelName: document.getElementById('edit-motel-name').value,
        Address: document.getElementById('edit-motel-address').value,
        Description: document.getElementById('edit-motel-description').value,
        UseFloorManagement: document.getElementById('edit-motel-useFloors').checked
    };

    try {
        const response = await fetch(`/api/MotelManagement/UpdateMotel/${motelId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        const result = await response.json();
        if (result.success) {
            showPremiumToast("Thành công", "Đã cập nhật thông tin khu trọ.", "success");
            bootstrap.Modal.getInstance(document.getElementById('editMotelModal')).hide();
            loadMotelsData();
            document.getElementById('setup-motel-name').innerText = data.MotelName;
            
            // Re-evaluate floor UI based on toggle
            openMotelSetup(motelId, data.MotelName, data.UseFloorManagement);
        } else {
            alert(result.message);
        }
    } catch (e) { alert("Lỗi kết nối Server"); }
}

window.submitEditMotel = submitEditMotel;

// === Edit/Delete Floor ===
function openEditFloor(floorId, floorName, floorNumber) {
    document.getElementById('edit-floor-id').value = floorId;
    document.getElementById('edit-floor-motelId').value = document.getElementById('setup-motelId').value;
    document.getElementById('edit-floor-name').value = floorName;
    document.getElementById('edit-floor-number').value = floorNumber;
    new bootstrap.Modal(document.getElementById('editFloorModal')).show();
}

window.openEditFloor = openEditFloor;

async function submitEditFloor() {
    const form = document.getElementById('editFloorForm');
    if (!form.reportValidity()) return;
    const data = Object.fromEntries(new FormData(form).entries());
    const floorId = data.FloorId;

    try {
        const response = await fetch(`/api/MotelManagement/UpdateFloor/${floorId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        const result = await response.json();
        if (result.success) {
            showPremiumToast("Thành công", "Đã cập nhật tầng.", "success");
            bootstrap.Modal.getInstance(document.getElementById('editFloorModal')).hide();
            loadSetupFloors(data.MotelId);
            loadMotelsData();
        } else {
            alert(result.message);
        }
    } catch (e) { alert("Lỗi kết nối Server"); }
}

window.submitEditFloor = submitEditFloor;

async function deleteFloor(floorId) {
    if (!confirm("Bạn có chắc chắn muốn xóa tầng này không? Các phòng thuộc tầng cũng sẽ bị ảnh hưởng.")) return;
    try {
        const response = await fetch(`/api/MotelManagement/DeleteFloor/${floorId}`, { method: 'DELETE' });
        const result = await response.json();
        if (result.success) {
            showPremiumToast("Thành công", "Đã xóa tầng.", "success");
            const motelId = document.getElementById('setup-motelId').value;
            loadSetupFloors(motelId);
            loadSetupRooms(motelId);
            loadMotelsData();
        } else {
            showPremiumToast("Lỗi", result.message, "danger");
        }
    } catch (e) { alert("Lỗi kết nối Server"); }
}

window.deleteFloor = deleteFloor;

// === Edit/Delete Room ===
function openEditRoom(roomId, roomCode, area, floorId) {
    document.getElementById('edit-room-id').value = roomId;
    document.getElementById('edit-room-motelId').value = document.getElementById('setup-motelId').value;
    document.getElementById('edit-room-code').value = roomCode;
    document.getElementById('edit-room-area').value = area;
    
    // Set floor select
    const motelId = document.getElementById('setup-motelId').value;
    const motel = _allMotels.find(m => m.motelId == motelId);
    const container = document.getElementById('edit-room-floor-select-container');
    const select = document.getElementById('edit-room-floor-select');
    
    if (motel && motel.useFloorManagement) {
        container.classList.remove('d-none');
        if (floorId) select.value = floorId;
    } else {
        container.classList.add('d-none');
        select.value = "";
    }
    
    new bootstrap.Modal(document.getElementById('editRoomModal')).show();
}

window.openEditRoom = openEditRoom;

async function submitEditRoom() {
    const form = document.getElementById('editRoomForm');
    if (!form.reportValidity()) return;
    const data = Object.fromEntries(new FormData(form).entries());
    const roomId = data.RoomId;
    
    if(!data.FloorId || data.FloorId === "") {
        data.FloorId = null;
    } else {
        data.FloorId = parseInt(data.FloorId);
    }
    data.Area = parseFloat(data.Area);

    try {
        const response = await fetch(`/api/MotelManagement/UpdateRoom/${roomId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        const result = await response.json();
        if (result.success) {
            showPremiumToast("Thành công", "Đã cập nhật phòng.", "success");
            bootstrap.Modal.getInstance(document.getElementById('editRoomModal')).hide();
            loadSetupRooms(data.MotelId);
            loadMotelsData();
        } else {
            alert(result.message);
        }
    } catch (e) { alert("Lỗi kết nối Server"); }
}

window.submitEditRoom = submitEditRoom;

async function deleteRoom(roomId) {
    if (!confirm("Bạn có chắc chắn muốn xóa phòng này không?")) return;
    try {
        const response = await fetch(`/api/MotelManagement/DeleteRoom/${roomId}`, { method: 'DELETE' });
        const result = await response.json();
        if (result.success) {
            showPremiumToast("Thành công", "Đã xóa phòng.", "success");
            const motelId = document.getElementById('setup-motelId').value;
            loadSetupRooms(motelId);
            loadMotelsData();
        } else {
            showPremiumToast("Lỗi", result.message, "danger");
        }
    } catch (e) { alert("Lỗi kết nối Server"); }
}

window.deleteRoom = deleteRoom;
console.log("motels-floormap.js execution finished");
