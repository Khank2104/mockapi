        let currentBillingPage = 1;
        let totalBillingPages = 1;

        async function loadBillingData(page = 1) {
            currentBillingPage = page;
            const container = document.getElementById('billing-list-container');
            const month = document.getElementById('billing-month').value;
            const year = document.getElementById('billing-year').value;
            const motelId = document.getElementById('billing-motel-filter').value;
            
            try {
                const response = await fetch(`/api/Invoice/Summary?month=${month}&year=${year}&motelId=${motelId}&page=${page}&pageSize=10`);
                const result = await response.json();
                
                if (result.success) {
                    const { items, totalCount, totalPages, currentPage } = result.data;
                    totalBillingPages = totalPages;

                    if (items.length === 0) {
                        container.innerHTML = `
                            <div class="col-12 text-center py-5">
                                <div class="empty-state border-0 bg-transparent">
                                    <i class="bi bi-receipt fs-1 opacity-25"></i>
                                    <h4 class="text-muted mt-3">Không có dữ liệu hóa đơn</h4>
                                    <p class="text-muted small">Kỳ này hiện chưa có hóa đơn nào được khởi tạo.</p>
                                </div>
                            </div>`;
                        updatePaginationUI(0, 0, 0, 1, 1);
                        return;
                    }

                    container.innerHTML = items.map(r => {
                        const elecHtml = r.electricity 
                            ? `<div class="bg-warning bg-opacity-10 rounded-3 p-2 text-center flex-grow-1 border border-warning border-opacity-25 shadow-sm position-relative">
                                <div class="text-warning small fw-bold mb-1"><i class="bi bi-lightning-charge-fill me-1"></i>Điện</div>
                                <div class="fw-bolder fs-6 text-body">${r.electricity.currentReading} <span class="fw-normal x-small text-muted">(${r.electricity.usageAmount} kWh)</span></div>
                                ${!r.invoice ? `<button class="btn btn-link btn-sm text-danger position-absolute top-0 end-0 p-1" onclick="deleteReading(${r.electricity.readingId})" style="line-height:1; font-size: 10px;"><i class="bi bi-x-circle"></i></button>` : ''}
                               </div>` 
                            : `<div class="bg-secondary bg-opacity-10 rounded-3 p-2 text-center flex-grow-1 border border-secondary border-opacity-10 opacity-75">
                                <div class="text-muted small fw-bold mb-1"><i class="bi bi-lightning-charge me-1"></i>Điện</div>
                                <div class="x-small text-muted mt-1">Chưa ghi</div>
                               </div>`;
                            
                        const waterHtml = r.water 
                            ? `<div class="bg-info bg-opacity-10 rounded-3 p-2 text-center flex-grow-1 border border-info border-opacity-25 shadow-sm position-relative">
                                <div class="text-info small fw-bold mb-1"><i class="bi bi-droplet-fill me-1"></i>Nước</div>
                                <div class="fw-bolder fs-6 text-body">${r.water.currentReading} <span class="fw-normal x-small text-muted">(${r.water.usageAmount} m³)</span></div>
                                ${!r.invoice ? `<button class="btn btn-link btn-sm text-danger position-absolute top-0 end-0 p-1" onclick="deleteReading(${r.water.readingId})" style="line-height:1; font-size: 10px;"><i class="bi bi-x-circle"></i></button>` : ''}
                               </div>` 
                            : `<div class="bg-secondary bg-opacity-10 rounded-3 p-2 text-center flex-grow-1 border border-secondary border-opacity-10 opacity-75">
                                <div class="text-muted small fw-bold mb-1"><i class="bi bi-droplet me-1"></i>Nước</div>
                                <div class="x-small text-muted mt-1">Chưa ghi</div>
                               </div>`;

                        const statusBadge = r.invoice 
                            ? `<div class="mb-3 text-center">
                                <div class="badge ${r.invoice.invoiceStatus === 'Paid' ? 'bg-success' : (r.invoice.invoiceStatus === 'Pending' ? 'bg-warning' : 'bg-danger')} bg-opacity-10 text-${r.invoice.invoiceStatus === 'Paid' ? 'success' : (r.invoice.invoiceStatus === 'Pending' ? 'warning' : 'danger')} rounded-pill px-3 py-1 mb-2 border border-${r.invoice.invoiceStatus === 'Paid' ? 'success' : (r.invoice.invoiceStatus === 'Pending' ? 'warning' : 'danger')} border-opacity-50 shadow-sm">
                                    <i class="bi ${r.invoice.invoiceStatus === 'Paid' ? 'bi-check-circle-fill' : 'bi-exclamation-circle-fill'} me-1"></i>
                                    ${r.invoice.invoiceStatus === 'Paid' ? 'Đã thanh toán' : (r.invoice.invoiceStatus === 'Pending' ? 'Chờ duyệt' : 'Chưa thanh toán')}
                                </div>
                                <div class="fs-4 fw-bolder text-primary drop-shadow-sm">${r.invoice.totalAmount.toLocaleString()}đ</div>
                               </div>`
                            : `<div class="mb-3 text-center">
                                <div class="badge bg-secondary bg-opacity-10 text-muted rounded-pill px-3 py-1 mb-2 border border-secondary border-opacity-25">
                                    <i class="bi bi-clock me-1"></i> Chưa chốt hóa đơn
                                </div>
                                <div class="fs-4 fw-bold text-muted opacity-25">0đ</div>
                               </div>`;

                        const actionArea = r.invoice
                            ? `<div class="d-flex gap-2 justify-content-center">
                                 <button class="btn btn-sm btn-outline-primary rounded-pill px-4 shadow-sm hover-lift" onclick="viewInvoiceDetails(${r.invoice.invoiceId})"><i class="bi bi-eye me-1"></i> Xem chi tiết</button>
                                 <button class="btn btn-sm btn-outline-danger rounded-circle shadow-sm hover-lift" style="width:32px; height:32px; padding:0" onclick="deleteInvoice(${r.invoice.invoiceId})" title="Xóa hóa đơn"><i class="bi bi-trash"></i></button>
                               </div>`
                            : `<div class="d-flex flex-column gap-2">
                                 <button class="btn btn-sm btn-outline-warning rounded-pill shadow-sm hover-lift" onclick="showRecordMeterModal(${r.roomId}, '${r.roomCode}', ${r.isOccupied})"><i class="bi bi-pencil-square me-1"></i> Nhập số điện nước</button>
                                 <button class="btn btn-sm btn-premium rounded-pill shadow-sm hover-lift" onclick="generateInvoice(${r.roomId}, ${r.isOccupied})"><i class="bi bi-calculator me-1"></i> Lập hóa đơn</button>
                               </div>`;

                        return `
                            <div class="col-md-6 col-lg-4 col-xl-3">
                                <div class="glass-card billing-card card-status-${r.invoice ? r.invoice.invoiceStatus : 'Unpaid'} p-4 h-100 d-flex flex-column hover-lift">
                                    <div class="text-center mb-4 position-relative">
                                        <div class="position-absolute top-0 end-0 opacity-25">
                                            <i class="bi bi-receipt fs-3"></i>
                                        </div>
                                        <div class="d-inline-flex align-items-center justify-content-center bg-primary bg-opacity-10 text-primary rounded-pill px-4 py-2 fw-bolder fs-5 shadow-sm border border-primary border-opacity-25">
                                            Phòng ${r.roomCode}
                                        </div>
                                        ${!r.isOccupied ? `<div class="mt-2"><span class="badge bg-secondary bg-opacity-10 text-muted border border-secondary border-opacity-25"><i class="bi bi-person-x me-1"></i> Trống</span></div>` : ''}
                                    </div>
                                    
                                    <div class="d-flex gap-3 mb-4">
                                        ${elecHtml}
                                        ${waterHtml}
                                    </div>
                                    
                                    <div class="mt-auto d-flex flex-column justify-content-end bg-secondary bg-opacity-10 rounded-4 p-3 border border-secondary border-opacity-10" style="min-height: 160px;">
                                        ${statusBadge}
                                        <div class="mt-auto">${actionArea}</div>
                                    </div>
                                </div>
                            </div>
                        `;
                    }).join('');

                    const start = (currentPage - 1) * 10 + 1;
                    const end = start + items.length - 1;
                    updatePaginationUI(start, end, totalCount, currentPage, totalPages);
                }
            } catch (e) {
                console.error('loadBillingData error:', e);
            }
        }

        function updatePaginationUI(start, end, total, current, totalPages) {
            document.getElementById('billing-range').innerText = `${start}-${end}`;
            document.getElementById('billing-total').innerText = total;
            document.getElementById('billing-current-page').innerText = current;
            
            document.getElementById('billing-prev-btn').classList.toggle('disabled', current <= 1);
            document.getElementById('billing-next-btn').classList.toggle('disabled', current >= totalPages);
        }

        function changeBillingPage(delta) {
            const next = currentBillingPage + delta;
            if (next >= 1 && next <= totalBillingPages) {
                loadBillingData(next);
            }
        }

        async function loadMotelsForBillingFilter() {
            try {
                const response = await fetch('/api/MotelManagement/MyMotels');
                const result = await response.json();
                if (result.success) {
                    const select = document.getElementById('billing-motel-filter');
                    select.innerHTML = '<option value="0">-- Tất cả khu trọ --</option>' + 
                        result.data.map(m => `<option value="${m.motelId}">${m.motelName}</option>`).join('');
                }
            } catch (e) {}
        }

        // Initialize motels filter on module load
        document.addEventListener('DOMContentLoaded', loadMotelsForBillingFilter);

