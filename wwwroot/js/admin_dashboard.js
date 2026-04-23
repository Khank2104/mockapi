const API_PROXY_URL = "/api/UserProxy";
let currentUser = null;
let currentEditingUser = null;
let allUsers = [];
let currentSortField = '';
let currentSortOrder = 'asc';

document.addEventListener('DOMContentLoaded', () => {
    currentUser = JSON.parse(localStorage.getItem('currentUser'));
    if (!currentUser || (currentUser.role !== 'admin' && currentUser.role !== 'superuser')) {
        window.location.href = '/Account/Login';
        return;
    }
    setupDashboard();
});

function getAuthHeaders() {
    return {
        'Content-Type': 'application/json',
        'X-Requester-Id': currentUser.id
    };
}

function setupDashboard() {
    document.getElementById('welcomeText').innerText = `Welcome back, ${currentUser.name}!`;
    loadUsers();
}

async function loadUsers() {
    try {
        const response = await fetch(`${API_PROXY_URL}/GetAll`, {
            headers: getAuthHeaders()
        });
        if (response.ok) {
            allUsers = await response.json();
            renderUserTable(allUsers);
        }
    } catch (error) {
        console.error("Lỗi khi tải dữ liệu.");
    }
}

function renderUserTable(users) {
    const userCountElements = document.querySelectorAll('#userCount, #userCountBadge');
    userCountElements.forEach(el => el.innerText = `${users.length} users`);

    const tbody = document.getElementById('userTableBody');
    const template = document.getElementById('userRowTemplate');
    tbody.innerHTML = '';

    users.forEach(user => {
        const clone = template.content.cloneNode(true);
        const tr = clone.querySelector('tr');

        // Fill data
        tr.querySelector('.user-avatar').src = user.avatar || 'https://ui-avatars.com/api/?name=' + user.name;
        tr.querySelector('.user-name').innerText = user.name;
        tr.querySelector('.user-username').innerText = `@${user.username}`;
        
        const roleBadge = tr.querySelector('.role-badge');
        roleBadge.innerText = user.role;
        roleBadge.className = `role-badge role-${user.role}`;

        tr.querySelector('.user-email').innerText = user.email;

        // Events
        tr.querySelector('.btn-edit').onclick = () => editUser(user.id);
        tr.querySelector('.btn-delete').onclick = () => deleteUser(user.id);

        tbody.appendChild(clone);
    });
}

async function deleteUser(id) {
    if (!confirm("Bạn có chắc chắn muốn xóa người dùng này?")) return;

    try {
        const response = await fetch(`${API_PROXY_URL}/Delete/${id}`, {
            method: 'DELETE',
            headers: getAuthHeaders()
        });

        const result = await response.json();
        if (result.success) {
            loadUsers();
        } else {
            alert(result.message);
        }
    } catch (error) {
        alert("Lỗi khi xóa người dùng.");
    }
}

function sortUsers(field) {
    if (currentSortField === field) {
        currentSortOrder = currentSortOrder === 'asc' ? 'desc' : 'asc';
    } else {
        currentSortField = field;
        currentSortOrder = 'asc';
    }
    allUsers.sort((a, b) => {
        let valA = (a[field] || "").toString().toLowerCase();
        let valB = (b[field] || "").toString().toLowerCase();
        if (valA < valB) return currentSortOrder === 'asc' ? -1 : 1;
        if (valA > valB) return currentSortOrder === 'asc' ? 1 : -1;
        return 0;
    });
    renderUserTable(allUsers);
}

document.getElementById('searchInput')?.addEventListener('input', (e) => {
    const term = e.target.value.toLowerCase();
    const filtered = allUsers.filter(u =>
        u.name.toLowerCase().includes(term) ||
        u.username.toLowerCase().includes(term) ||
        u.email.toLowerCase().includes(term)
    );
    renderUserTable(filtered);
});

async function editUser(id) {
    const roleSelect = document.getElementById('editRole');
    roleSelect.innerHTML = ''; 

    const optUser = new Option("User (Thành viên thường)", "user");
    roleSelect.add(optUser);

    if (currentUser.role === 'superuser') {
        const optAdmin = new Option("Admin (Quản trị viên)", "admin");
        roleSelect.add(optAdmin);
    }

    if (id) {
        currentEditingUser = allUsers.find(u => u.id === id);
        document.getElementById('userId').value = currentEditingUser.id;
        document.getElementById('editName').value = currentEditingUser.name;
        document.getElementById('editUsername').value = currentEditingUser.username;
        document.getElementById('editEmail').value = currentEditingUser.email;
        document.getElementById('editPassword').value = "";
        document.getElementById('editRole').value = currentEditingUser.role;
        document.getElementById('modalTitle').innerText = 'Edit User / Set Permission';

        if (currentEditingUser.role === 'superuser') {
            const optSuper = new Option("Superuser (Quản trị cấp cao)", "superuser");
            roleSelect.add(optSuper);
            roleSelect.value = "superuser";
            roleSelect.disabled = true;
        } else if (currentUser.role === 'admin') {
            if (currentEditingUser.id === currentUser.id) {
                if (![...roleSelect.options].some(o => o.value === 'admin')) {
                    roleSelect.add(new Option("Admin (Quản trị viên)", "admin"));
                }
                roleSelect.value = "admin";
                roleSelect.disabled = true;
            } else {
                roleSelect.disabled = true;
            }
        } else {
            roleSelect.disabled = false;
        }
    } else {
        currentEditingUser = null;
        document.getElementById('userId').value = "";
        document.getElementById('userForm').reset();
        document.getElementById('modalTitle').innerText = 'Add New User';
        document.getElementById('editRole').disabled = (currentUser.role === 'admin');
        document.getElementById('editRole').value = "user";
    }

    const modal = new bootstrap.Modal(document.getElementById('userModal'));
    modal.show();
}

async function saveUser() {
    const id = document.getElementById('userId').value;
    const isEdit = id && id !== "";

    const data = {
        name: document.getElementById('editName').value,
        username: document.getElementById('editUsername').value,
        role: document.getElementById('editRole').value,
        email: document.getElementById('editEmail').value,
        password: document.getElementById('editPassword').value,
        avatar: currentEditingUser ? currentEditingUser.avatar : ""
    };

    if (!data.name || !data.username || !data.email || (!isEdit && !data.password)) {
        alert("Vui lòng nhập đầy đủ các trường bắt buộc.");
        return;
    }

    try {
        const url = isEdit ? `${API_PROXY_URL}/Update/${id}` : `${API_PROXY_URL}/CreateByAdmin`;
        const method = isEdit ? 'PUT' : 'POST';

        const response = await fetch(url, {
            method: method,
            headers: getAuthHeaders(),
            body: JSON.stringify(data)
        });

        const result = await response.json();
        if (result.success) {
            bootstrap.Modal.getInstance(document.getElementById('userModal')).hide();
            loadUsers();
            alert(result.message || "Thao tác thành công!");
        } else {
            alert(result.message);
        }
    } catch (error) {
        console.error(error);
        alert("Lỗi kết nối hoặc xử lý dữ liệu.");
    }
}

function logout() {
    localStorage.removeItem('currentUser');
    window.location.href = '/Account/Login';
}
