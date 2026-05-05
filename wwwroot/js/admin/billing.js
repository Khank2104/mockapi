        async function loadBillingData() {
            const container = document.getElementById('billing-list-container');
            const month = document.getElementById('billing-month').value;
            const year = document.getElementById('billing-year').value;
            
            try {
                const response = await fetch(`/api/Invoice/Summary?month=${month}&year=${year}`);
                const result = await response.json();
                if (result.success) {
                    if (result.data.length === 0) {
                        container.innerHTML = '<tr><td colspan="5" class="text-center py-5">Không có phòng nào đang ở để tính tiền.</td></tr>';
                        return;
                    }

                    container.innerHTML = result.data.map(r => {
                        // Hiển thị chỉ số điện - dùng cả previousReading
                        const elecHtml = r.electricity 
                            ? `<div class="d-flex align-items-center gap-2 text-warning">
                                <i class="bi bi-lightning-charge-fill"></i>
                                <span><small class="text-muted">Cũ: ${r.electricity.previousReading}</small> → <b>${r.electricity.currentReading}</b> <small class="text-muted">(${r.electricity.usageAmount} kWh)</small></span>
                               </div>` 
                            : `<div class="d-flex align-items-center gap-2 text-warning opacity-50"><i class="bi bi-lightning-charge"></i> <span class="small">Chưa ghi...</span></div>`;
                            
                        // Hiển thị chỉ số nước - dùng cả previousReading
                        const waterHtml = r.water 
                            ? `<div class="d-flex align-items-center gap-2 text-info">
                                <i class="bi bi-droplet-fill"></i>
                                <span><small class="text-muted">Cũ: ${r.water.previousReading}</small> → <b>${r.water.currentReading}</b> <small class="text-muted">(${r.water.usageAmount} m³)</small></span>
                               </div>` 
                            : `<div class="d-flex align-items-center gap-2 text-info opacity-50"><i class="bi bi-droplet"></i> <span class="small">Chưa ghi...</span></div>`;

                        const invoiceStatus = r.invoice 
                            ? (r.invoice.invoiceStatus === 'Paid' 
                                ? `<span class="badge bg-success bg-opacity-10 text-success px-3 py-2 rounded-pill"><i class="bi bi-check-circle me-1"></i> Đã thanh toán</span><br><small class="text-muted fw-bold mt-1 d-block">${r.invoice.totalAmount.toLocaleString()} đ</small>` 
                                : `<span class="badge bg-danger bg-opacity-10 text-danger px-3 py-2 rounded-pill"><i class="bi bi-exclamation-circle me-1"></i> Chưa thu</span><br><small class="text-muted fw-bold mt-1 d-block">${r.invoice.totalAmount.toLocaleString()} đ</small>`)
                            : `<span class="badge bg-secondary bg-opacity-10 text-muted px-3 py-2 rounded-pill">Chưa chốt</span>`;

                        const actionBtn = r.invoice
                            ? `<div class="btn-group">
                                 <button class="btn btn-sm btn-outline-primary px-3" onclick="viewInvoiceDetails(${r.invoice.invoiceId})" title="Xem hóa đơn"><i class="bi bi-eye"></i> Xem</button>
                                 <button class="btn btn-sm btn-outline-danger px-3" onclick="deleteInvoice(${r.invoice.invoiceId})" title="Xóa để tính lại"><i class="bi bi-trash"></i> Xóa</button>
                               </div>`
                            : `<div class="btn-group">
                                 <button class="btn btn-sm btn-outline-warning px-3" onclick="showRecordMeterModal(${r.roomId}, '${r.roomCode}')"><i class="bi bi-pencil-square"></i> Số điện/nước</button>
                                 <button class="btn btn-sm btn-premium px-3" onclick="generateInvoice(${r.roomId})"><i class="bi bi-calculator"></i> Tính tiền</button>
                               </div>`;

                        return `
                            <tr>
                                <td><div class="fw-bold fs-6 text-primary">Phòng ${r.roomCode}</div></td>
                                <td>${elecHtml}</td>
                                <td>${waterHtml}</td>
                                <td>${invoiceStatus}</td>
                                <td>${actionBtn}</td>
                            </tr>
                        `;
                    }).join('');
                } else {
                    container.innerHTML = `<tr><td colspan="5" class="text-center py-5 text-danger">${result.message || 'Lỗi tải dữ liệu.'}</td></tr>`;
                }
            } catch (e) {
                console.error('loadBillingData error:', e);
                container.innerHTML = '<tr><td colspan="5" class="text-center py-5 text-danger">Lỗi khi tải dữ liệu hóa đơn.</td></tr>';
            }
        }


window.loadBillingData = loadBillingData;
        async function showRecordMeterModal(roomId, roomCode) {
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
        async function generateInvoice(roomId) {
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
                                <thead class="bg-light">
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
                } else {
                    container.innerHTML = `<div class="alert alert-danger">${result.message}</div>`;
                }
            } catch (e) {
                container.innerHTML = `<div class="alert alert-danger">Lỗi khi tải chi tiết hóa đơn.</div>`;
            }
        }

        async function deleteInvoice(invoiceId) {
            if (confirm(`Bạn có chắc chắn muốn XÓA hóa đơn này? Bạn có thể "Tính tiền" lại sau khi xóa.`)) {
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
