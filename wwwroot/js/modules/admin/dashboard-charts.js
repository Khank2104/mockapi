/**
 * QuanTro Dashboard - Chart Utilities
 * Provides chart initialization and data visualization helpers
 */

// Chart.js Configuration
Chart.defaults.font.family = "'Outfit', sans-serif";
Chart.defaults.font.size = 13;
Chart.defaults.color = '#64748b';
Chart.defaults.plugins.tooltip.padding = 12;
Chart.defaults.plugins.tooltip.cornerRadius = 12;

/**
 * Initialize Revenue Chart
 * @param {string} canvasId - Canvas element ID
 * @param {object} data - Chart data
 */
function initRevenueChart(canvasId, data) {
    const ctx = document.getElementById(canvasId)?.getContext('2d');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.labels || ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
            datasets: [
                {
                    label: 'Revenue',
                    data: data.revenue || [0, 0, 0, 0, 0, 0],
                    borderColor: '#6366f1',
                    backgroundColor: 'rgba(99, 102, 241, 0.1)',
                    tension: 0.4,
                    fill: true,
                    borderWidth: 2,
                    pointRadius: 4,
                    pointBackgroundColor: '#6366f1',
                    pointBorderColor: 'white',
                    pointBorderWidth: 2,
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: true,
                    position: 'top',
                },
                tooltip: {
                    backgroundColor: 'rgba(31, 41, 55, 0.8)',
                    titleColor: 'white',
                    bodyColor: 'white',
                    borderColor: 'rgba(99, 102, 241, 0.3)',
                    borderWidth: 1,
                    padding: 12,
                    displayColors: true,
                    callbacks: {
                        label: function(context) {
                            return 'Revenue: ' + formatCurrency(context.parsed.y);
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: function(value) {
                            return formatCurrency(value);
                        }
                    }
                }
            }
        }
    });
}

/**
 * Initialize Occupancy Chart
 * @param {string} canvasId - Canvas element ID
 * @param {object} data - Chart data
 */
function initOccupancyChart(canvasId, data) {
    const ctx = document.getElementById(canvasId)?.getContext('2d');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Occupied', 'Available', 'Maintenance'],
            datasets: [
                {
                    data: data.occupancy || [65, 25, 10],
                    backgroundColor: [
                        '#6366f1', // primary indigo
                        '#e2e8f0', // light slate
                        '#f43f5e'  // rose
                    ],
                    hoverOffset: 10,
                    borderWidth: 0,
                    cutout: '80%',
                    borderRadius: 20
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    position: 'bottom',
                },
                tooltip: {
                    backgroundColor: 'rgba(31, 41, 55, 0.8)',
                    titleColor: 'white',
                    bodyColor: 'white',
                    padding: 12,
                }
            }
        }
    });
}

/**
 * Initialize Invoice Status Chart
 * @param {string} canvasId - Canvas element ID
 * @param {object} data - Chart data
 */
function initInvoiceStatusChart(canvasId, data) {
    const ctx = document.getElementById(canvasId)?.getContext('2d');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'bar',
        data: {
            labels: ['Paid', 'Pending', 'Overdue'],
            datasets: [
                {
                    label: 'Invoices',
                    data: data.invoiceStatus || [45, 30, 15],
                    backgroundColor: [
                        'rgba(16, 185, 129, 0.8)',  // green - paid
                        'rgba(99, 102, 241, 0.8)', // indigo - pending
                        'rgba(239, 68, 68, 0.8)'   // red - overdue
                    ],
                    borderRadius: 8,
                    borderSkipped: false,
                }
            ]
        },
        options: {
            indexAxis: 'y',
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: false,
                }
            },
            scales: {
                x: {
                    beginAtZero: true,
                }
            }
        }
    });
}

/**
 * Initialize Payment Methods Chart
 * @param {string} canvasId - Canvas element ID
 * @param {object} data - Chart data
 */
function initPaymentMethodsChart(canvasId, data) {
    const ctx = document.getElementById(canvasId)?.getContext('2d');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'pie',
        data: {
            labels: ['Cash', 'Bank Transfer', 'E-Wallet'],
            datasets: [
                {
                    data: data.paymentMethods || [40, 35, 25],
                    backgroundColor: [
                        'rgba(107, 114, 128, 0.8)',    // gray - cash
                        'rgba(59, 130, 246, 0.8)',     // blue - transfer
                        'rgba(168, 85, 247, 0.8)'      // purple - ewallet
                    ],
                    borderColor: 'white',
                    borderWidth: 2,
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    position: 'right',
                },
            }
        }
    });
}

