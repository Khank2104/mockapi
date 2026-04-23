let currentUser = null;

document.addEventListener('DOMContentLoaded', () => {
    currentUser = JSON.parse(localStorage.getItem('currentUser'));
    if (!currentUser) {
        window.location.href = '/Account/Login';
        return;
    }
    setupDashboard();
});

function setupDashboard() {
    document.getElementById('welcomeText').innerText = `Welcome back, ${currentUser.name}!`;
    document.getElementById('userName').innerText = currentUser.name;
    document.getElementById('userEmail').innerText = currentUser.email;
    document.getElementById('userAvatar').src = currentUser.avatar || 'https://ui-avatars.com/api/?name=' + currentUser.name;
    
    const roleBadge = document.getElementById('userRoleBadge');
    if (roleBadge) {
        roleBadge.innerText = currentUser.role === 'user' ? 'Verified Member' : `${currentUser.role.toUpperCase()} Member`;
        roleBadge.className = `role-badge role-${currentUser.role}`;
    }
}

function logout() {
    localStorage.removeItem('currentUser');
    window.location.href = '/Account/Login';
}
