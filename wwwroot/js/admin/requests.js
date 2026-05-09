        async function loadAllRequestsData() {
            const container = document.getElementById('all-requests-list-container');
            const headerRow = document.querySelector('#module-requests thead tr');
            if (!container) return;
            
            try {
                const response = await fetch('/api/Request/GetAllRequests');
                const result = await response.json();
                
                if (result.success) {
                    const requests = result.data.requests;
                    const role = result.data.role;
                    const isSuper = role === 'superuser';

                    // Update Headers for Superuser
                    if (headerRow) {
                        const hasMotelCol = headerRow.innerText.includes('KHU TRỌ');
                        if (isSuper && !hasMotelCol) {
                            headerRow.insertAdjacentHTML('afterbegin', '<th class="ps-4">KHU TRỌ</th>');
                        } else if (!isSuper && hasMotelCol) {
                            headerRow.querySelector('th:first-child').remove();
                        }
                    }

                    if (requests.length === 0) {
                        container.innerHTML = `<tr><td colspan="${isSuper ? 7 : 6}" class="text-center py-5">Không có yêu cầu nào.</td></tr>`;
                        return;
                    }

                    container.innerHTML = requests.map(r => `
                        <tr>
                            ${isSuper ? `
                                <td class="ps-4">
                                    <div class="fw-bold text-primary small">${r.motelName || 'N/A'}</div>
                                </td>
                            ` : ''}
                            <td><span class="badge bg-info bg-opacity-10 text-info px-3 py-2 rounded-pill">Phòng ${r.roomCode || 'N/A'}</span></td>
                            <td>
                                <div class="fw-bold">${r.title}</div>
                                <div class="small text-muted">${r.tenantName || 'Ẩn danh'}</div>
                            </td>
                            <td><span class="badge border border-muted text-muted x-small">${r.requestType}</span></td>
                            <td>
                                <span class="status-pill ${r.status === 'Pending' ? 'status-locked' : 'status-active'}">
                                    <i class="bi bi-circle-fill me-1" style="font-size: 0.4rem;"></i> ${r.status}
                                </span>
                            </td>
                            <td class="text-muted small">${new Date(r.createdAt).toLocaleString('vi-VN')}</td>
                            <td>
                                <button class="btn btn-sm btn-outline-primary rounded-pill px-3" 
                                    onclick="showHandleRequestModal(${r.requestId}, \`${r.title.replace(/'/g, "\\'")}\`, \`${(r.description || '').replace(/'/g, "\\'").replace(/\n/g, '<br>')}\`, '${r.status}')">
                                    Xử lý ngay
                                </button>
                            </td>
                        </tr>
                    `).join('');
                }
            } catch (e) {
                console.error("Error loading requests:", e);
            }
        }

        function showHandleRequestModal(id, title, desc, status) {
            const modalEl = document.getElementById('handleRequestModal');
            if (!modalEl) return;
            const modal = new bootstrap.Modal(modalEl);
            document.getElementById('handle-request-id').value = id;
            document.getElementById('handle-request-title').innerText = title;
            document.getElementById('handle-request-desc').innerHTML = desc;
            document.getElementById('handle-request-status').value = status;
            document.getElementById('handle-request-note').value = '';
            modal.show();
        }

        async function submitHandleRequest() {
            const id = document.getElementById('handle-request-id').value;
            const status = document.getElementById('handle-request-status').value;
            const note = document.getElementById('handle-request-note').value;

            try {
                const response = await fetch(`/api/Request/UpdateStatus/${id}`, {
                    method: 'PATCH',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ status: status, resolutionNote: note })
                });
                const result = await response.json();
                if (result.success) {
                    showPremiumToast('Thành công', 'Đã cập nhật trạng thái yêu cầu', 'success');
                    const modalEl = document.getElementById('handleRequestModal');
                    const modal = bootstrap.Modal.getInstance(modalEl);
                    if (modal) modal.hide();
                    loadAllRequestsData();
                } else {
                    showPremiumToast('Lỗi', result.message || 'Lỗi cập nhật', 'danger');
                }
            } catch (e) {
                showPremiumToast('Lỗi', 'Lỗi hệ thống', 'danger');
            }
        }

        async function rejectRequest() {
            if (!confirm('Bạn có chắc chắn muốn từ chối yêu cầu này không?')) return;
            
            const id = document.getElementById('handle-request-id').value;
            const note = document.getElementById('handle-request-note').value || 'Yêu cầu bị từ chối do không hợp lệ hoặc spam.';

            try {
                const response = await fetch(`/api/Request/UpdateStatus/${id}`, {
                    method: 'PATCH',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ status: 'Rejected', resolutionNote: note })
                });
                const result = await response.json();
                if (result.success) {
                    showPremiumToast('Đã từ chối', 'Yêu cầu đã được chuyển sang trạng thái Từ chối', 'success');
                    const modalEl = document.getElementById('handleRequestModal');
                    const modal = bootstrap.Modal.getInstance(modalEl);
                    if (modal) modal.hide();
                    loadAllRequestsData();
                } else {
                    showPremiumToast('Lỗi', result.message || 'Lỗi cập nhật', 'danger');
                }
            } catch (e) {
                showPremiumToast('Lỗi', 'Lỗi hệ thống', 'danger');
            }
        }

window.loadAllRequestsData = loadAllRequestsData;
window.showHandleRequestModal = showHandleRequestModal;
window.submitHandleRequest = submitHandleRequest;
window.rejectRequest = rejectRequest;
