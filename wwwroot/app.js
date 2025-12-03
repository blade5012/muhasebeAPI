// ==================== AYARLAR ====================
//const apiBase = "http://localhost:5000/api"; // API adresin


// =================================================

// ==================== LOGIN =====================
async function login() {
    const username = document.getElementById("username").value;
    const password = document.getElementById("password").value;

    try {
        const res = await fetch(`${apiBase}/login/login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username, password })
        });

        const msg = document.getElementById("loginMsg");
        if (res.ok) {
            msg.textContent = "";
            // login sayfasını gizle
            document.getElementById("loginDiv").classList.add("hidden");
            // yönlendirme
            window.location.href = "kategoriler.html";
        } else {
            msg.textContent = await res.text();
        }
    } catch {
        alert("Login sırasında hata oluştu.");
    }
}

// ==================== KATEGORİLER =====================
async function loadKategoriler() {
    try {
        const res = await fetch(`${apiBase}/kategoriler`);
        if (!res.ok) throw new Error("Kategoriler yüklenemedi");
        const data = await res.json();

        const tbody = document.getElementById("kategoriBody");
        tbody.innerHTML = "";

        data.forEach((k, index) => {
            const row = `
                <tr>
                    <td>${index + 1}</td>
                    <td>${k.KategoriAdi}</td>
                    <td>${k.Tur}</td>
                </tr>`;
            tbody.innerHTML += row;
        });
    } catch (err) {
        console.error(err);
        alert("Kategoriler yüklenirken hata oluştu.");
    }
}

// ==================== GELİR-GİDER =====================
async function loadGelirGider() {
    try {
        const res = await fetch(`${apiBase}/GelirGider`);
        if (!res.ok) throw new Error("Kayıtlar alınamadı");

        const data = await res.json();
        const tbody = document.getElementById("gelirGiderBody");
        tbody.innerHTML = "";

        data.forEach(g => {
            const row = `
                <tr>
                    <td>${new Date(g.tarih).toLocaleDateString("tr-TR")}</td>
                    <td>${g.turu}</td>
                    <td>${g.kategoriID}</td>
                    <td>${g.aciklama}</td>
                    <td>${g.tutar.toFixed(2)} ₺</td>
                </tr>`;
            tbody.innerHTML += row;
        });

        // toplamları getir
        loadToplam();
    } catch (err) {
        console.error(err);
        alert("Gelir/gider kayıtları alınamadı.");
    }
}

// toplam gelir/gider/net tutar
async function loadToplam() {
    try {
        const res = await fetch(`${apiBase}/GelirGider/toplam`);
        if (!res.ok) throw new Error("Toplamlar alınamadı");
        const data = await res.json();

        document.getElementById("topGelir").textContent = data.toplamGelir.toFixed(2) + " ₺";
        document.getElementById("topGider").textContent = data.toplamGider.toFixed(2) + " ₺";
        document.getElementById("netTutar").textContent = data.netTutar.toFixed(2) + " ₺";
    } catch (err) {
        console.error(err);
    }
}
