/**
 * Tenant Billing Management Module
 */
const BillingMgmt = (() => {
    const init = () => {
        loadInvoices();
    };

    const loadInvoices = async () => {
        const container = document.getElementById('invoice-list-container');
        if (!container) return;

        try {
            const response = await fetch('/api/TenantPortal/MyInvoices');
            const result = await response.json();
            
            if (result.success) {
                const data = result.data;
                const invoices = data.invoices || [];
                const balance = data.balance || 0;

                updateSummaryCards(balance, invoices);
                
                if (invoices.length === 0) {
                    container.innerHTML = '<div class="text-center py-5 opacity-50"><i class="bi bi-receipt fs-1 d-block mb-3"></i>Chưa có hóa đơn nào.</div>';
                    return;
                }
                renderInvoices(invoices);
            }
        } catch (e) {
            container.innerHTML = '<div class="alert alert-danger">Lỗi tải dữ liệu.</div>';
        }
    };

    const updateSummaryCards = (balance, invoices) => {
        const balanceEl = document.getElementById('current-balance');
        const statusEl = document.getElementById('balance-status');
        const paidEl = document.getElementById('paid-this-month');
        const pendingEl = document.getElementById('total-pending');
        const countEl = document.getElementById('invoice-count');

        if (balanceEl) balanceEl.innerText = `${balance.toLocaleString('vi-VN')} đ`;
        if (statusEl) {
            if (balance > 0) {
                statusEl.innerHTML = '<span class="text-success"><i class="bi bi-plus-circle-fill me-1"></i>Bạn đang đóng dư tiền</span>';
            } else if (balance < 0) {
                statusEl.innerHTML = '<span class="text-danger"><i class="bi bi-exclamation-circle-fill me-1"></i>Bạn còn khoản nợ cần trả</span>';
            } else {
                statusEl.innerHTML = '<span class="text-muted"><i class="bi bi-check-circle-fill me-1"></i>Số dư hiện tại bằng 0</span>';
            }
        }

        const now = new Date();
        const curMonth = now.getMonth() + 1;
        const curYear = now.getFullYear();

        const paidThisMonth = invoices
            .filter(i => i.billingMonth === curMonth && i.billingYear === curYear)
            .reduce((sum, i) => sum + (i.paidAmount || 0), 0);
        
        const totalPending = invoices
            .filter(i => i.invoiceStatus !== 'Paid')
            .reduce((sum, i) => sum + (i.totalAmount - (i.paidAmount || 0)), 0);

        if (paidEl) paidEl.innerText = `${paidThisMonth.toLocaleString('vi-VN')} đ`;
        if (pendingEl) pendingEl.innerText = `${totalPending.toLocaleString('vi-VN')} đ`;
        if (countEl) countEl.innerText = invoices.length;
    };

    const renderInvoices = (data) => {
        const container = document.getElementById('invoice-list-container');
        container.innerHTML = data.map(i => `
            <div class="glass-card mb-3 p-4 border-0 shadow-sm animate-fade-in hover-lift">
                <div class="d-flex justify-content-between align-items-center">
                    <div class="d-flex align-items-center gap-4">
                        <div class="bg-primary bg-opacity-10 text-primary p-3 rounded-4" style="width: 60px; height: 60px; display: flex; align-items: center; justify-content: center;">
                            <i class="bi bi-receipt fs-3"></i>
                        </div>
                        <div>
                            <div class="small text-muted mb-1">Hóa đơn kỳ tháng ${i.billingMonth}/${i.billingYear}</div>
                            <h5 class="fw-bold mb-0">Phòng ${i.roomCode || ''}</h5>
                            <div class="d-flex gap-2 mt-2">
                                <span class="badge ${i.invoiceStatus === 'Paid' ? 'bg-success' : (i.invoiceStatus === 'Pending' ? 'bg-warning' : 'bg-danger')} bg-opacity-10 ${i.invoiceStatus === 'Paid' ? 'text-success' : (i.invoiceStatus === 'Pending' ? 'text-warning' : 'text-danger')} rounded-pill px-3">
                                    ${i.invoiceStatus === 'Paid' ? 'Đã thanh toán' : (i.invoiceStatus === 'Pending' ? 'Chờ duyệt' : 'Chưa thanh toán')}
                                </span>
                                ${i.paidAmount > 0 && i.invoiceStatus !== 'Paid' ? `<span class="badge bg-info bg-opacity-10 text-info rounded-pill px-3">Đã đóng ${i.paidAmount.toLocaleString('vi-VN')} đ</span>` : ''}
                            </div>
                        </div>
                    </div>
                    <div class="text-end">
                        <div class="small text-muted mb-1">Tổng cộng</div>
                        <div class="fw-bold fs-4 text-primary">${i.totalAmount.toLocaleString('vi-VN')} đ</div>
                        <div class="mt-3">
                            <button class="btn btn-premium btn-sm rounded-pill px-4" onclick="BillingMgmt.viewDetail(${i.invoiceId})">
                                Chi tiết
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `).join('');
    };

    const viewDetail = async (id) => {
        try {
            const response = await fetch(`/api/TenantPortal/GetInvoiceDetail/${id}`);
            const result = await response.json();
            if (result.success) {
                const i = result.data;
                const detailContent = document.getElementById('invoice-detail-content');
                
                let html = `
                    <div class="mb-4 text-center">
                        <h4 class="fw-bold">CHI TIẾT HÓA ĐƠN</h4>
                        <p class="text-muted">Phòng ${i.roomCode} - Tháng ${i.billingMonth}/${i.billingYear}</p>
                    </div>
                    <table class="table table-borderless">
                        <thead>
                            <tr class="border-bottom">
                                <th class="small text-muted">DỊCH VỤ</th>
                                <th class="small text-muted text-center">SỐ LƯỢNG</th>
                                <th class="small text-muted text-end">THÀNH TIỀN</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td class="fw-bold">Tiền thuê phòng</td>
                                <td class="text-center">1</td>
                                <td class="text-end fw-bold">${i.roomRent.toLocaleString('vi-VN')} đ</td>
                            </tr>
                            ${i.details.map(d => `
                                <tr>
                                    <td>${d.serviceName} <br/><small class="text-muted">${d.description}</small></td>
                                    <td class="text-center">${d.quantity}</td>
                                    <td class="text-end fw-bold">${d.subTotal.toLocaleString('vi-VN')} đ</td>
                                </tr>
                            `).join('')}
                        </tbody>
                        <tfoot>
                            <tr class="border-top">
                                <td colspan="2" class="fw-bold pt-3 fs-5">TỔNG CỘNG</td>
                                <td class="text-end fw-bold pt-3 fs-5 text-primary">${i.totalAmount.toLocaleString('vi-VN')} đ</td>
                            </tr>
                        </tfoot>
                    </table>
                `;
                
                detailContent.innerHTML = html;
                const modalEl = document.getElementById('invoiceDetailModal');
                if (modalEl && modalEl.parentNode !== document.body) {
                    document.body.appendChild(modalEl);
                }
                const modal = new bootstrap.Modal(modalEl);
                
                // Add click handler to Pay Now button in modal footer
                const payBtn = document.querySelector('#invoiceDetailModal .btn-premium');
                if (payBtn) {
                    payBtn.onclick = () => {
                        window.location.href = `/QRPayment/${id}`;
                    };
                    // Hide if already paid
                    // Hide if already paid or pending
                    payBtn.style.display = (i.status === 'Paid' || i.status === 'Pending') ? 'none' : 'block';
                }
                
                modal.show();
            }
        } catch (e) {
            showPremiumToast("Lỗi", "Không thể lấy thông tin hóa đơn.", "danger");
        }
    };

    const payNow = (id) => {
        window.location.href = `/QRPayment/${id}`;
    };

    return { init, viewDetail, payNow };
})();

document.addEventListener('DOMContentLoaded', BillingMgmt.init);