window.loadBillingData = loadBillingData;
window.changeBillingPage = changeBillingPage;
        async function showRecordMeterModal(roomId, roomCode, isOccupied = true) {
            if (!isOccupied) {
                showPremiumToast("Phòng trống", `Phòng ${roomCode} hiện không có người ở. Bạn không thể nhập chỉ số điện nước cho phòng trống.`, "warning");
                return;
            }

            document.getElementById('record-roomId').value = roomId;
            document.getElementById('record-room-code').innerText = roomCode;
            document.getElementById('record-elec-curr').value = '';
            document.getElementById('record-water-curr').value = '';
            const elecCont = document.getElementById('usage-elec-container');
            if (elecCont) elecCont.classList.add('d-none');
            const waterCont = document.getElementById('usage-water-container');
            if (waterCont) waterCont.classList.add('d-none');
            
            // Reset serviceId trước
            document.getElementById('record-elec-serviceId').value = '';
            document.getElementById('record-water-serviceId').value = '';
            
            try {
                const response = await fetch('/api/MotelManagement/GetGlobalServices');
                const result = await response.json();
                if (result.success) {
                    currentServices = result.data.filter(s => s.calculationType === 'metered');
                    
                    // Tìm dịch vụ điện: ưu tiên serviceCode chứa electric/elec/dien, fallback theo serviceName
                    const elecService = currentServices.find(s => 
                        s.serviceCode.toLowerCase().includes('electric') || 
                        s.serviceCode.toLowerCase().includes('elec') || 
                        s.serviceCode.toLowerCase().includes('dien')
                    ) || currentServices.find(s => s.serviceName.toLowerCase().includes('điện'));
                    
                    // Tìm dịch vụ nước: ưu tiên serviceCode chứa water/wat/nuoc, fallback theo serviceName
                    const waterService = currentServices.find(s => 
                        s.serviceCode.toLowerCase().includes('water') || 
                        s.serviceCode.toLowerCase().includes('wat') || 
                        s.serviceCode.toLowerCase().includes('nuoc')
                    ) || currentServices.find(s => s.serviceName.toLowerCase().includes('nước'));
                    
                    if (elecService) document.getElementById('record-elec-serviceId').value = elecService.serviceId;
                    if (waterService) document.getElementById('record-water-serviceId').value = waterService.serviceId;
                    
                    // Tải chỉ số cũ sau khi đã có serviceId
                    await loadPreviousReading(roomId);
                }
            } catch (e) {
                console.error('showRecordMeterModal error:', e);
                showPremiumToast("Lỗi", "Không thể tải danh sách dịch vụ.", "danger");
            }
            
            new bootstrap.Modal(document.getElementById('recordMeterModal')).show();
        }


