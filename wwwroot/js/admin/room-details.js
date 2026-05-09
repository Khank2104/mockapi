        async function showEditContractModal() {
            const roomId = _currentFmpRoomId;
            if (!roomId) return;

            try {
                const [cRes, sRes] = await Promise.all([
                    fetch(`/api/OccupancyManagement/GetActiveContractByRoom/${roomId}`),
                    fetch('/api/MotelManagement/GetGlobalServices')
                ]);
                const cResult = await cRes.json();
                const sResult = await sRes.json();

                if (!cResult.success) {
                    showPremiumToast("Lỗi", "Không tìm thấy hợp đồng đang hoạt động cho phòng này.", "warning");
                    return;
                }

                const c = cResult.data;
                document.getElementById('e-contractId').value = c.contractId;
                document.getElementById('e-roomId').value = c.roomId;
                document.getElementById('e-roomCode').innerText = document.getElementById('fmp-room-code').innerText;
                document.getElementById('e-monthlyRent').value = c.monthlyRent;
                document.getElementById('e-depositAmount').value = c.depositAmount;
                document.getElementById('e-startDate').value = c.startDate ? c.startDate.split('T')[0] : '';
                document.getElementById('e-endDate').value = c.endDate ? c.endDate.split('T')[0] : '';
                document.getElementById('e-terms').value = c.terms || '';
                document.getElementById('e-standardOccupants').value = c.standardOccupants || 2;
                document.getElementById('e-extraFee').value = c.extraOccupantFee || 0;

                if (sResult.success) {
                    const container = document.getElementById('e-services-checkboxes');
                    container.innerHTML = sResult.data.map(s => {
                        const sName = (s.serviceName || "").toLowerCase();
                        const isMandatory = sName.includes("điện") || sName.includes("nước");
                        const isActive = (c.selectedServiceIds || []).includes(s.serviceId);
                        return `
                        <div class="col-md-6">
                            <div class="form-check p-2 border rounded-4 ${isMandatory ? 'bg-primary bg-opacity-10' : 'bg-white'}">
                                <input class="form-check-input ms-0 me-2" type="checkbox" value="${s.serviceId}" id="esvc-${s.serviceId}" 
                                       ${isMandatory ? 'checked disabled' : (isActive ? 'checked' : '')}>
                                <label class="form-check-label x-small fw-bold" for="esvc-${s.serviceId}">
                                    ${s.serviceName} ${isMandatory ? '<span class="text-danger">(Bắt buộc)</span>' : ''}
                                </label>
                            </div>
                        </div>`;
                    }).join('');
                }

                new bootstrap.Modal(document.getElementById('editContractModal')).show();
            } catch (e) {
                showPremiumToast("Lỗi", "Không thể tải thông tin hợp đồng.", "danger");
            }
        }


window.showEditContractModal = showEditContractModal;
        async function openEditContractFromList(roomId, roomCode) {
            _currentFmpRoomId = roomId;
            // We need a way to set the room code for the modal
            // In the modal, we use document.getElementById('fmp-room-code').innerText
            // So let's ensure that element exists or handle it
            const codeEl = document.getElementById('fmp-room-code');
            if (codeEl) codeEl.innerText = roomCode;
            
            showEditContractModal();
        }


