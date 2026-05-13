/**
 * Tenant Profile Module
 * Consolidated profile logic
 */

const ProfileMain = (() => {
    const init = () => {
        bindEvents();
        switchTab('account');
    };

    const bindEvents = () => {
        document.querySelectorAll('.profile-nav-link').forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                switchTab(link.dataset.tab);
            });
        });
    };

    const switchTab = (tabId) => {
        document.querySelectorAll('.profile-nav-link').forEach(l => l.classList.remove('active'));
        document.querySelector(`.profile-nav-link[data-tab="${tabId}"]`)?.classList.add('active');

        document.querySelectorAll('.profile-tab-content').forEach(c => c.classList.add('d-none'));
        document.getElementById(`tab-${tabId}`)?.classList.remove('d-none');

        const user = JSON.parse(localStorage.getItem('currentUser') || '{}');
        const isTenant = user.role && user.role.toLowerCase() === 'tenant';

        if (tabId === 'account') AccountInfo.load();
        if (tabId === 'identity' && isTenant) IdentityInfo.load();
    };

    return { init };
})();

const AccountInfo = (() => {
    const load = async () => {
        try {
            const response = await fetch('/api/UserProxy/GetMyProfile');
            const result = await response.json();
            if (result.success) {
                const u = result.data;
                document.getElementById('acc-name').value = u.name || '';
                document.getElementById('acc-phone').value = u.phone || '';
                document.getElementById('acc-email').innerText = u.email || '';
                document.getElementById('acc-username').innerText = u.username || '';
                document.getElementById('main-avatar-img').src = u.avatar || `https://ui-avatars.com/api/?name=${encodeURIComponent(u.name)}&background=6366f1&color=fff`;
                document.getElementById('header-name').innerText = u.name;
                document.getElementById('header-role').innerText = u.role.toUpperCase();
            }
        } catch (e) {}
    };

    const save = async () => {
        const payload = {
            name: document.getElementById('acc-name').value,
            phone: document.getElementById('acc-phone').value,
            email: document.getElementById('acc-email').innerText,
            username: document.getElementById('acc-username').innerText
        };
        try {
            const response = await fetch('/api/UserProxy/UpdateProfile', {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            const result = await response.json();
            if (result.success) {
                showPremiumToast("Thành công", "Cập nhật tài khoản thành công!", "success");
                load();
            } else {
                showPremiumToast("Lỗi", result.message, "danger");
            }
        } catch (e) {
            showPremiumToast("Lỗi", "Lỗi kết nối máy chủ", "danger");
        }
    };

    const uploadAvatar = async (file) => {
        const formData = new FormData();
        formData.append('file', file);
        try {
            const response = await fetch('/api/UserProxy/UploadAvatar', { method: 'POST', body: formData });
            const result = await response.json();
            if (result.success) {
                showPremiumToast("Thành công", "Đã cập nhật ảnh đại diện!", "success");
                load();
            }
        } catch (e) {}
    };

    return { load, save, uploadAvatar };
})();

const IdentityInfo = (() => {
    const load = async () => {
        try {
            const response = await fetch('/api/TenantPortal/GetMyProfile');
            const result = await response.json();
            if (result.success) {
                const d = result.data;
                document.getElementById('id-fullname').value = d.fullName || '';
                document.getElementById('id-citizenId').value = d.citizenId || '';
                document.getElementById('id-dob').value = d.dateOfBirth ? d.dateOfBirth.split('T')[0] : '';
                document.getElementById('id-gender').value = d.gender || '';
                document.getElementById('id-address').value = d.permanentAddress || '';
                document.getElementById('id-emergency').value = d.emergencyContact || '';
            }
        } catch (e) {}
    };

    const save = async () => {
        const payload = {
            fullName: document.getElementById('id-fullname').value,
            citizenId: document.getElementById('id-citizenId').value,
            dateOfBirth: document.getElementById('id-dob').value,
            gender: document.getElementById('id-gender').value,
            permanentAddress: document.getElementById('id-address').value,
            emergencyContact: document.getElementById('id-emergency').value
        };
        try {
            const response = await fetch('/api/TenantPortal/UpdateMyProfile', {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            const result = await response.json();
            if (result.success) {
                showPremiumToast("Thành công", "Đã cập nhật hồ sơ chi tiết!", "success");
                load();
            } else {
                showPremiumToast("Lỗi", result.message, "danger");
            }
        } catch (e) {}
    };

    return { load, save };
})();

const SecurityMgmt = (() => {
    const changePassword = async () => {
        const current = document.getElementById('sec-current-pw').value;
        const next = document.getElementById('sec-new-pw').value;
        const confirm = document.getElementById('sec-confirm-pw').value;

        if (next !== confirm) {
            showPremiumToast("Lỗi", "Mật khẩu xác nhận không khớp", "danger");
            return;
        }

        try {
            const response = await fetch('/api/UserProxy/ChangePassword', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ currentPassword: current, newPassword: next })
            });
            const result = await response.json();
            if (result.success) {
                showPremiumToast("Thành công", "Đổi mật khẩu thành công!", "success");
                document.getElementById('security-form').reset();
            } else {
                showPremiumToast("Lỗi", result.message, "danger");
            }
        } catch (e) {}
    };

    return { changePassword };
})();

document.addEventListener('DOMContentLoaded', ProfileMain.init);
