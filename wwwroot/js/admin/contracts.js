let currentContractPage = 1;
        let totalContractPages = 1;

        async function loadContractsData(page = 1, motelId = window._globalSelectedMotelId) {
            currentContractPage = page;
            const container = document.getElementById('contracts-list-container');
            
            // UI indicator for selected motel
            const badge = document.getElementById('contract-motel-badge');
            const nameSpan = document.getElementById('contract-selected-motel-name');
            const filterEl = document.getElementById('contract-motel-filter');
            const motelIdVal = motelId || (filterEl ? filterEl.value : 0);

            // Show loading spinner while fetching
            container.innerHTML = '<div class="col-12 text-center py-5"><div class="spinner-border text-primary" role="status"></div><p class="mt-2 text-muted">Đang tải danh sách hợp đồng...</p></div>';

            if (motelIdVal && motelIdVal != "0" && window._allMotelsCache && window._allMotelsCache.length > 0) {
                const motel = window._allMotelsCache.find(m => m.motelId == motelIdVal);
                if (motel) {
                    if (nameSpan) nameSpan.innerText = motel.motelName;
                    if (badge) badge.classList.remove('d-none');
                }
            } else {
                if (badge) badge.classList.add('d-none');
            }

            try {
                const response = await fetch(`/api/OccupancyManagement/GetAllContracts?motelId=${motelIdVal}&page=${page}&pageSize=10`);
                const result = await response.json();
                
                if (result.success && result.data) {
                    const { items, totalCount, totalPages, currentPage } = result.data;
                    totalContractPages = totalPages;

                    if (!items || items.length === 0) {
                        container.innerHTML = `
                            <div class="col-12 text-center py-5">
                                <div class="empty-state border-0 bg-transparent">
                                    <i class="bi bi-file-earmark-x fs-1 opacity-25"></i>
                                    <h4 class="text-muted mt-3">Chưa có hợp đồng nào</h4>
                                    <p class="text-muted small">Khu trọ này hiện chưa có dữ liệu hợp đồng nào được tạo.</p>
                                </div>
                            </div>`;
                        updateContractPaginationUI(0, 0, 0, 1, 1);
                        return;
                    }

                    container.innerHTML = items.map(c => `
                        <div class="col-md-6 col-lg-4 col-xl-3 animate-fade-in">
                            <div class="glass-card contract-card p-4 h-100 d-flex flex-column">
                                <div class="d-flex justify-content-between align-items-start mb-3">
                                    <div>
                                        <div class="badge bg-primary bg-opacity-10 text-primary rounded-pill mb-2 px-3">Phòng ${c.roomCode || 'N/A'}</div>
                                        <h5 class="fw-bold mb-0">${c.tenantName || 'N/A'}</h5>
                                    </div>
                                    <span class="status-pill ${c.contractStatus === 'Active' ? 'status-active' : c.contractStatus === 'Waiting' ? 'status-waiting' : 'status-locked'}">
                                        ${c.contractStatus === 'Active' ? 'Hiệu lực' : c.contractStatus === 'Waiting' ? 'Sắp đi' : 'Kết thúc'}
                                    </span>
                                </div>
                                
                                <div class="flex-grow-1 p-3 bg-light rounded-4 mb-3">
                                    <div class="card-info-row d-flex justify-content-between mb-2">
                                        <span class="text-muted small">Giá thuê:</span>
                                        <span class="fw-bold text-primary">${(c.monthlyRent || 0).toLocaleString()}đ</span>
                                    </div>
                                    <div class="card-info-row d-flex justify-content-between mb-2">
                                        <span class="text-muted small">Tiền cọc:</span>
                                        <span class="fw-bold text-dark">${(c.depositAmount || 0).toLocaleString()}đ</span>
                                    </div>
                                    <div class="card-info-row d-flex justify-content-between border-0">
                                        <span class="text-muted small">Thời hạn:</span>
                                        <span class="small fw-bold">${c.startDate ? new Date(c.startDate).toLocaleDateString('vi-VN') : 'N/A'} - ${c.endDate ? new Date(c.endDate).toLocaleDateString('vi-VN') : 'N/A'}</span>
                                    </div>
                                </div>
                                
                                <div class="d-flex gap-2 mt-auto">
                                    <button class="btn btn-sm btn-outline-primary rounded-pill flex-grow-1" onclick="openEditContractFromList(${c.roomId}, '${c.roomCode}')">
                                        <i class="bi bi-pencil-square me-1"></i> Sửa
                                    </button>
                                    ${c.contractStatus === 'Active' ? `
                                        <button class="btn btn-sm btn-outline-danger rounded-pill px-3" onclick="terminateContract(${c.contractId})" title="Chấm dứt">
                                            <i class="bi bi-x-circle"></i>
                                        </button>` : ''}
                                </div>
                            </div>
                        </div>
                    `).join('');

                    const start = (currentPage - 1) * 10 + 1;
                    const end = start + items.length - 1;
                    updateContractPaginationUI(start, end, totalCount, currentPage, totalPages);
                } else {
                    throw new Error("Invalid API response");
                }
            } catch (e) { 
                console.error("Contracts load error", e); 
                container.innerHTML = `
                    <div class="col-12 text-center py-5">
                        <div class="alert alert-soft-danger d-inline-block px-5 rounded-4">
                            <i class="bi bi-exclamation-triangle fs-4 d-block mb-2"></i>
                            Lỗi khi tải danh sách hợp đồng. Vui lòng thử lại.
                        </div>
                    </div>`;
            }
        }

        function updateContractPaginationUI(start, end, total, current, totalPages) {
            const rangeEl = document.getElementById('contract-range');
            const totalEl = document.getElementById('contract-total');
            const currentEl = document.getElementById('contract-current-page');
            
            if (rangeEl) rangeEl.innerText = `${start}-${end}`;
            if (totalEl) totalEl.innerText = total;
            if (currentEl) currentEl.innerText = current;
            
            document.getElementById('contract-prev-btn')?.classList.toggle('disabled', current <= 1);
            document.getElementById('contract-next-btn')?.classList.toggle('disabled', current >= totalPages);
        }

        function changeContractPage(delta) {
            const next = currentContractPage + delta;
            if (next >= 1 && next <= totalContractPages) {
                loadContractsData(next, window._globalSelectedMotelId);
            }
        }

        async function loadMotelsForContractFilter() {
            try {
                const response = await fetch('/api/MotelManagement/MyMotels');
                const result = await response.json();
                if (result.success) {
                    const select = document.getElementById('contract-motel-filter');
                    if (select) {
                        select.innerHTML = '<option value="0">-- Tất cả khu trọ --</option>' + 
                            result.data.map(m => `<option value="${m.motelId}">${m.motelName}</option>`).join('');
                        
                        // Sync with global selection if applicable
                        if (window._globalSelectedMotelId) {
                            select.value = window._globalSelectedMotelId;
                        }
                    }
                }
            } catch (e) {}
        }

        // Initialize motels filter on module load
        document.addEventListener('DOMContentLoaded', loadMotelsForContractFilter);

        // ---- Contract 3-Step State ----
        let _contractMotels = [];
        let _contractOccupants = []; // {tenantId, tenantName, role}
        let _contractRoomId = null;
        let _contractRoomCode = '';


