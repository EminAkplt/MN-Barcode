using MN_Barcode.Business;

// ─────────────────────────────────────────────────────────────────────
// MN-Barcode doğrulama testleri
//
// Çalıştırma:  dotnet run --project MN-Barcode.Tests
// Çıkış kodu:  0 = hepsi geçti, 1 = en az bir test kaldı
//
// Buradaki iki alan sessizce bozulduğunda sonuç ağırdır:
//  - PasswordHasher bozulursa müşteri programa HİÇ giremez.
//  - TutarParser bozulursa kasada YANLIŞ PARA tahsil edilir.
// Bu yüzden her sürüm öncesi çalıştırılmalıdır.
// ─────────────────────────────────────────────────────────────────────

int gecti = 0, kaldi = 0;

void Kontrol(string ad, bool beklenen, bool gercek)
{
    bool ok = beklenen == gercek;
    if (ok) gecti++; else kaldi++;
    Console.WriteLine($"  [{(ok ? "GECTI" : "KALDI")}] {ad}");
}

void Tutar(string girdi, decimal beklenen)
{
    bool ok = TutarParser.TryParse(girdi, out decimal d) && d == beklenen;
    if (ok) gecti++; else kaldi++;
    Console.WriteLine($"  [{(ok ? "GECTI" : "KALDI")}] \"{girdi}\" -> {d} (beklenen {beklenen})");
}

void Gecersiz(string girdi)
{
    bool ok = !TutarParser.TryParse(girdi, out _);
    if (ok) gecti++; else kaldi++;
    Console.WriteLine($"  [{(ok ? "GECTI" : "KALDI")}] \"{girdi}\" reddedilir");
}

Console.WriteLine("=== SIFRELEME ===");
string h = PasswordHasher.Hash("admin123");
Console.WriteLine($"  Hash uzunlugu: {h.Length} karakter (sutun siniri 200)");

Kontrol("dogru sifre kabul edilir",            true,  PasswordHasher.Verify("admin123", h));
Kontrol("yanlis sifre reddedilir",             false, PasswordHasher.Verify("admin124", h));
Kontrol("bos sifre reddedilir",                false, PasswordHasher.Verify("", h));
Kontrol("null sifre reddedilir",               false, PasswordHasher.Verify(null, h));
Kontrol("hash sutun sinirina sigar",           true,  h.Length <= 200);

string h2 = PasswordHasher.Hash("admin123");
Kontrol("ayni sifre farkli hash uretir (tuz)", true,  h != h2);
Kontrol("ikinci hash de dogrulanir",           true,  PasswordHasher.Verify("admin123", h2));

Console.WriteLine("\n=== GERIYE UYUMLULUK (eski duz metin kayitlar) ===");
Kontrol("duz metin dogru sifre kabul",         true,  PasswordHasher.Verify("123", "123"));
Kontrol("duz metin yanlis sifre red",          false, PasswordHasher.Verify("456", "123"));
Kontrol("duz metin yukseltme gerektirir",      true,  PasswordHasher.NeedsUpgrade("123"));
Kontrol("hash yukseltme gerektirmez",          false, PasswordHasher.NeedsUpgrade(h));
Kontrol("eski admin123 kaydi kabul edilir",    true,  PasswordHasher.Verify("admin123", "admin123"));

Console.WriteLine("\n=== TURKCE KARAKTERLI SIFRE ===");
string ht = PasswordHasher.Hash("Sifrem-ışğüöçİŞĞÜÖÇ");
Kontrol("turkce sifre dogrulanir",             true,  PasswordHasher.Verify("Sifrem-ışğüöçİŞĞÜÖÇ", ht));
Kontrol("benzer ascii karsiligi reddedilir",   false, PasswordHasher.Verify("Sifrem-isguocISGUOC", ht));

Console.WriteLine("\n=== BOZUK KAYIT DAYANIKLILIGI (cokme olmamali) ===");
Kontrol("bozuk hash cokmez",                   false, PasswordHasher.Verify("x", "pbkdf2$abc$bozuk"));
Kontrol("yarim hash cokmez",                   false, PasswordHasher.Verify("x", "pbkdf2$100000$"));
Kontrol("null kayit cokmez",                   false, PasswordHasher.Verify("x", null));
Kontrol("bos kayit cokmez",                    false, PasswordHasher.Verify("x", ""));

Console.WriteLine("\n=== TUTAR COZUMLEME (kasada para hatasi testi) ===");
Tutar("12,50",     12.50m);     // Turkce yazim
Tutar("12.50",     12.50m);     // Ingilizce yazim - eskiden 1250 oluyordu!
Tutar("1.5",        1.5m);      // eskiden 15 oluyordu!
Tutar("1250",    1250m);
Tutar("0,99",       0.99m);
Tutar("1.234,56", 1234.56m);    // binlik nokta + ondalik virgul
Tutar("1,234.56", 1234.56m);    // binlik virgul + ondalik nokta
Tutar("1.234.567", 1234567m);   // sadece binlik ayrac
Tutar("99,90 TL",  99.90m);     // para birimi temizlenir
Tutar(" 45 ",      45m);        // bosluk temizlenir
Gecersiz("abc");
Gecersiz("");
Gecersiz(null);

Console.WriteLine($"\n────────────────────────────────");
Console.WriteLine($"SONUC: {gecti} gecti, {kaldi} kaldi");
Console.WriteLine($"────────────────────────────────");

if (kaldi > 0)
{
    Console.WriteLine("\nDIKKAT: Testler kaldi. Bu haliyle surum cikarmayin.");
    return 1;
}

Console.WriteLine("\nTum testler gecti.");
return 0;
