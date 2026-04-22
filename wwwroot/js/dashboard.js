const API_PROXY_URL = "/api/UserProxy";
let currentUser = null;
let currentEditingUser = null;
let allUsers = []; 
let currentSortField = '';
let currentSortOrder = 'asc';

document.addEventListener('DOMContentLoaded', () => {
    currentUser = JSON.parse(localStorage.getItem('currentUser'));
    if (!currentUser) {
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
    
    // Reset display
    document.getElementById('adminSection').style.display = 'none';
    document.getElementById('addUserBtn').style.display = 'none';
    document.getElementById('userSection').style.display = 'none';

    // Phân quyền hiển thị (Presentation logic)
    if (currentUser.role === 'admin' || currentUser.role === 'superuser') {
        document.getElementById('adminSection').style.display = 'block';
        document.getElementById('addUserBtn').style.display = 'block';
        loadUsers();
    } else {
        document.getElementById('userSection').style.display = 'block';
        document.getElementById('userName').innerText = currentUser.name;
        document.getElementById('userEmail').innerText = currentUser.email;
        document.getElementById('userAvatar').src = currentUser.avatar || 'https://ui-avatars.com/api/?name=' + currentUser.name;
    }
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
    document.getElementById('userCount').innerText = `${users.length} users`;
    const tbody = document.getElementById('userTableBody');
    tbody.innerHTML = '';

    users.forEach(user => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td>
                <div class="d-flex align-items-center gap-3">
                    <img src="${user.avatar || 'https://ui-avatars.com/api/?name=' + user.name}" class="avatar-img">
                    <div>
                        <div class="fw-bold">${user.name}</div>
                        <div class="text-muted small">@${user.username}</div>
                    </div>
                </div>
            </td>
            <td><span class="role-badge role-${user.role}">${user.role}</span></td>
            <td>${user.email}</td>
            <td>
                <div class="d-flex gap-2">
                    <button onclick="editUser('${user.id}')" class="btn btn-sm btn-outline-primary border-0" title="Edit User"><i class="bi bi-pencil-fill"></i></button>
                    <button onclick="deleteUser('${user.id}')" class="btn btn-sm btn-outline-danger border-0" title="Delete User"><i class="bi bi-trash-fill"></i></button>
                </div>
            </td>
        `;
        tbody.appendChild(tr);
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
            alert(result.message); // Hiển thị lỗi từ backend (ví dụ: "Tài khoản Superuser là bất tử!")
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
    // Lọc options trong Role dropdown dựa trên quyền người đang đăng nhập
    const roleSelect = document.getElementById('editRole');
    roleSelect.innerHTML = ''; // Clear old options
    
    // Thêm option User (Ai cũng có quyền gán/giữ role này trừ khi là superuser sửa superuser)
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
        
        // Nếu là superuser, hiển thị thêm role của user đang sửa nếu nó là superuser (để không bị mất value)
        if (currentEditingUser.role === 'superuser') {
            const optSuper = new Option("Superuser (Quản trị cấp cao)", "superuser");
            roleSelect.add(optSuper);
            roleSelect.value = "superuser";
        }

        // Logic disable role select
        if (currentEditingUser.role === 'superuser') {
            roleSelect.disabled = true; // Không ai được đổi role superuser
        } else if (currentUser.role === 'admin') {
            if (currentEditingUser.id === currentUser.id) {
                // Admin tự sửa chính mình: Phải có option Admin để hiển thị đúng và khóa lại
                if (![...roleSelect.options].some(o => o.value === 'admin')) {
                    roleSelect.add(new Option("Admin (Quản trị viên)", "admin"));
                }
                roleSelect.value = "admin";
                roleSelect.disabled = true;
            } else {
                // Admin sửa user thường: khóa role là user
                roleSelect.disabled = true;
            }
        } else if (currentEditingUser.id === currentUser.id && currentUser.role !== 'superuser') {
            roleSelect.disabled = true; // Tự sửa mình không được đổi role
        } else {
            roleSelect.disabled = false;
        }

    } else {
        // Mode: Add New User
        currentEditingUser = null;
        document.getElementById('userId').value = "";
        document.getElementById('userForm').reset();
        document.getElementById('modalTitle').innerText = 'Add New User';
        document.getElementById('editRole').disabled = (currentUser.role === 'admin'); // Admin tạo mặc định là user
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

    // Validation cơ bản ở FE trước khi gửi
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
            
            if (isEdit && id === currentUser.id) {
                const updatedUser = { ...currentUser, ...data };
                delete updatedUser.password;
                localStorage.setItem('currentUser', JSON.stringify(updatedUser));
                location.reload();
            } else {
                loadUsers();
                alert(result.message || "Thao tác thành công!");
            }
        } else {
            alert(result.message);
        }
    } catch (error) {
        console.error(error);
        alert("Lỗi kết nối hoặc xử lý dữ liệu.");
    }
}



function logout() {
    localStorage.setItem('currentUser', null);
    localStorage.removeItem('currentUser');
    window.location.href = '/Account/Login';
}

