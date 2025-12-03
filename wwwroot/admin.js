// const apiBase = "http://localhost:5000/api"; // API'nizin temel URL'si

// Custom Alert Function
function showAlert(type, msg) {
    const alertBox = document.createElement('div');
    alertBox.className = `alert-box alert-${type}`;
    alertBox.innerHTML = `${msg} <span class="close-btn" onclick="this.parentElement.remove()">×</span>`;
    document.body.appendChild(alertBox);

    setTimeout(() => {
        alertBox.remove();
    }, 5000);
}

document.addEventListener("DOMContentLoaded", () => {
    console.log("DOMContentLoaded fired in admin.js");
    const token = sessionStorage.getItem('jwtToken');
    if (!token) {
        showAlert('error', '❌ Yetkisiz erişim! Lütfen giriş yapın.');
        window.location.href = 'login.html';
        return;
    }

    // Admin rol kontrolü (isteğe bağlı, API de kontrol etmeli)
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const decodedToken = JSON.parse(atob(base64));
        console.log("Decoded Token in admin.js: ", decodedToken);
        console.log("Decoded Token Role in admin.js: ", decodedToken.role); // Debug eklendi
        if (decodedToken.role !== 'Admin') {
            showAlert('error', '❌ Bu sayfaya sadece yöneticiler erişebilir.');
            window.location.href = 'gelirgider.html';
            return;
        }
    } catch (e) {
        console.error("Token çözümlenirken hata oluştu (admin.js):", e);
        showAlert('error', '❌ Token hatası, lütfen tekrar giriş yapın.');
        sessionStorage.removeItem('jwtToken');
        window.location.href = 'login.html';
        return;
    }

    loadUsers();
    loadEmailSettings(); // E-posta ayarlarını yükle
});

async function loadEmailSettings() {
    const token = sessionStorage.getItem('jwtToken');
    try {
        const res = await fetch(`${apiBase}/Settings/email`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        if (!res.ok) {
            const errorText = await res.text();
            showAlert('error', `❌ E-posta ayarları yüklenemedi: ${errorText}`);
            return;
        }

        const settings = await res.json();
        document.getElementById('smtpServer').value = settings.smtpServer;
        document.getElementById('smtpPort').value = settings.smtpPort;
        document.getElementById('smtpUsername').value = settings.smtpUsername;
        document.getElementById('smtpPassword').value = settings.smtpPassword;
        document.getElementById('senderEmail').value = settings.senderEmail;
        document.getElementById('senderName').value = settings.senderName;

    } catch (e) {
        console.error("loadEmailSettings fonksiyonunda hata oluştu:", e);
        showAlert('error', `❌ E-posta ayarları yüklenirken bir hata oluştu: ${e.message}`);
    }
}

document.getElementById('emailSettingsForm').addEventListener('submit', async (event) => {
    event.preventDefault();
    const token = sessionStorage.getItem('jwtToken');

    const updatedSettings = {
        SmtpServer: document.getElementById('smtpServer').value,
        SmtpPort: parseInt(document.getElementById('smtpPort').value, 10),
        SmtpUsername: document.getElementById('smtpUsername').value,
        SmtpPassword: document.getElementById('smtpPassword').value,
        SenderEmail: document.getElementById('senderEmail').value,
        SenderName: document.getElementById('senderName').value
    };

    try {
        const res = await fetch(`${apiBase}/Settings/email`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(updatedSettings)
        });

        if (!res.ok) {
            const errorText = await res.text();
            showAlert('error', `❌ E-posta ayarları kaydedilemedi: ${errorText}`);
            return;
        }

        showAlert('success', '✅ E-posta ayarları başarıyla güncellendi!');
    } catch (e) {
        console.error("Email ayarları kaydedilirken hata oluştu:", e);
        showAlert('error', `❌ E-posta ayarları kaydedilirken bir hata oluştu: ${e.message}`);
    }
});