window.openEditContractFromList = openEditContractFromList;
        async function submitUpdateContract() {
            const contractId = document.getElementById('e-contractId').value;
            const roomId = document.getElementById('e-roomId').value;
            const monthlyRent = parseFloat(document.getElementById('e-monthlyRent').value);
            const depositAmount = parseFloat(document.getElementById('e-depositAmount').value);
            const startDate = document.getElementById('e-startDate').value;
            const endDate = document.getElementById('e-endDate').value;
            const terms = document.getElementById('e-terms').value;
            const selectedServiceIds = Array.from(document.querySelectorAll('#e-services-checkboxes input:checked')).map(cb => parseInt(cb.value));

            try {
                const response = await fetch(`/api/OccupancyManagement/UpdateContract/${contractId}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ 
                        roomId, 
                        monthlyRent, 
                        depositAmount, 
                        startDate, 
                        endDate, 
                        terms, 
                        SelectedServiceIds: selectedServiceIds,
                        standardOccupants: parseInt(document.getElementById('e-standardOccupants').value) || 2,
                        extraOccupantFee: parseFloat(document.getElementById('e-extraFee').value) || 0
                    })
                });
                const result = await response.json();
                if (result.success) {
                    showPremiumToast("Thành công", result.message, "success");
                    bootstrap.Modal.getInstance(document.getElementById('editContractModal')).hide();
                    openFmpRoom(roomId, document.getElementById('fmp-room-code').innerText, document.getElementById('fmp-room-status').innerText);
                    loadMotelsData(); 
                } else {
                    showPremiumToast("Lỗi", result.message, "danger");
                }
            } catch (e) {
                showPremiumToast("Lỗi", "Không thể kết nối đến máy chủ.", "danger");
            }
        }


window.submitUpdateContract = submitUpdateContract;
        async function removeOccupantFromFmp(occId, name) {
            if (!confirm(`Bạn có chắc chắn muốn mời ${name} dời đi không?`)) return;

            try {
                const response = await fetch(`/api/OccupancyManagement/RemoveOccupant/${occId}`, { method: 'DELETE' });
                const result = await response.json();
                if (result.success) {
                    showPremiumToast("Thành công", "Đã xóa người ở.", "success");
                    openFmpRoom(_currentFmpRoomId, document.getElementById('fmp-room-code').innerText, document.getElementById('fmp-room-status').innerText);
                } else {
                    showPremiumToast("Lỗi", result.message, "danger");
                }
            } catch (e) {
                showPremiumToast("Lỗi", "Lỗi kết nối.", "danger");
            }
        }


window.removeOccupantFromFmp = removeOccupantFromFmp;
        async function showAddOccupantModalFromFmp() {
            _currentRoomId = _currentFmpRoomId;
            if (!_currentRoomId) return;

            document.getElementById('ao-roomId').value = _currentRoomId;

            // Tải danh sách khách thuê chưa có phòng
            try {
                const response = await fetch('/api/TenantManagement/GetAllTenants');
                const result = await response.json();
                if (result.success) {
                    const sel = document.getElementById('ao-tenantSelect');
                    const items = result.data.items || result.data;
                    const availableTenants = items.filter(t => t.status !== 'Staying');
                    
                    if (availableTenants.length === 0) {
                        sel.innerHTML = '<option value="">(Không có khách thuê nào đang trống)</option>';
                    } else {
                        sel.innerHTML = '<option value="">-- Chọn khách thuê --</option>' +
                            availableTenants.map(t => `<option value="${t.tenantId}">${t.fullName} - ${t.phone || 'Không SĐT'}</option>`).join('');
                    }
                }
            } catch (e) {
                console.error("Error loading tenants for occupant modal", e);
            }

            new bootstrap.Modal(document.getElementById('addOccupantModal')).show();
        }

        window.showAddOccupantModalFromFmp = showAddOccupantModalFromFmp;

        async function submitAddOccupant() {
            const roomId = document.getElementById('ao-roomId').value;
            const tenantId = document.getElementById('ao-tenantSelect').value;
            const role = document.getElementById('ao-occupantRole').value;

            if (!roomId || !tenantId) {
                showPremiumToast("Lỗi", "Vui lòng chọn khách thuê cần thêm.", "warning");
                return;
            }

            try {
                const response = await fetch('/api/OccupancyManagement/AddOccupant', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ 
                        roomId: parseInt(roomId), 
                        tenantId: parseInt(tenantId), 
                        occupantRole: role, 
                        checkInDate: new Date().toISOString() 
                    })
                });
                const result = await response.json();
                if (result.success) {
                    showPremiumToast("Thành công", "Đã thêm thành viên vào phòng.", "success");
                    bootstrap.Modal.getInstance(document.getElementById('addOccupantModal')).hide();
                    
                    // Reload danh sách thành viên trong khung Sơ đồ phòng
                    if (typeof openFmpRoom === 'function') {
                        openFmpRoom(_currentFmpRoomId, document.getElementById('fmp-room-code').innerText, document.getElementById('fmp-room-status').innerText);
                    }
                } else {
                    showPremiumToast("Lỗi", result.message, "danger");
                }
            } catch (e) {
                showPremiumToast("Lỗi", "Không kết nối được server.", "danger");
            }
        }

        window.submitAddOccupant = submitAddOccupant;
        function confirmTerminateFromEdit() {
            const contractId = document.getElementById('e-contractId').value;
            if (confirm("CẢNH BÁO: Bạn có chắc chắn muốn kết thúc hợp đồng này? Khách sẽ được dời đi và phòng sẽ được giải phóng.")) {
                bootstrap.Modal.getInstance(document.getElementById('editContractModal')).hide();
                terminateContract(contractId);
            }
        }


window.confirmTerminateFromEdit = confirmTerminateFromEdit;
        async function openRoomDetails(roomId, roomCode) {
            document.getElementById('detail-room-code').innerText = roomCode;
            document.getElementById('settings-roomId').value = roomId;
            switchModule('room-details');

            // Load Details from API
            try {
                // Room Settings
                const resSettings = await fetch(`/api/MotelManagement/GetRoomSettings/${roomId}`);
                const dataSettings = await resSettings.json();
                if (dataSettings.success && dataSettings.data) {
                    const s = dataSettings.data;
                    const form = document.getElementById('roomSettingsForm');
                    form.BaseRent.value = s.baseRent;
                    form.DepositAmount.value = s.depositAmount;
                    form.StandardOccupants.value = s.standardOccupants;
                    form.ExtraOccupantFee.value = s.extraOccupantFee;
                    form.ApplyExtraOccupantFee.checked = s.applyExtraOccupantFee;
                }

                // Room Services
                const resServices = await fetch(`/api/MotelManagement/GetRoomServices/${roomId}`);
                const dataServices = await resServices.json();
                const serviceList = document.getElementById('room-services-list');
                if (dataServices.success) {
                    serviceList.innerHTML = dataServices.data.map(s => `
                        <div class="p-3 bg-light rounded-4 d-flex justify-content-between align-items-center">
                            <div>
                                <div class="fw-bold small">${s.serviceName}</div>
                                <div class="x-small text-muted">${s.unit} - ${s.calculationType}</div>
                            </div>
                            <div class="d-flex align-items-center gap-2">
                                <span class="fw-bold text-primary">${s.unitPrice.toLocaleString()}đ</span>
                                <span class="x-small text-muted">/ ${s.unit}</span>
                            </div>
                        </div>
                    `).join('');
                }

                // Occupants
                const resOccupants = await fetch(`/api/MotelManagement/GetRoomOccupants/${roomId}`);
                const dataOccupants = await resOccupants.json();
                const occupantList = document.getElementById('occupants-list-container');
                if (dataOccupants.success) {
                    if (dataOccupants.data.length === 0) {
                        occupantList.innerHTML = '<tr><td colspan="5" class="text-center py-4 text-muted">Chưa có thành viên nào.</td></tr>';
                    } else {
                        occupantList.innerHTML = dataOccupants.data.map(o => `
                            <tr>
                                <td><div class="fw-bold">${o.fullName}</div></td>
                                <td><span class="badge bg-light text-dark">${o.occupantRole}</span></td>
                                <td class="small text-muted">${new Date(o.checkInDate).toLocaleDateString('vi-VN')}</td>
                                <td class="small">${o.phone || 'N/A'}</td>
                                <td>
                                    <button class="btn btn-sm btn-outline-danger rounded-pill" onclick="removeOccupant(${o.roomOccupantId})">Xóa</button>
                                </td>
                            </tr>
                        `).join('');
                    }
                }
            } catch (e) {
                console.error("Error loading room details", e);
            }
        }


window.openRoomDetails = openRoomDetails;
        async function removeOccupant(occupantId) {
            if (!confirm("Bạn có chắc chắn muốn xóa thành viên này khỏi phòng?")) return;
            try {
                // Assuming an endpoint exists or we use a generic update
                showPremiumToast("Thông tin", "Tính năng xóa đang được xử lý...", "info");
            } catch (e) {}
        }


window.removeOccupant = removeOccupant;
        async function saveRoomSettings() {
            const form = document.getElementById('roomSettingsForm');
            const data = Object.fromEntries(new FormData(form).entries());
            data.RoomId = parseInt(data.RoomId);
            data.BaseRent = parseFloat(data.BaseRent);
            data.DepositAmount = parseFloat(data.DepositAmount);
            data.StandardOccupants = parseInt(data.StandardOccupants);
            data.ExtraOccupantFee = parseFloat(data.ExtraOccupantFee);
            data.ApplyExtraOccupantFee = document.getElementById('applyExtraFee').checked;
            data.MaxOccupants = data.StandardOccupants; // Giới hạn tối đa chính là giá trị thỏa thuận nhập từ UI

            try {
                const response = await fetch('/api/MotelManagement/UpdateRoomSetting', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(data)
                });
                const result = await response.json();
                if (result.success) {
                    showPremiumToast("Thành công", "Cấu hình thuê đã được lưu.", "success");
                } else {
                    alert(result.message);
                }
            } catch (e) {
                alert("Lỗi khi lưu cấu hình.");
            }
        }




window.saveRoomSettings = saveRoomSettings;
        async function showAddOccupantModal() {
            // Re-use logic for the Room Details page tab
            if (!_currentRoomId) return;
            _currentFmpRoomId = _currentRoomId;
            await showAddOccupantModalFromFmp();
        }


window.showAddOccupantModal = showAddOccupantModal;