/**
 * Initialize Tenant Satisfaction Chart
 * @param {string} canvasId - Canvas element ID
 * @param {object} data - Chart data
 */
function initSatisfactionChart(canvasId, data) {
    const ctx = document.getElementById(canvasId)?.getContext('2d');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'radar',
        data: {
            labels: ['Cleanliness', 'Management', 'Amenities', 'Response Time', 'Value for Money'],
            datasets: [
                {
                    label: 'Satisfaction Score',
                    data: data.satisfaction || [80, 75, 85, 90, 80],
                    borderColor: 'rgba(99, 102, 241, 1)',
                    backgroundColor: 'rgba(99, 102, 241, 0.2)',
                    pointBackgroundColor: 'rgba(99, 102, 241, 1)',
                    pointBorderColor: 'white',
                    pointBorderWidth: 2,
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            scales: {
                r: {
                    beginAtZero: true,
                    max: 100,
                    ticks: {
                        stepSize: 20,
                        color: '#9ca3af',
                    },
                    grid: {
                        color: 'rgba(99, 102, 241, 0.1)',
                    }
                }
            },
            plugins: {
                legend: {
                    position: 'bottom',
                }
            }
        }
    });
}

/**
 * Format number as currency VND
 * @param {number} value - Value to format
 * @returns {string} Formatted currency string
 */
function formatCurrency(value) {
    if (!value) return '₫0';
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
    }).format(value);
}

/**
 * Format number as percentage
 * @param {number} value - Value to format
 * @returns {string} Formatted percentage
 */
function formatPercentage(value) {
    return (value || 0).toFixed(1) + '%';
}

/**
 * Format date to locale string
 * @param {Date|string} date - Date to format
 * @returns {string} Formatted date
 */
function formatDate(date) {
    if (!date) return '-';
    return new Date(date).toLocaleDateString('vi-VN');
}

/**
 * Initialize dashboard statistics with animations
 */
function initStatistics() {
    const stats = document.querySelectorAll('[data-stat-value]');
    stats.forEach(stat => {
        const targetValue = parseFloat(stat.dataset.statValue);
        const finalValue = stat.textContent.trim();
        
        // Animate number counting if it's a number
        if (!isNaN(targetValue)) {
            animateValue(stat, 0, targetValue, 1000);
        }
    });
}

/**
 * Animate value change
 * @param {Element} element - Element to update
 * @param {number} start - Start value
 * @param {number} end - End value
 * @param {number} duration - Animation duration in ms
 */
function animateValue(element, start, end, duration) {
    const startTime = Date.now();
    const animate = () => {
        const progress = Math.min((Date.now() - startTime) / duration, 1);
        const value = Math.floor(start + (end - start) * progress);
        element.textContent = value.toLocaleString('vi-VN');
        
        if (progress < 1) {
            requestAnimationFrame(animate);
        }
    };
    animate();
}

/**
 * Initialize real-time data updates
 */
function initRealTimeUpdates() {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub")
        .withAutomaticReconnect()
        .build();

    connection.start().catch(err => console.error(err));

    connection.on("NewInvoice", (data) => {
        updateDashboard();
    });

    connection.on("PaymentReceived", (data) => {
        updateDashboard();
    });

    connection.on("OccupancyChanged", (data) => {
        updateDashboard();
    });
}

/**
 * Update dashboard data
 */
async function updateDashboard() {
    try {
        // Fetch updated statistics
        const response = await fetch('/api/admin/dashboard-stats', {
            headers: {
                'Authorization': `Bearer ${getToken()}`
            }
        });

        if (response.ok) {
            const data = await response.json();
            updateStatisticCards(data);
            // Re-render charts if needed
        }
    } catch (error) {
        console.error('Dashboard update error:', error);
    }
}

/**
 * Update statistic cards
 */
function updateStatisticCards(data) {
    // Update each stat card with new data
    if (data.totalRevenue) {
        document.getElementById('totalRevenue').textContent = formatCurrency(data.totalRevenue);
    }
    if (data.totalTenants) {
        document.getElementById('totalTenants').textContent = data.totalTenants;
    }
    if (data.occupancyRate) {
        document.getElementById('occupancyRate').textContent = formatPercentage(data.occupancyRate);
    }
}

/**
 * Get JWT token from localStorage
 */
function getToken() {
    return localStorage.getItem('access_token') || '';
}

// Initialize on DOM ready
document.addEventListener('DOMContentLoaded', () => {
    initStatistics();
    initRealTimeUpdates();
});
