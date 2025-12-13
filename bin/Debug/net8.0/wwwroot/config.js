
const apiBase = `${window.location.origin}/api`;

// Global showAlert fonksiyonu
window.showAlert = function (type, msg) {
    const alertContainer = document.getElementById('alertBox');
    if (!alertContainer) {
        console.error('alertBox divi bulunamadı!');
        return;
    }
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert-box alert-${type}`;
    alertDiv.innerHTML = `${msg}`;
    alertContainer.appendChild(alertDiv);

    setTimeout(() => {
        alertDiv.remove();
    }, 5000);
};