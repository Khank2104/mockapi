/**
 * QuanTro Tenant - Notifications & SignalR Logic
 * Consolidated from _TenantLayout.cshtml
 */

(function() {
    // SignalR Initialization
    if (typeof signalR !== 'undefined') {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/notificationHub")
            .withAutomaticReconnect()
            .build();

        connection.on("NewNotification", (userId) => {
            if (typeof showPremiumToast === 'function') {
                showPremiumToast("Thông báo", "Bạn có thông báo mới trong hệ thống.", "info");
            }
            loadNotificationsCount();
        });

        connection.start().catch(err => console.error("SignalR Connection Error: ", err.toString()));
    }

    document.addEventListener('DOMContentLoaded', () => {
        loadNotificationsCount();
    });
})();

async function loadNotificationsCount() {
    try {
        const res = await fetch('/api/NotificationProxy/GetMyNotifications');
        const result = await res.json();
        if (result.success) {
            const count = result.unreadCount;
            const badge = document.getElementById('notification-count');
            if (badge) {
                if (count > 0) {
                    badge.innerText = count > 99 ? '99+' : count;
                    badge.classList.remove('d-none');
                } else {
                    badge.classList.add('d-none');
                }
            }
        }
    } catch (e) {
        console.error("Error loading notification count:", e);
    }
}

window.toggleNotificationModal = async function() {
    const modalEl = document.getElementById('notificationListModal');
    if (!modalEl) return;
    
    const modal = new bootstrap.Modal(modalEl);
    modal.show();
    
    const list = document.getElementById('notification-list');
    if (!list) return;

    try {
        const res = await fetch('/api/NotificationProxy/GetMyNotifications');
        const result = await res.json();
        if (result.success) {
            if (result.data.length === 0) {
                list.innerHTML = '<div class="text-center py-4 opacity-50">Không có thông báo nào.</div>';
            } else {
                list.innerHTML = result.data.map(n => `
                    <div class="p-3 mb-2 rounded-3 ${n.isRead ? 'bg-secondary bg-opacity-10 opacity-75' : 'bg-primary bg-opacity-10 border-start border-primary border-4'}" 
                         onclick="markNotificationAsRead(${n.id}, this)" style="cursor: pointer;">
                        <div class="d-flex justify-content-between align-items-start mb-1">
                            <span class="fw-bold small">${n.title}</span>
                            <span class="x-small text-muted">${new Date(n.createdAt).toLocaleDateString('vi-VN')}</span>
                        </div>
                        <div class="small text-muted">${n.message}</div>
                    </div>
                `).join('');
            }
        }
    } catch (e) {
        list.innerHTML = '<div class="alert alert-danger">Lỗi tải thông báo.</div>';
    }
};

window.markNotificationAsRead = async function(id, element) {
    try {
        const res = await fetch(`/api/NotificationProxy/MarkAsRead/${id}`, { method: 'POST' });
        const result = await res.json();
        if (result.success) {
            element.classList.remove('bg-primary', 'bg-opacity-10', 'border-start', 'border-primary', 'border-4');
            element.classList.add('bg-secondary bg-opacity-10', 'opacity-75');
            loadNotificationsCount();
        }
    } catch (e) {
        console.error("Error marking notification as read:", e);
    }
};

window.markAllNotificationsAsRead = async function() {
    try {
        const res = await fetch('/api/NotificationProxy/MarkAllAsRead', { method: 'POST' });
        const result = await res.json();
        if (result.success) {
            const items = document.querySelectorAll('#notification-list > div');
            items.forEach(el => {
                el.classList.remove('bg-primary', 'bg-opacity-10', 'border-start', 'border-primary', 'border-4');
                el.classList.add('bg-secondary bg-opacity-10', 'opacity-75');
            });
            loadNotificationsCount();
        }
    } catch (e) {
        console.error("Error marking all notifications as read:", e);
    }
};

window.loadNotificationsCount = loadNotificationsCount;