window.loadContractsData = loadContractsData;
        async function showAddContractModal() {
            // Reset state
            _contractMotels = [];
            _contractOccupants = [];
            _contractRoomId = null;
            _contractRoomCode = '';
            document.getElementById('c-roomPreview').classList.add('d-none');
            document.getElementById('c-primaryTenantSelectGlobal').innerHTML = '<option value="">-- Chọn khách từ hệ thống --</option>';
            document.getElementById('c-tenantInfoPreview').classList.add('d-none');
            document.getElementById('summary-room').innerText = '--';
            document.getElementById('summary-primary').innerText = '--';

            // Reset to step 1
            contractGoStep(1, true);
            new bootstrap.Modal(document.getElementById('addContractModal')).show();

            // Load data in parallel
            try {
                const [mRes, tRes, sRes] = await Promise.all([
                    fetch('/api/MotelManagement/MyMotels'),
                    fetch('/api/TenantManagement/GetAllTenants'),
                    fetch('/api/MotelManagement/GetGlobalServices')
                ]);
                const mResult = await mRes.json();
                const tResult = await tRes.json();
                const sResult = await sRes.json();

                if (mResult.success) {
                    _contractMotels = mResult.data;
                    const motelSel = document.getElementById('c-motelSelect');
                    motelSel.innerHTML = '<option value="">-- Chọn khu trọ --</option>' +
                        _contractMotels.map(m => `<option value="${m.motelId}">${m.motelName} (${m.address || ''})</option>`).join('');
                }

                if (tResult.success) {
                    // Lọc những người CHƯA CÓ CHỖ Ở (tức là không phải trạng thái Staying)
                    const items = tResult.data.items || tResult.data;
                    const availableTenants = items.filter(t => t.status !== 'Staying' && t.tenantStatus !== 'Staying');
                    
                    const opts = '<option value="">-- Chọn khách từ hệ thống --</option>' +
                        availableTenants.map(t => `<option value="${t.tenantId}" data-name="${t.fullName}" data-phone="${t.phone || ''}">${t.fullName} - ${t.phone || ''}</option>`).join('');
                    document.getElementById('c-primaryTenantSelectGlobal').innerHTML = opts;
                    
                    if (availableTenants.length === 0) {
                        showPremiumToast("Thông báo", "Tất cả khách thuê hiện tại đều đã có hợp đồng. Vui lòng thêm khách thuê mới.", "info");
                    }
                }

                if (sResult.success) {
                    const container = document.getElementById('c-services-checkboxes');
                    container.innerHTML = sResult.data.map(s => {
                        const sName = (s.serviceName || "").toLowerCase();
                        const isMandatory = sName.includes("điện") || sName.includes("nước");
                        return `
                        <div class="col-md-6">
                            <div class="form-check p-2 border rounded-4 ${isMandatory ? 'bg-primary bg-opacity-10 border-primary border-opacity-25' : 'bg-light bg-opacity-50'}">
                                <input class="form-check-input ms-0 me-2" type="checkbox" value="${s.serviceId}" id="svc-${s.serviceId}" 
                                       ${isMandatory ? 'checked disabled' : 'checked'}>
                                <label class="form-check-label small fw-bold" for="svc-${s.serviceId}">
                                    ${s.serviceName} ${isMandatory ? '<span class="x-small text-danger">(Bắt buộc)</span>' : `(${s.defaultPrice.toLocaleString()}đ)`}
                                </label>
                            </div>
                        </div>
                        `;
                    }).join('');
                }
            } catch (e) {
                showPremiumToast("Lỗi", "Không thể tải dữ liệu.", "danger");
            }
        }


