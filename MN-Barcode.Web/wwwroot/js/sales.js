/* wwwroot/js/sales.js */

document.addEventListener("DOMContentLoaded", function () {

    // Sayfa açılınca imleç direkt barkod kutusuna gitsin
    focusBarcodeInput();

    // KLAVYE DİNLEME (Global Listener)
    document.addEventListener("keydown", function (e) {

        // F Tuşlarını Yakala
        switch (e.key) {
            case "F2":
                e.preventDefault(); // Tarayıcının F2 özelliğini engelle
                document.getElementById("btnCash").click(); // Nakit butonuna bas
                break;
            case "F3":
                e.preventDefault(); // Arama kutusunu açmasını engelle
                document.getElementById("btnCard").click(); // Kart butonuna bas
                break;
            case "F4":
                e.preventDefault();
                document.getElementById("btnHold").click(); // Beklet butonuna bas
                break;
            case "F5":
                e.preventDefault(); // Sayfa yenilemeyi engelle (Kritik!)
                if (confirm("Satış iptal edilecek, emin misiniz?")) {
                    document.getElementById("btnCancel").click();
                }
                break;

            // Enter Tuşu: Eğer barkod kutusundaysa "Ekle", değilse "Nakit Bitir" olabilir
            // Şimdilik sadece barkod okutma olarak bırakıyoruz.
            case "Enter":
                // Barkod okuyucu genelde sonunda Enter'a basar.
                // Burada özel bir işlem yapmaya gerek yok, form submit olacak.
                break;

            // Numpad Tuşları (Eğer numaratör kullanıyorsak)
            // Barkod kutusu odaklı değilse, kutuya odaklayıp yazdıralım
            default:
                if (e.key >= '0' && e.key <= '9') {
                    const barcodeInput = document.getElementById("barcodeInput");
                    if (document.activeElement !== barcodeInput) {
                        barcodeInput.focus();
                    }
                }
                break;
        }
    });

    // Boşluğa tıklayınca tekrar barkoda odaklan (Kasiyer fareye dokunursa geri toplar)
    document.addEventListener("click", function (e) {
        // Eğer tıklanan yer buton veya input değilse
        if (e.target.tagName !== "BUTTON" && e.target.tagName !== "INPUT") {
            focusBarcodeInput();
        }
    });
});

function focusBarcodeInput() {
    document.getElementById("barcodeInput").focus();
}

// --- GEÇİCİ FONKSİYONLAR (Test İçin) ---
function payWithCash() {
    alert("NAKİT ödeme alındı! (F2 çalıştı)");
    // Buraya backend kodu gelecek
}

function payWithCard() {
    alert("KREDİ KARTI seçildi! (F3 çalıştı)");
}

function holdSale() {
    alert("Satış BEKLEMEYE alındı! (F4 çalıştı)");
    // Listeyi temizle ama veritabanına 'Park' olarak kaydet
}

function cancelSale() {
    alert("Satış İPTAL edildi! (F5 çalıştı)");
    // Sepeti boşalt
}