window.showRecordMeterModal = showRecordMeterModal;
        async function loadPreviousReading(roomId) {
            document.getElementById('record-elec-prev').value = "0";
            document.getElementById('record-water-prev').value = "0";

            try {
                const response = await fetch(`/api/MeterReading/Latest/${roomId}`);
                const result = await response.json();
                
                if (result.success && result.data && result.data.length > 0) {
                    const elecServiceId = document.getElementById('record-elec-serviceId').value;
                    const waterServiceId = document.getElementById('record-water-serviceId').value;

                    // Tìm theo serviceId trước (chính xác nhất), fallback theo serviceName
                    const elecReading = result.data.find(r => String(r.serviceId) === String(elecServiceId))
                        || result.data.find(r => 
                            r.serviceName && (
                                r.serviceName.toLowerCase().includes('điện') ||
                                r.serviceName.toLowerCase().includes('dien') ||
                                r.serviceName.toLowerCase().includes('electric')
                            )
                        );
                    
                    const waterReading = result.data.find(r => String(r.serviceId) === String(waterServiceId))
                        || result.data.find(r => 
                            r.serviceName && (
                                r.serviceName.toLowerCase().includes('nước') ||
                                r.serviceName.toLowerCase().includes('nuoc') ||
                                r.serviceName.toLowerCase().includes('water')
                            )
                        );

                    if (elecReading) {
                        document.getElementById('record-elec-prev').value = elecReading.currentReading;
                    }
                    if (waterReading) {
                        document.getElementById('record-water-prev').value = waterReading.currentReading;
                    }
                }
            } catch (e) {
                console.error("Lỗi lấy chỉ số cũ:", e);
            }
        }


