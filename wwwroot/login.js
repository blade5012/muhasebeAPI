// apiBase, config.js dosyasından geliyor.

document.addEventListener("DOMContentLoaded", () => {
    const loginForm = document.getElementById("loginForm");

    if (loginForm) {
        loginForm.addEventListener("submit", async (e) => {
            e.preventDefault();

            const email = document.getElementById("email").value;
            const password = document.getElementById("password").value;

            if (!email || !password) {
                showAlert('warning', "⚠️ E-posta ve şifre zorunludur.");
                return;
            }

            try {
                const res = await fetch(`${apiBase}/Auth/login`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ email, password })
                });

                const data = await res.json();
                if (res.ok) {
                    sessionStorage.setItem('isLoggedIn', 'true');
                    sessionStorage.setItem('jwtToken', data.token);
                    sessionStorage.setItem('showWelcomeMessage', 'true'); // Yeni eklenen satır

                    // Login sayfasında mesaj GÖSTERMEDEN doğrudan gelirgider.html'e yönlendir
                    window.location.href = "gelirgider.html";
                } else {
                    showAlert('error', data.message);
                }
            } catch (err) {
                showAlert('error', "❌ Sunucuya bağlanırken hata oluştu: " + err.message);
            }
        });
    }

    const forgotLink = document.getElementById("forgotLink");
    const forgotModal = document.getElementById("forgotModal");
    const closeModalBtn = document.getElementById("closeModal");
    const sendResetBtn = document.getElementById("sendReset");
    const forgotEmailInput = document.getElementById("forgotEmail");

    if (forgotLink) {
        forgotLink.addEventListener("click", (e) => {
            e.preventDefault();
            if (forgotModal) {
                forgotModal.style.display = "flex";
                forgotEmailInput.value = "";
            }
        });
    }

    if (closeModalBtn) {
        closeModalBtn.addEventListener("click", () => {
            if (forgotModal) {
                forgotModal.style.display = "none";
            }
        });
    }

    if (sendResetBtn) {
        sendResetBtn.addEventListener("click", async () => {
            const email = forgotEmailInput.value;

            if (!email) {
                showAlert('warning', "⚠️ E-posta giriniz.");
                return;
            }

            try {
                const res = await fetch(`${apiBase}/Auth/forgotpassword`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ email })
                });

                const data = await res.json();
                if (res.ok) {
                    showAlert('success', data.message || "✅ Şifre sıfırlama linki e-postanıza gönderildi!");
                    if (forgotModal) {
                        setTimeout(() => forgotModal.style.display = "none", 2000);
                    }
                } else {
                    showAlert('error', data.message || "❌ Şifre sıfırlama linki gönderilirken hata oluştu.");
                }
            } catch (err) {
                showAlert('error', "❌ Sunucuya bağlanırken hata oluştu: " + err.message);
            }
        });
    }
});



//document.addEventListener("DOMContentLoaded", () => {
//    const loginForm = document.getElementById("loginForm");
    
//    if (loginForm) {
//        loginForm.addEventListener("submit", async (e) => {
//            e.preventDefault();

//            const email = document.getElementById("email").value;
//            const password = document.getElementById("password").value;

//            if (!email || !password) {
//                showAlert('warning', "⚠️ E-posta ve şifre zorunludur.");
//                return;
//            }

//            try {
//                const res = await fetch(`${apiBase}/Auth/login`, {
//                    method: "POST",
//                    headers: { "Content-Type": "application/json" },
//                    body: JSON.stringify({ email, password })
//                });

//                const data = await res.json(); // Yanıtı JSON olarak al
//                if (res.ok) {
//                    sessionStorage.setItem('isLoggedIn', 'true');
//                    sessionStorage.setItem('jwtToken', data.token); // JWT token'ı kaydet
//                    showAlert('success', data.message); // API'den gelen mesajı kullan
//                    setTimeout(() => window.location.href = "gelirgider.html", 500);
//                } else {
//                    showAlert('error', data.message); // API'den gelen hata mesajını kullan
//                }
//            } catch (err) {
//                showAlert('error', "❌ Sunucuya bağlanırken hata oluştu: " + err.message);
//            }
//        });
//    }

//    const forgotLink = document.getElementById("forgotLink");
//    const forgotModal = document.getElementById("forgotModal");
//    const closeModalBtn = document.getElementById("closeModal");
//    const sendResetBtn = document.getElementById("sendReset");
//    const forgotEmailInput = document.getElementById("forgotEmail");

//    if (forgotLink) {
//        forgotLink.addEventListener("click", (e) => {
//            e.preventDefault();
//            if (forgotModal) {
//                forgotModal.style.display = "flex"; // modal aç
//                forgotEmailInput.value = ""; // E-posta alanını temizle
//            }
//        });
//    }

//    if (closeModalBtn) {
//        closeModalBtn.addEventListener("click", () => {
//            if (forgotModal) {
//                forgotModal.style.display = "none"; // modal kapa
//            }
//        });
//    }

//    if (sendResetBtn) {
//        sendResetBtn.addEventListener("click", async () => {
//            const email = forgotEmailInput.value;

//            if (!email) {
//                showAlert('warning', "⚠️ E-posta giriniz.");
//                return;
//            }

//            try {
//                const res = await fetch(`${apiBase}/Auth/forgotpassword`, {
//                    method: "POST",
//                    headers: { "Content-Type": "application/json" },
//                    body: JSON.stringify({ email })
//                });

//                const data = await res.json(); // Yanıtı JSON olarak al
//                if (res.ok) {
//                    showAlert('success', data.message || "✅ Şifre sıfırlama linki e-postanıza gönderildi!"); // data.message kullan
//                    if (forgotModal) {
//                        setTimeout(() => forgotModal.style.display = "none", 2000);
//                    }
//                } else {
//                    showAlert('error', data.message || "❌ Şifre sıfırlama linki gönderilirken hata oluştu."); // data.message kullan
//                }
//            } catch (err) {
//                showAlert('error', "❌ Sunucuya bağlanırken hata oluştu: " + err.message);
//            }
//        });
//    }
//});
