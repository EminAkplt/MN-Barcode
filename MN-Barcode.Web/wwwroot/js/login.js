// wwwroot/js/login.js

document.addEventListener("DOMContentLoaded", function () {

    const togglePassword = document.querySelector("#togglePassword");
    const passwordInput = document.querySelector("#passwordInput");

    if (togglePassword) {
        togglePassword.addEventListener("click", function () {
            // Tipini değiştir: password <-> text
            const type = passwordInput.getAttribute("type") === "password" ? "text" : "password";
            passwordInput.setAttribute("type", type);

            // İkonu değiştir (Basitçe metin değişimi veya sınıf değişimi)
            this.classList.toggle("active");

            // Eğer bootstrap ikonu kullanıyorsak sınıfı değişiriz, şimdilik basit tutalım:
            if (type === "text") {
                this.style.color = "#3498db"; // Aktifken mavi olsun
            } else {
                this.style.color = "#95a5a6"; // Pasifken gri
            }
        });
    }
});