window.loadPreviousReading = loadPreviousReading;
        function calculateElecUsage() {
            const prev = parseFloat(document.getElementById('record-elec-prev').value) || 0;
            const curr = parseFloat(document.getElementById('record-elec-curr').value) || 0;
            const container = document.getElementById('usage-elec-container');
            if (curr > prev) {
                if (container) container.classList.remove('d-none');
                const valEl = document.getElementById('usage-elec-value');
                if (valEl) valEl.innerText = (curr - prev).toFixed(1);
            } else {
                if (container) container.classList.add('d-none');
            }
        }


window.calculateElecUsage = calculateElecUsage;
        function calculateWaterUsage() {
            const prev = parseFloat(document.getElementById('record-water-prev').value) || 0;
            const curr = parseFloat(document.getElementById('record-water-curr').value) || 0;
            const container = document.getElementById('usage-water-container');
            if (curr > prev) {
                if (container) container.classList.remove('d-none');
                const valEl = document.getElementById('usage-water-value');
                if (valEl) valEl.innerText = (curr - prev).toFixed(1);
            } else {
                if (container) container.classList.add('d-none');
            }
        }


window.calculateWaterUsage = calculateWaterUsage;
        async function submitMeterReading() {
            const roomId = document.getElementById('record-roomId').value;
            const month = document.getElementById('billing-month').value;
            const year = document.getElementById('billing-year').value;
            
            const elecServiceId = document.getElementById('record-elec-serviceId').value;
            const elecCurr = document.getElementById('record-elec-curr').value;
            
            const waterServiceId = document.getElementById('record-water-serviceId').value;
            const waterCurr = document.getElementById('record-water-curr').value;

            const btn = document.getElementById('btnSubmitReading');
            
            if (!elecCurr && !waterCurr) {
                showPremiumToast("Lưu ý", "Vui lòng nhập ít nhất một chỉ số Điện hoặc Nước.", "warning");
                return;
            }

            // Kiểm tra serviceId có tồn tại không
            if (elecCurr && !elecServiceId) {
                showPremiumToast("Lỗi cấu hình", "Không tìm thấy dịch vụ Điện trong hệ thống. Vui lòng vào 'Cấu hình Dịch vụ' để khởi tạo.", "warning");
                return;
            }
            if (waterCurr && !waterServiceId) {
                showPremiumToast("Lỗi cấu hình", "Không tìm thấy dịch vụ Nước trong hệ thống. Vui lòng vào 'Cấu hình Dịch vụ' để khởi tạo.", "warning");
                return;
            }

            const requests = [];
            
            if (elecCurr && elecServiceId) {
                requests.push({ type: 'Điện', promise: fetch('/api/MeterReading/Create', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        roomId: parseInt(roomId),
                        serviceId: parseInt(elecServiceId),
                        billingMonth: parseInt(month),
                        billingYear: parseInt(year),
                        readingValue: parseFloat(elecCurr)
                    })
                })});
            }

            if (waterCurr && waterServiceId) {
                requests.push({ type: 'Nước', promise: fetch('/api/MeterReading/Create', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        roomId: parseInt(roomId),
                        serviceId: parseInt(waterServiceId),
                        billingMonth: parseInt(month),
                        billingYear: parseInt(year),
                        readingValue: parseFloat(waterCurr)
                    })
                })});
            }

            btn.disabled = true;
            const btnText = btn.querySelector('.btn-text');
            const btnSpinner = btn.querySelector('.spinner-border');
            if (btnText) btnText.classList.add('d-none');
            if (btnSpinner) btnSpinner.classList.remove('d-none');

            try {
                // Gọi tuần tự để tránh conflict (điện trước, nước sau)
                const errors = [];
                for (const req of requests) {
                    const res = await req.promise;
                    const json = await res.json();
                    if (!json.success) {
                        errors.push(`${req.type}: ${json.message}`);
                    }
                }

                bootstrap.Modal.getInstance(document.getElementById('recordMeterModal')).hide();

                if (errors.length > 0) {
                    showPremiumToast("Có lỗi xảy ra", errors.join('\n'), "warning");
                } else {
                    showPremiumToast("Thành công", "Đã ghi chỉ số thành công!", "success");
                }

                // Luôn reload lại danh sách để hiển thị kết quả mới nhất
                loadBillingData();
            } catch (e) {
                console.error('submitMeterReading error:', e);
                showPremiumToast("Lỗi", "Không thể kết nối máy chủ để lưu chỉ số.", "danger");
            } finally {
                btn.disabled = false;
                if (btnText) btnText.classList.remove('d-none');
                if (btnSpinner) btnSpinner.classList.add('d-none');
            }
        }