async function loadUsers() {
    console.log("loadUsers çağrıldı.");
    const token = sessionStorage.getItem('jwtToken');
    try {
        const res = await fetch(`${apiBase}/Auth/listusers`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        console.log("API Response Status (loadUsers): ", res.status);

        if (!res.ok) {
            const errorText = await res.text();
            console.error("API Error Text (loadUsers): ", errorText);
            showAlert('error', `❌ Kullanıcılar yüklenemedi: ${errorText}`);
            return;
        }

        const users = await res.json();
        console.log("API Users Data (loadUsers): ", users);
        renderUserTable(users);

    } catch (e) {
        console.error("loadUsers fonksiyonunda hata oluştu:", e);
        showAlert('error', `❌ Kullanıcı listesi yüklenirken bir hata oluştu: ${e.message}`);
    }
}

function renderUserTable(users) {
    console.log("renderUserTable çağrıldı, kullanıcı sayısı: ", users.length);
    const tbody = document.getElementById('userListBody');
    tbody.innerHTML = '';

    if (users.length === 0) {
        tbody.innerHTML = '<tr><td colspan="8">Kayıtlı kullanıcı bulunamadı.</td></tr>';
        return;
    }

    users.forEach(user => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td style="display:none;">${user.id}</td>
            <td>${user.email}</td>
            <td>${user.username || '-'}</td>

            <td>
                <select class="role-select" onchange="changeUserRole(${user.id}, this.value)" 
                    ${user.email === getCurrentUserEmail() ? 'disabled' : ''}>

                    <option value="User" ${user.role === 'User' ? 'selected' : ''}>Kullanıcı</option>
                    <option value="Admin" ${user.role === 'Admin' ? 'selected' : ''}>Yönetici</option>

                </select>
            </td>

            <td>${user.isActive ? '✅' : '❌'}</td>
            <td>${user.isEmailConfirmed ? '✅' : '❌'}</td>
            <td>${new Date(user.createdAt).toLocaleDateString('tr-TR')}</td>

            <td class="action-btns">
                <button class="btn-delete-user" onclick="deleteUser(${user.id})" 
                    ${user.email === getCurrentUserEmail() ? 'disabled' : ''}>
                    🗑️ Sil
                </button>
            </td>
        `;
        tbody.appendChild(row);
    });
}


async function changeUserRole(userId, newRole) {
    const token = sessionStorage.getItem('jwtToken');

    // Rolü Türkçeye çevir
    const roleDisplay = newRole === "Admin" ? "Yönetici" : "Kullanıcı";

    if (!confirm(`Kullanıcının rolünü ${roleDisplay} olarak değiştirmek istediğinize emin misiniz?`))
        return;

    try {
        const res = await fetch(`${apiBase}/Auth/changerole/${userId}?newRole=${newRole}`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        if (!res.ok) {
            const errorText = await res.text();
            showAlert('error', `❌ Rol değiştirilemedi: ${errorText}`);
            return;
        }

        showAlert('success', `✅ Kullanıcı rolü başarıyla '${roleDisplay}' olarak güncellendi!`);
        loadUsers();

    } catch (e) {
        showAlert('error', `❌ Rol değiştirilirken bir hata oluştu: ${e.message}`);
    }
}


async function deleteUser(userId) {
    const token = sessionStorage.getItem('jwtToken');
    if (!confirm('Bu kullanıcıyı silmek istediğinize emin misiniz? Bu işlem geri alınamaz!')) return;

    try {
        const res = await fetch(`${apiBase}/Auth/deleteuser/${userId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (!res.ok) {
            const errorText = await res.text();
            showAlert('error', `❌ Kullanıcı silinemedi: ${errorText}`);
            return;
        }

        showAlert('success', '🗑️ Kullanıcı başarıyla silindi!');
        loadUsers(); // Listeyi yeniden yükle

    } catch (e) {
        showAlert('error', `❌ Kullanıcı silinirken bir hata oluştu: ${e.message}`);
    }
}

function getCurrentUserEmail() {
    const token = sessionStorage.getItem('jwtToken');
    if (!token) return null;
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const decodedToken = JSON.parse(atob(base64));
        return decodedToken.email; // JWT payload'ındaki e-posta alanı
    } catch (e) {
        console.error("Mevcut kullanıcının e-postası çözümlenemedi:", e);
        return null;
    }
}
