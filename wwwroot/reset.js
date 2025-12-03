// apiBase, config.js dosyasından geliyor.

document.addEventListener("DOMContentLoaded", () => {
    const urlParams = new URLSearchParams(window.location.search);
    const tokenFromUrl = urlParams.get('token');
    const tokenInput = document.getElementById("token");
    
    console.log("DOMContentLoaded: tokenFromUrl", tokenFromUrl);

    if (tokenInput && tokenFromUrl) {
        tokenInput.value = tokenFromUrl;
        tokenInput.type = "hidden"; // Tokenı gizle
        console.log("DOMContentLoaded: tokenInput.value (set)", tokenInput.value);
    } else if (tokenInput) {
        console.log("DOMContentLoaded: Token URL'den alınamadı.");
        tokenInput.placeholder = "Token URL'de bulunamadı veya boş";
        // Kullanıcının manuel girmesi için type'ı text olarak bırakabiliriz ya da hatayı gösterebiliriz.
    }

    // resetForm değişkeni DOMContentLoaded içinde tanımlanmalı ve event listener burada atanmalı
    const resetForm = document.getElementById("resetForm");
    if (resetForm) {
        resetForm.addEventListener("submit", async (e) => {
            e.preventDefault();

            const token = document.getElementById("token").value;
            const password = document.getElementById("newPassword").value;
            const confirmPassword = document.getElementById("confirmPassword").value;

            console.log("Submit: token from input", token);

            if (!token) {
                showAlert('error', "❌ Token eksik!");
                console.error("Submit: Token eksik, gönderilmiyor.");
                return;
            }

            // Şifre validasyonu
            const passwordPattern = /^(?=.*[a-z])(?=.*[A-Z]).{6,}$/;
            if (!passwordPattern.test(password)) {
                showAlert('error', "❌ Şifre en az 6 karakter, bir büyük harf ve bir küçük harf içermelidir.");
                return;
            }

            if (password !== confirmPassword) {
                showAlert('error', "❌ Yeni şifreler eşleşmiyor!");
                return;
            }

            try {
                const fetchUrl = `${apiBase}/Auth/resetpassword?token=${encodeURIComponent(token)}`;
                console.log("Submit: Fetch URL", fetchUrl);

                const res = await fetch(fetchUrl, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ password, confirmPassword })
                });

                const data = await res.text();
                if (res.ok) {
                    showAlert('success', data || "✅ Şifre başarıyla sıfırlandı!");
                    resetForm.reset();
                    console.log("Şifre sıfırlama başarılı, yanıt: ", data);
                    setTimeout(() => { 
                        window.location.href = "login.html"; 
                    }, 3000);
                } else {
                    showAlert('error', data || "❌ Şifre sıfırlanırken bir hata oluştu.");
                }
            } catch (err) {
                showAlert('error', "❌ Sunucuya ulaşılamadı veya bir hata oluştu: " + err.message);
                console.error("Submit: Fetch hatası", err);
            }
        });
    }
});
