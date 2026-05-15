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
                    
                    document.getElementById('label-occupied').innerText = occupied;
                    document.getElementById('label-vacant').innerText = total - occupied;
                    
                    renderChart(occupied, total - occupied);
                }
                
                await loadFinancialDashboard();
                await loadPendingRequestsCount();
            } catch (e) {
                console.error("Failed to load overview", e);
            }
        }

        async function loadFinancialDashboard() {
            try {
                const now = new Date();
                const month = now.getMonth() + 1;
                const year = now.getFullYear();
                
                const response = await fetch(`/api/Invoice/FinancialDashboard?month=${month}&year=${year}`);
                const result = await response.json();
                
                if (result.success) {
                    const data = result.data;
                    document.getElementById('stat-revenue').innerText = `${data.totalExpected.toLocaleString()}đ`;
                    
                    document.getElementById('dash-total-expected').innerText = `${data.totalExpected.toLocaleString()}đ`;
                    document.getElementById('dash-total-collected').innerText = `${data.totalCollected.toLocaleString()}đ`;
                    document.getElementById('dash-paid-count').innerText = data.paidCount;
                    document.getElementById('dash-unpaid-count').innerText = data.unpaidCount;
                    
                    const list = document.getElementById('dash-recent-payments');
                    if (data.lastPayments && data.lastPayments.length > 0) {
                        list.innerHTML = data.lastPayments.map(p => `
                            <tr>
                                <td><div class="fw-bold text-primary">Phòng ${p.roomCode}</div></td>
                                <td class="fw-bold text-success">+${p.amount.toLocaleString()}đ</td>
                                <td><span class="badge bg-light text-dark fw-normal">${p.method}</span></td>
                                <td class="text-muted small">${new Date(p.date).toLocaleDateString('vi-VN')}</td>
                            </tr>
                        `).join('');
                    } else {
                        list.innerHTML = '<tr><td colspan="4" class="text-center py-4 text-muted">Chưa có khoản thu nào trong tháng này.</td></tr>';
                    }
                }
            } catch (e) {
                console.error("Error loading financial dashboard", e);
            }
        }

        async function loadPendingRequestsCount() {
            try {
                const response = await fetch('/api/Request/GetAllRequests');
                const result = await response.json();
                if (result.success) {
                    const requests = result.data.requests || [];
                    const pendingCount = requests.filter(r => r.status === 'Pending').length;
                    const statRequestsEl = document.getElementById('stat-requests');
                    if (statRequestsEl) statRequestsEl.innerText = pendingCount;
                }
            } catch (e) {
                console.error("Error loading requests count", e);
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

        // Initialization
        document.addEventListener('DOMContentLoaded', () => {
            loadOverviewData();
        });

window.loadOverviewData = loadOverviewData;
window.renderChart = renderChart;
window.loadFinancialDashboard = loadFinancialDashboard;
