        async function loadOverviewData() {
            try {
                const response = await fetch('/api/MotelManagement/MyMotels');
                const result = await response.json();
                
                if (result.success) {
                    let total = 0, occupied = 0;
                    result.data.forEach(m => {
                        m.floors.forEach(f => {
                            total += f.rooms.length;
                            occupied += f.rooms.filter(r => r.status === 'Occupied').length;
                        });
                    });

                    document.getElementById('stat-total-rooms').innerText = total;
                    document.getElementById('stat-occupied-rooms').innerText = occupied;
                    
                    const rate = total > 0 ? Math.round((occupied / total) * 100) : 0;
                    document.getElementById('label-occupancy-rate').innerText = `${rate}%`;
                    
                    // Mock revenue calculation
                    const mockRevenue = occupied * 3500000; 
                    document.getElementById('stat-revenue').innerText = `${mockRevenue.toLocaleString()}đ`;

                    document.getElementById('label-occupied').innerText = occupied;
                    document.getElementById('label-vacant').innerText = total - occupied;
                    
                    renderChart(occupied, total - occupied);
                }
                
                loadRecentRequests();
            } catch (e) {
                console.error("Failed to load overview", e);
            }
        }

        let occupancyChartInstance;

        function renderChart(occupied, vacant) {
            const chartEl = document.getElementById('occupancyChart');
            if (!chartEl) return;
            const ctx = chartEl.getContext('2d');
            if (occupancyChartInstance) occupancyChartInstance.destroy();
            
            occupancyChartInstance = new Chart(ctx, {
                type: 'doughnut',
                data: {
                    datasets: [{
                        data: [occupied, vacant],
                        backgroundColor: ['#6366f1', '#e2e8f0'],
                        borderWidth: 0,
                        cutout: '80%',
                        borderRadius: 20
                    }]
                },
                options: {
                    maintainAspectRatio: false,
                    animation: {
                        animateRotate: true,
                        animateScale: true
                    },
                    plugins: { 
                        legend: { display: false },
                        tooltip: {
                            enabled: true,
                            backgroundColor: 'rgba(15, 23, 42, 0.9)',
                            padding: 12,
                            cornerRadius: 12
                        }
                    }
                }
            });
        }

        async function loadRecentRequests() {
            try {
                const response = await fetch('/api/Request/GetAllRequests');
                const result = await response.json();
                const list = document.getElementById('recent-requests-list');
                if (!list) return;

                if (result.success) {
                    const requests = result.data.requests || [];
                    const role = result.data.role;
                    const isSuper = role === 'superuser';

                    const pendingCount = requests.filter(r => r.status === 'Pending').length;
                    const statRequestsEl = document.getElementById('stat-requests');
                    if (statRequestsEl) statRequestsEl.innerText = pendingCount;
                    
                    const recent = requests.slice(0, 5);
                    if (recent.length === 0) {
                        list.innerHTML = '<tr><td colspan="4" class="text-center py-4 text-muted">Không có yêu cầu nào mới.</td></tr>';
                        return;
                    }

                    list.innerHTML = recent.map(r => `
                        <tr>
                            <td>
                                <div class="fw-bold text-primary small">Phòng ${r.roomCode || 'N/A'}</div>
                                ${isSuper ? `<div class="x-small text-muted">${r.motelName || ''}</div>` : ''}
                            </td>
                            <td>
                                <div class="small fw-bold">${r.title}</div>
                                <div class="x-small text-muted text-truncate" style="max-width: 150px;">${r.tenantName || ''}</div>
                            </td>
                            <td>
                                <span class="status-pill ${r.status === 'Pending' ? 'status-locked' : 'status-active'} x-small">
                                    <i class="bi bi-circle-fill me-1" style="font-size: 0.3rem;"></i> ${r.status}
                                </span>
                            </td>
                            <td class="text-muted x-small">${new Date(r.createdAt).toLocaleDateString('vi-VN')}</td>
                        </tr>
                    `).join('');
                }
            } catch (e) {
                console.error("Error loading recent requests", e);
            }
        }

        // Initialization
        document.addEventListener('DOMContentLoaded', () => {
            loadOverviewData();
        });

window.loadOverviewData = loadOverviewData;
window.renderChart = renderChart;
window.loadRecentRequests = loadRecentRequests;
