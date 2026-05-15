/**
 * QuanTro Tenant - Core UI & Auth Logic
 * Consolidated from _TenantLayout.cshtml
 */

window.toggleTheme = function() {
    const currentTheme = document.documentElement.getAttribute('data-theme');
    const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
    document.documentElement.setAttribute('data-theme', newTheme);
    localStorage.setItem('tenant-theme', newTheme);
    updateThemeIcon(newTheme);
};

window.updateThemeIcon = function(theme) {
    const icon = document.querySelector('#theme-btn i');
    if (!icon) return;
    icon.className = theme === 'dark' ? 'bi bi-sun-fill' : 'bi bi-moon-stars';
};

window.showPremiumToast = function(title, message, type = 'primary') {
    const toastEl = document.getElementById('liveToast');
    const iconEl = document.getElementById('toastIcon');
    const titleEl = document.getElementById('toastTitle');
    const msgEl = document.getElementById('toastMessage');
    
    if (!toastEl || !iconEl || !titleEl || !msgEl) return;

    titleEl.innerText = title;
    msgEl.innerText = message;
    
    // Reset and set icon/color
    iconEl.className = 'bi me-3 fs-4';
    if (type === 'success') {
        iconEl.classList.add('bi-check-circle-fill', 'text-success');
    } else if (type === 'danger' || type === 'error') {
        iconEl.classList.add('bi-exclamation-octagon-fill', 'text-danger');
    } else if (type === 'warning') {
        iconEl.classList.add('bi-exclamation-triangle-fill', 'text-warning');
    } else {
        iconEl.classList.add('bi-info-circle-fill', 'text-primary');
    }

    const toast = new bootstrap.Toast(toastEl);
    toast.show();
};

window.logout = async function() {
    try { 
        await fetch('/api/UserProxy/Logout', { method: 'POST' }); 
    } catch (e) {
        console.error("Logout failed:", e);
    }
    localStorage.removeItem('currentUser');
    window.location.href = '/Account/Login';
};

function initTenantAuth() {
    const userJson = localStorage.getItem('currentUser');
    if (userJson) {
        const user = JSON.parse(userJson);
        const nameEl = document.getElementById('navUserName');
        const avatarEl = document.getElementById('navAvatar');
        const welcomeEl = document.getElementById('tenant-welcome');
        
        if (nameEl) nameEl.innerText = user.name;
        if (avatarEl) avatarEl.src = user.avatar || `https://ui-avatars.com/api/?name=${encodeURIComponent(user.name)}&background=6366f1&color=fff`;
        if (welcomeEl) welcomeEl.innerText = `Chào ${user.name}!`;
    } else {
        window.location.href = '/Account/Login';
    }
}

document.addEventListener('DOMContentLoaded', () => {
    const theme = localStorage.getItem('tenant-theme') || 'light';
    updateThemeIcon(theme);
    initTenantAuth();
});
