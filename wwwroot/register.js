const registerForm = document.getElementById("registerForm");

registerForm.addEventListener("submit", async (e) => {
    e.preventDefault();

    const email = document.getElementById("email").value;
    const username = document.getElementById("username").value;
    const password = document.getElementById("password").value;
    const confirmPassword = document.getElementById("confirmPassword").value;

    // Şifre validasyonu
    const passwordPattern = /^(?=.*[a-z])(?=.*[A-Z]).{6,}$/;
    if (!passwordPattern.test(password)) {
        showAlert('error', "❌ Şifre en az 6 karakter, bir büyük harf ve bir küçük harf içermelidir.");
        return;
    }

    // Şifreler eşleşmiyorsa API'ye göndermeden kullanıcıya göster
    if (password !== confirmPassword) {
        showAlert('error', "❌ Şifreler eşleşmiyor!");
        return;
    }

    try {
        const res = await fetch(`${apiBase}/Auth/register`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ email, username, password, confirmPassword })
        });

        const data = await res.json(); // Yanıtı JSON olarak al

        if (res.ok) {
            showAlert('success', data.message || "✅ Kayıt başarılı. Onay bekleniyor!"); // data.message kullan
            registerForm.reset();
        } else {
            showAlert('error', data.message || "❌ Kayıt başarısız!"); // data.message kullan
        }

    } catch (err) {
        showAlert('error', "❌ Sunucuya bağlanırken hata oluştu: " + err.message);
    }
});