window.showAddContractModal = showAddContractModal;
        function contractGoStep(step, reset = false) {
            [1, 2, 3].forEach(i => {
                document.getElementById(`contract-step-${i}`).classList.add('d-none');
                document.getElementById(`step-badge-${i}`).classList.remove('active', 'bg-primary', 'text-white');
                document.getElementById(`step-label-${i}`).classList.add('text-muted');
                document.getElementById(`step-label-${i}`).classList.remove('fw-bold');
            });
            document.getElementById(`contract-step-${step}`).classList.remove('d-none');
            document.getElementById(`step-badge-${step}`).classList.add('active');
            document.getElementById(`step-label-${step}`).classList.remove('text-muted');
            document.getElementById(`step-label-${step}`).classList.add('fw-bold');

            // Validate before moving to step 3
            if (step === 3 && !reset) {
                const primaryTenantId = parseInt(document.getElementById('c-primaryTenantSelectGlobal').value);
                if (!primaryTenantId) {
                    showPremiumToast("Lỗi", "Vui lòng chọn người đứng tên hợp đồng ở Bước 2.", "warning");
                    contractGoStep(2);
                    return;
                }
                const opt = document.getElementById('c-primaryTenantSelectGlobal').options[document.getElementById('c-primaryTenantSelectGlobal').selectedIndex];
                
                document.getElementById('summary-room').innerText = _contractRoomCode;
                document.getElementById('summary-primary').innerText = opt.dataset.name;
            }
        }


window.contractGoStep = contractGoStep;
        function onPrimaryTenantChange() {
            const sel = document.getElementById('c-primaryTenantSelectGlobal');
            const preview = document.getElementById('c-tenantInfoPreview');
            const opt = sel.options[sel.selectedIndex];

            if (!sel.value) {
                preview.classList.add('d-none');
                return;
            }

            document.getElementById('c-preview-name').innerText = opt.dataset.name;
            document.getElementById('c-preview-phone').innerText = opt.dataset.phone || 'Không có SĐT';
            preview.classList.remove('d-none');
        }


window.onPrimaryTenantChange = onPrimaryTenantChange;
        function onMotelChangeForContract() {
            const motelId = parseInt(document.getElementById('c-motelSelect').value);
            const motel = _contractMotels.find(m => m.motelId === motelId);
            const floorSel = document.getElementById('c-floorSelect');
            const roomSel = document.getElementById('c-roomSelect');

            floorSel.innerHTML = '<option value="">-- Chọn tầng --</option>';
            roomSel.innerHTML = '<option value="">-- Chọn phòng --</option>';
            document.getElementById('c-roomPreview').classList.add('d-none');

            if (!motel) return;

            if (motel.floors && motel.floors.length > 0) {
                floorSel.innerHTML = '<option value="">-- Tất cả tầng --</option>' +
                    motel.floors.map(f => `<option value="${f.floorId}">${f.floorName || 'Tầng ' + f.floorNumber}</option>`).join('');
            } else {
                floorSel.innerHTML = '<option value="">Không có tầng</option>';
            }
            // Load all vacant rooms of this motel
            onFloorChangeForContract();
        }


