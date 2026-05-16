/**
 * QuanTro Admin - Core UI & Auth Logic
 * Consolidated from _AdminLayout.cshtml
 */

window.toggleTheme = function() {
    const html = document.documentElement;
    const currentTheme = html.getAttribute('data-theme');
    const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
    
    html.setAttribute('data-theme', newTheme);
    localStorage.setItem('theme', newTheme);
    updateThemeIcon(newTheme);
};

window.updateThemeIcon = function(theme) {
    const icon = document.getElementById('theme-icon');
    if (icon) {
        icon.className = theme === 'dark' ? 'bi bi-sun-fill text-warning' : 'bi bi-moon-stars';
    }
};

window.toggleMobileMenu = function() {
    const sidebar = document.querySelector('.sidebar');
    if (sidebar) sidebar.classList.toggle('active');
};

window.logout = async function() {
    try {
        await fetch('/api/UserProxy/Logout', { method: 'POST' });
    } catch (e) {
        console.error("Logout API failed", e);
    }
    localStorage.removeItem('currentUser');
    window.location.href = '/Account/Login';
};

/**
 * Initialize Auth & Greetings
 */
function initAdminAuth() {
    const hour = new Date().getHours();
    const greetingEl = document.getElementById('dynamic-greeting');
    if (greetingEl) {
        const now = new Date();
        const options = { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' };
        const dateStr = now.toLocaleDateString('vi-VN', options);
        
        let message = "Chào buổi sáng";
        if (hour >= 12 && hour < 18) message = "Chào buổi chiều";
        if (hour >= 18) message = "Chào buổi tối";
        
        greetingEl.innerText = `${message}, hôm nay là ${dateStr}`;
    }

    const userJson = localStorage.getItem('currentUser');
    if (userJson) {
        const user = JSON.parse(userJson);
        const nameEl = document.getElementById('navUserName');
        const avatarEl = document.getElementById('navAvatar');
        
        if (nameEl) nameEl.innerText = user.name;
        if (avatarEl) avatarEl.src = user.avatar || `https://ui-avatars.com/api/?name=${encodeURIComponent(user.name)}&background=6366f1&color=fff`;
        
        if (user.role && user.role.toLowerCase() === 'superuser') {
            document.querySelectorAll('.superuser-only').forEach(el => el.classList.remove('d-none'));
        }
    } else {
        window.location.href = '/Account/Login';
    }
}

// Intercept switchModule to close sidebar on mobile
const originalSwitchModule = window.switchModule;
window.switchModule = function(id, el) {
    if (typeof originalSwitchModule === 'function') originalSwitchModule(id, el);
    if (window.innerWidth <= 768) {
        const sidebar = document.querySelector('.sidebar');
        if (sidebar) sidebar.classList.remove('active');
    }
};

document.addEventListener('DOMContentLoaded', () => {
    updateThemeIcon(document.documentElement.getAttribute('data-theme'));
    initAdminAuth();
});