window.submitMeterReading = submitMeterReading;
        async function generateInvoice(roomId, isOccupied = true) {
            if (!isOccupied) {
                showPremiumToast("Phòng trống", "Phòng hiện đang trống, không thể lập hóa đơn.", "warning");
                return;
            }

            const month = document.getElementById('billing-month').value;
            const year = document.getElementById('billing-year').value;
            
            if (confirm(`Bạn có chắc muốn chốt hóa đơn tháng ${month}/${year} cho phòng này? Sau khi chốt sẽ không thể sửa chỉ số điện nước.`)) {
                try {
                    const response = await fetch('/api/Invoice/Generate', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({
                            roomId: parseInt(roomId),
                            billingMonth: parseInt(month),
                            billingYear: parseInt(year),
                            dueDate: new Date(parseInt(year), parseInt(month), 5).toISOString()
                        })
                    });
                    const result = await response.json();
                    if (result.success) {
                        showPremiumToast("Thành công", "Đã chốt hóa đơn. Người thuê có thể xem hóa đơn này.", "success");
                        loadBillingData();
                    } else {
                        showPremiumToast("Lỗi", result.message, "danger");
                    }
                } catch (e) {
                    showPremiumToast("Lỗi", "Không thể tạo hóa đơn.", "danger");
                }
            }
        }