window.onMotelChangeForContract = onMotelChangeForContract;
        function onFloorChangeForContract() {
            const motelId = parseInt(document.getElementById('c-motelSelect').value);
            const floorId = parseInt(document.getElementById('c-floorSelect').value) || null;
            const motel = _contractMotels.find(m => m.motelId === motelId);
            const roomSel = document.getElementById('c-roomSelect');

            document.getElementById('c-roomPreview').classList.add('d-none');
            if (!motel) return;

            let rooms = [];
            motel.floors.forEach(f => {
                if (!floorId || f.floorId === floorId) {
                    f.rooms.filter(r => r.status === 'Available' || r.status === 'Vacant').forEach(r => {
                        rooms.push({ ...r, floorName: f.floorName || ('Tầng ' + f.floorNumber) });
                    });
                }
            });

            if (rooms.length === 0) {
                roomSel.innerHTML = '<option value="">Không có phòng trống</option>';
            } else {
                roomSel.innerHTML = '<option value="">-- Chọn phòng --</option>' +
                    rooms.map(r => `<option value="${r.roomId}" data-area="${r.area}" data-floor="${r.floorName}" data-status="${r.status}" data-code="${r.roomCode}">${r.roomCode} - ${r.area}m²</option>`).join('');
            }
        }


window.onFloorChangeForContract = onFloorChangeForContract;
        function onRoomChangeForContract() {
            const sel = document.getElementById('c-roomSelect');
            const opt = sel.options[sel.selectedIndex];
            _contractRoomId = parseInt(sel.value) || null;
            _contractRoomCode = opt.dataset.code || '';

            if (!_contractRoomId) {
                document.getElementById('c-roomPreview').classList.add('d-none');
                return;
            }
            document.getElementById('c-preview-area').innerText = (opt.dataset.area || '--') + ' m²';
            document.getElementById('c-preview-status').innerText = 'Trống';
            document.getElementById('c-preview-floor').innerText = opt.dataset.floor || '--';
            document.getElementById('c-roomPreview').classList.remove('d-none');
        }


window.onRoomChangeForContract = onRoomChangeForContract;
        async function addOccupantToRoom() {
            if (!_contractRoomId) {
                showPremiumToast("Lỗi", "Vui lòng chọn phòng ở Bước 1 trước.", "warning");
                return;
            }
            const sel = document.getElementById('c-occupantSelect');
            const tenantId = parseInt(sel.value);
            const tenantName = sel.options[sel.selectedIndex]?.dataset.name;
            const role = document.getElementById('c-occupantRole').value;

            if (!tenantId) {
                showPremiumToast("Lỗi", "Vui lòng chọn khách thuê.", "warning");
                return;
            }
            if (_contractOccupants.find(o => o.tenantId === tenantId)) {
                showPremiumToast("Thông tin", "Khách này đã được thêm vào phòng.", "info");
                return;
            }

            try {
                const response = await fetch('/api/OccupancyManagement/AddOccupant', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ roomId: _contractRoomId, tenantId, occupantRole: role, checkInDate: new Date().toISOString() })
                });
                const result = await response.json();
                if (result.success) {
                    _contractOccupants.push({ tenantId, tenantName, role });
                    renderOccupantsList();
                    showPremiumToast("Thêm thành công", `Đã thêm ${tenantName} vào phòng.`, "success");
                } else {
                    showPremiumToast("Lỗi", result.message, "danger");
                }
            } catch (e) {
                showPremiumToast("Lỗi", "Không thể kết nối máy chủ.", "danger");
            }
        }


