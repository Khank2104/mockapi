        async function loadAllRequestsData() {
            const container = document.getElementById('all-requests-list-container');
            try {
                const response = await fetch('/api/Request/GetAllRequests');
                const result = await response.json();
                if (result.success) {
                    if (result.data.length === 0) {
                        container.innerHTML = '<tr><td colspan="6" class="text-center py-5">Không có yêu cầu nào.</td></tr>';
                        return;
                    }
                    container.innerHTML = result.data.map(r => `
                        <tr>
                            <td><span class="badge bg-info bg-opacity-10 text-info px-3 py-2 rounded-pill">Phòng ${r.roomId}</span></td>
                            <td><div class="fw-bold">${r.title}</div></td>
                            <td><span class="badge border border-muted text-muted x-small">${r.requestType}</span></td>
                            <td>
                                <span class="status-pill ${r.status === 'Pending' ? 'status-locked' : 'status-active'}">
                                    <i class="bi bi-circle-fill me-1" style="font-size: 0.4rem;"></i> ${r.status}
                                </span>
                            </td>
                            <td class="text-muted small">${new Date(r.createdAt).toLocaleString('vi-VN')}</td>
                            <td>
                                <button class="btn btn-sm btn-outline-primary rounded-pill px-3" onclick="showHandleRequestModal(${r.requestId}, '${r.title}')">
                                    Xử lý ngay
                                </button>
                            </td>
                        </tr>
                    `).join('');
                }
            } catch (e) {}
        }

        // --- Meter Reading Logic ---
        let currentServices = [];

window.loadAllRequestsData = loadAllRequestsData;