window.generateInvoice = generateInvoice;
        async function viewInvoiceDetails(invoiceId) {
            const container = document.getElementById('invoice-detail-content');
            const modal = new bootstrap.Modal(document.getElementById('invoiceDetailsModal'));
            modal.show();

            document.getElementById('btnExportExcel').onclick = () => downloadInvoiceExcel(invoiceId);
            // Track current invoice for QR button
            const idField = document.getElementById('current-invoice-id');
            if (idField) idField.value = invoiceId;

            // Reset content
            container.innerHTML = `<div class="text-center py-5"><div class="spinner-border text-primary" role="status"></div><p class="mt-2 text-muted">Đang tải chi tiết hóa đơn...</p></div>`;

            try {
                const response = await fetch(`/api/Invoice/${invoiceId}`);
                const result = await response.json();

                if (result.success) {
                    const inv = result.data;
                    let html = `
                        <div class="row g-3 mb-4">
                            <div class="col-6">
                                <div class="text-muted small">Phòng:</div>
                                <div class="fw-bold fs-5">${inv.roomCode}</div>
                            </div>
                            <div class="col-6 text-end">
                                <div class="text-muted small">Kỳ thanh toán:</div>
                                <div class="fw-bold fs-5">Tháng ${inv.billingMonth}/${inv.billingYear}</div>
                            </div>
                        </div>
                        <div class="table-responsive">
                            <table class="table table-hover border-top">
                                <thead class="bg-secondary bg-opacity-10">
                                    <tr>
                                        <th>Nội dung</th>
                                        <th>Mô tả</th>
                                        <th class="text-end">Thành tiền</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    ${inv.details.map(d => `
                                        <tr>
                                            <td><span class="fw-bold">${d.serviceName}</span></td>
                                            <td class="small text-muted">${d.description}</td>
                                            <td class="text-end fw-bold">${d.subTotal.toLocaleString()} đ</td>
                                        </tr>
                                    `).join('')}
                                    <tr class="table-active">
                                        <td colspan="2" class="text-end fw-bold">TỔNG CỘNG:</td>
                                        <td class="text-end fw-bold text-danger fs-5">${inv.totalAmount.toLocaleString()} đ</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    `;
                    container.innerHTML = html;

                    // Handle Payment Proof display
                    const proofContainer = document.getElementById('payment-proof-container');
                    const proofImg = document.getElementById('payment-proof-img');
                    const verifyForm = document.getElementById('verify-payment-form');
                    const actualAmountInput = document.getElementById('verify-actual-amount');

                    if (inv.paymentProofPath) {
                        proofContainer.classList.remove('d-none');
                        proofImg.src = inv.paymentProofPath;
                        
                        // Chỉ hiện nút Phê duyệt/Từ chối nếu trạng thái là Pending
                        const actionButtons = proofContainer.querySelector('.d-flex.gap-2');
                        if (actionButtons) {
                            const isPending = inv.status === 'Pending';
                            actionButtons.style.display = isPending ? 'flex' : 'none';
                            if (verifyForm) verifyForm.classList.toggle('d-none', !isPending);
                            if (actualAmountInput) actualAmountInput.value = inv.totalAmount - inv.paidAmount;
                        }
                    } else {
                        proofContainer.classList.add('d-none');
                        if (verifyForm) verifyForm.classList.add('d-none');
                    }

                    // Ẩn nút QR Thanh toán nếu đã thanh toán
                    const qrBtn = document.querySelector('#invoiceDetailsModal .btn-outline-success');
                    if (qrBtn) {
                        qrBtn.style.display = inv.status === 'Paid' ? 'none' : 'block';
                    }
                } else {
                    container.innerHTML = `<div class="alert alert-danger">${result.message}</div>`;
                }
            } catch (e) {
                container.innerHTML = `<div class="alert alert-danger">Lỗi khi tải chi tiết hóa đơn.</div>`;
            }
        }

        async function deleteReading(readingId) {
            if (confirm("Bạn có chắc chắn muốn XÓA chỉ số này?")) {
                try {
                    const response = await fetch(`/api/MeterReading/${readingId}`, { method: 'DELETE' });
                    const result = await response.json();
                    if (result.success) {
                        showPremiumToast("Thành công", result.message, "success");
                        loadBillingData();
                    } else {
                        showPremiumToast("Lỗi", result.message, "danger");
                    }
                } catch (e) {
                    showPremiumToast("Lỗi", "Không thể xóa chỉ số.", "danger");
                }
            }
        }

        async function deleteInvoice(invoiceId) {
            if (confirm(`Bạn có chắc chắn muốn XÓA hóa đơn này? Sau khi xóa bạn có thể sửa chỉ số điện nước.`)) {
                try {
                    const response = await fetch(`/api/Invoice/${invoiceId}`, { method: 'DELETE' });
                    const result = await response.json();
                    if (result.success) {
                        showPremiumToast("Thành công", result.message, "success");
                        loadBillingData();
                    } else {
                        showPremiumToast("Lỗi", result.message || "Không thể xóa hóa đơn.", "danger");
                    }
                } catch (e) {
                    showPremiumToast("Lỗi", "Lỗi kết nối khi xóa hóa đơn.", "danger");
                }
            }
        }

        async function downloadInvoiceExcel(invoiceId) {
            try {
                const response = await fetch(`/api/Invoice/${invoiceId}/ExportExcel`);
                if (!response.ok) throw new Error("Export failed");

                const blob = await response.blob();
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `HoaDon_Phong_${invoiceId}.xlsx`;
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                a.remove();
            } catch (e) {
                showPremiumToast("Lỗi", "Không thể xuất file Excel.", "danger");
            }
        }


