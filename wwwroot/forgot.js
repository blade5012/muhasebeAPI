// apiBase, config.js dosyasından geliyor.

document.getElementById("forgotForm").addEventListener("submit", async (e) => {
    e.preventDefault();

    const email = document.getElementById("email").value;

    if (!email) {
        showAlert('warning', "⚠️ E-posta adresi boş olamaz!");
        return;
    }

    try {
        const res = await fetch(`${apiBase}/Auth/forgotpassword`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ email })
        });

        const data = await res.text();
        if (res.ok) {
            showAlert('success', data || "✅ Şifre sıfırlama linki e-postanıza gönderildi!");
        } else {
            showAlert('error', data || "❌ Şifre sıfırlama linki gönderilirken bir hata oluştu.");
        }
    } catch (err) {
        showAlert('error', "❌ Sunucuya bağlanırken hata oluştu: " + err.message);
    }
});
