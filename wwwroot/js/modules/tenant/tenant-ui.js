/**
 * QuanTro Tenant - Core UI & Auth Logic
 * Unified with the Global Design System
 */

window.toggleTheme = function() {
    const currentTheme = document.documentElement.getAttribute('data-theme');
    const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
    document.documentElement.setAttribute('data-theme', newTheme);
    localStorage.setItem('theme', newTheme); // Unified key with Admin
    updateThemeIcon(newTheme);
};

window.toggleSidebar = function() {
    const sidebar = document.querySelector('.sidebar');
    if (sidebar) {
        sidebar.classList.toggle('active');
    }
};

window.updateThemeIcon = function(theme) {
    const icon = document.querySelector('#theme-btn i');
    if (!icon) return;
    icon.className = theme === 'dark' ? 'bi bi-sun-fill' : 'bi bi-moon-stars';
};

window.showPremiumToast = function(title, message, type = 'primary') {
    const toastEl = document.getElementById('liveToast');
    if (!toastEl) return;

    document.getElementById('toastTitle').innerText = title;
    document.getElementById('toastMessage').innerText = message;
    
    const iconEl = document.getElementById('toastIcon');
    iconEl.className = 'bi me-3 fs-4';
    
    const colors = {
        success: ['bi-check-circle-fill', 'text-success'],
        danger: ['bi-exclamation-octagon-fill', 'text-danger'],
        error: ['bi-exclamation-octagon-fill', 'text-danger'],
        warning: ['bi-exclamation-triangle-fill', 'text-warning']
    };

    const [iconClass, textClass] = colors[type] || ['bi-info-circle-fill', 'text-primary'];
    iconEl.classList.add(iconClass, textClass);

    const toast = new bootstrap.Toast(toastEl);
    toast.show();
};

window.logout = async function() {
    try { 
        await fetch('/api/UserProxy/Logout', { method: 'POST' }); 
    } finally {
        localStorage.removeItem('currentUser');
        window.location.href = '/Account/Login';
    }
};

function initTenantUI() {
    const userJson = localStorage.getItem('currentUser');
    if (userJson) {
        try {
            const user = JSON.parse(userJson);
            const nameEl = document.getElementById('navUserName');
            const initialEl = document.getElementById('navInitial');
            const welcomeEl = document.getElementById('tenant-welcome');
            
            if (nameEl) nameEl.innerText = user.name || 'Cư dân';
            if (initialEl && user.name) {
                initialEl.innerText = user.name.charAt(0).toUpperCase();
            }
            if (welcomeEl) welcomeEl.innerText = `Chào ${user.name || 'bạn'}!`;
        } catch (e) {
            console.error("Failed to parse user data", e);
        }
    }
    
    // Highlight active nav link
    const currentPath = window.location.pathname;
    document.querySelectorAll('.sidebar .nav-link').forEach(link => {
        if (link.getAttribute('href') === currentPath) {
            link.classList.add('active');
        } else {
            link.classList.remove('active');
        }
    });
}

document.addEventListener('DOMContentLoaded', () => {
    const theme = localStorage.getItem('theme') || 'light';
    document.documentElement.setAttribute('data-theme', theme);
    updateThemeIcon(theme);
    initTenantUI();
});