window.viewInvoiceDetails = viewInvoiceDetails;
window.downloadInvoiceExcel = downloadInvoiceExcel;
window.deleteInvoice = deleteInvoice;
window.deleteReading = deleteReading;

        function openQRPaymentPage() {
            const id = document.getElementById('current-invoice-id')?.value;
            if (!id || id === '0') {
                showPremiumToast('Lỗi', 'Không tìm thấy mã hóa đơn.', 'danger');
                return;
            }
            window.open(`/QRPayment/${id}`, '_blank');
        }

window.openQRPaymentPage = openQRPaymentPage;

        async function verifyInvoicePayment(approved) {
            const id = document.getElementById('current-invoice-id')?.value;
            if (!id || id === '0') return;

            const action = approved ? 'phê duyệt' : 'từ chối';
            if (!confirm(`Bạn có chắc chắn muốn ${action} minh chứng thanh toán này?`)) return;

            const actualAmount = approved ? document.getElementById('verify-actual-amount').value : null;

            try {
                const response = await fetch('/api/QRPayment/Verify', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ 
                        invoiceId: parseInt(id), 
                        approved: approved,
                        actualAmount: actualAmount ? parseFloat(actualAmount) : null
                    })
                });
                const result = await response.json();
                if (result.success) {
                    showPremiumToast("Thành công", result.message, "success");
                    bootstrap.Modal.getInstance(document.getElementById('invoiceDetailsModal')).hide();
                    loadBillingData();
                } else {
                    showPremiumToast("Lỗi", result.message, "danger");
                }
            } catch (e) {
                showPremiumToast("Lỗi", "Không thể kết nối máy chủ.", "danger");
            }
        }

window.verifyInvoicePayment = verifyInvoicePayment;