window.addOccupantToRoom = addOccupantToRoom;
        function renderOccupantsList() {
            const el = document.getElementById('c-occupantsList');
            if (_contractOccupants.length === 0) {
                el.innerHTML = '<div class="text-center text-muted small py-3 rounded-4 border">Chưa có người ở nào được thêm</div>';
                return;
            }
            el.innerHTML = _contractOccupants.map((o, i) => `
                <div class="d-flex align-items-center justify-content-between p-3 rounded-4 bg-light border">
                    <div class="d-flex align-items-center gap-3">
                        <img src="https://ui-avatars.com/api/?name=${encodeURIComponent(o.tenantName)}&background=6366f1&color=fff" class="rounded-circle" style="width:36px;height:36px">
                        <div>
                            <div class="fw-bold small">${o.tenantName}</div>
                            <span class="badge ${o.role === 'Primary' ? 'bg-primary' : 'bg-secondary'} bg-opacity-10 ${o.role === 'Primary' ? 'text-primary' : 'text-secondary'} small">${o.role === 'Primary' ? 'Đại diện' : 'Thành viên'}</span>
                        </div>
                    </div>
                    <span class="badge bg-success bg-opacity-10 text-success"><i class="bi bi-check-circle me-1"></i>Đã thêm</span>
                </div>
            `).join('');
        }


window.renderOccupantsList = renderOccupantsList;
        async function submitFullContract() {
            const primaryTenantId = parseInt(document.getElementById('c-primaryTenantSelectGlobal').value);
            const monthlyRent = parseFloat(document.getElementById('c-monthlyRent').value);
            const depositAmount = parseFloat(document.getElementById('c-depositAmount').value);
            const startDate = document.getElementById('c-startDate').value;
            const endDate = document.getElementById('c-endDate').value;
            const terms = document.getElementById('c-terms').value;
            const selectedServiceIds = Array.from(document.querySelectorAll('#c-services-checkboxes input:checked')).map(cb => parseInt(cb.value));

            if (!primaryTenantId) { showPremiumToast("Lỗi", "Vui lòng chọn người đại diện ký hợp đồng.", "warning"); return; }
            if (!monthlyRent || monthlyRent <= 0) { showPremiumToast("Lỗi", "Vui lòng nhập giá thuê hợp lệ.", "warning"); return; }
            if (!depositAmount || depositAmount <= 0) { showPremiumToast("Lỗi", "Vui lòng nhập tiền cọc hợp lệ.", "warning"); return; }
            if (!_contractRoomId) { showPremiumToast("Lỗi", "Không xác định được phòng. Vui lòng bắt đầu lại.", "danger"); return; }

            const btn = document.getElementById('btnSubmitContract');
            btn.disabled = true;
            btn.querySelector('.btn-text').innerText = 'Đang xử lý...';
            btn.querySelector('.spinner-border').classList.remove('d-none');

            try {
                const response = await fetch('/api/OccupancyManagement/CreateContract', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ 
                        roomId: _contractRoomId, 
                        primaryTenantId, 
                        monthlyRent, 
                        depositAmount, 
                        startDate, 
                        endDate, 
                        terms,
                        selectedServiceIds,
                        standardOccupants: parseInt(document.getElementById('c-standardOccupants').value) || 2,
                        extraOccupantFee: parseFloat(document.getElementById('c-extraFee').value) || 0
                    })
                });
                const result = await response.json();
                if (result.success) {
                    showPremiumToast("Ký kết thành công!", `Hợp đồng phòng ${_contractRoomCode} đã được tạo.`, "success");
                    bootstrap.Modal.getInstance(document.getElementById('addContractModal')).hide();
                    loadContractsData();
                } else {
                    showPremiumToast("Lỗi", result.message, "danger");
                }
            } catch (e) {
                showPremiumToast("Lỗi", "Không thể hoàn tất giao dịch.", "danger");
            } finally {
                btn.disabled = false;
                btn.querySelector('.btn-text').innerText = 'Xác nhận & Ký kết Hợp đồng';
                btn.querySelector('.spinner-border').classList.add('d-none');
            }
        }


window.submitFullContract = submitFullContract;
        async function terminateContract(id) {
            if (!confirm("CẢNH BÁO: Bạn có chắc chắn muốn CHẤM DỨT hợp đồng này?\n\n- Tài khoản đăng nhập của khách thuê sẽ bị XÓA.\n- Hồ sơ khách và lịch sử hóa đơn sẽ được LƯU TRỮ để đối soát.\n- Phòng sẽ trở thành trạng thái TRỐNG.")) return;
            try {
                const response = await fetch(`/api/OccupancyManagement/TerminateContract/${id}`, { method: 'POST' });
                const result = await response.json();
                if (result.success) {
                    showPremiumToast("Thành công", result.message, "success");
                    loadContractsData();
                } else {
                    showPremiumToast("Lỗi", result.message, "danger");
                }
            } catch (e) { 
                showPremiumToast("Lỗi", "Không thể kết nối máy chủ.", "danger");
            }
        }

        // --- Data Loading Logic ---

window.terminateContract = terminateContract;
