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
                if (result.data.length === 0) {
                    container.innerHTML = '<div class="text-center py-5 opacity-50"><i class="bi bi-receipt fs-1 d-block mb-3"></i>Chưa có hóa đơn nào.</div>';
                    return;
                }
                renderInvoices(result.data);
            }
        } catch (e) {
            container.innerHTML = '<div class="alert alert-danger">Lỗi tải dữ liệu.</div>';
        }
    };

    const renderInvoices = (data) => {
        const container = document.getElementById('invoice-list-container');
        container.innerHTML = data.map(i => `
            <div class="glass-card mb-3 p-3 border-0 shadow-sm animate-fade-in">
                <div class="d-flex justify-content-between align-items-center">
                    <div class="d-flex align-items-center gap-3">
                        <div class="bg-primary bg-opacity-10 text-primary p-3 rounded-4">
                            <i class="bi bi-calendar2-event fs-4"></i>
                        </div>
                        <div>
                            <h5 class="fw-bold mb-0">Tháng ${i.billingMonth}/${i.billingYear}</h5>
                            <span class="status-pill status-${i.invoiceStatus.toLowerCase() === 'paid' ? 'active' : 'locked'} mt-1">
                                ${i.invoiceStatus === 'Paid' ? 'Đã thanh toán' : 'Chưa thanh toán'}
                            </span>
                        </div>
                    </div>
                    <div class="text-end">
                        <div class="fw-bold fs-5 text-primary">${i.totalAmount.toLocaleString('vi-VN')} đ</div>
                        <button class="btn btn-sm btn-outline-primary rounded-pill px-3 mt-2" onclick="BillingMgmt.viewDetail(${i.invoiceId})">
                            Xem chi tiết
                        </button>
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
                new bootstrap.Modal(document.getElementById('invoiceDetailModal')).show();
            }
        } catch (e) {
            showPremiumToast("Lỗi", "Không thể lấy thông tin hóa đơn.", "danger");
        }
    };

    return { init, viewDetail };
})();

document.addEventListener('DOMContentLoaded', BillingMgmt.init);
