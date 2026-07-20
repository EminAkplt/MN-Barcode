<#
.SYNOPSIS
    MN-Barcode musteri paketini olusturur (tek dosya exe + kurulum betikleri).

.DESCRIPTION
    Tek dosya, .NET gomulu (self-contained) bir exe uretir. Musterinin
    bilgisayarina .NET kurmaya gerek yoktur.

    Uretilen paket:
        MN-Barcode.exe          Uygulama (tek dosya, ~62 MB)
        appsettings.json        Baglanti cumlesi (destek duzenleyebilsin diye ayri)
        Yazici-Optimize.ps1     Etiket yazicisi ayari (yonetici olarak bir kez)
        KURULUM.txt             Kurulum adimlari

    ONEMLI: Uygulama SQL Server Express LocalDB gerektirir. Musteri
    bilgisayarinda kurulu degilse uygulama acilista anlasilir bir hata
    verir. LocalDB kurulumu KURULUM.txt icinde anlatilir.

.EXAMPLE
    .\Yayin-Olustur.ps1
    Paketi ..\yayin\MN-Barcode klasorune uretir.

.EXAMPLE
    .\Yayin-Olustur.ps1 -Surum 1.1.0
    Surum numarasini vererek uretir.

.EXAMPLE
    .\Yayin-Olustur.ps1 -Ad MN-barcode01
    Exe adini ve cikti klasorunu MN-barcode01 yapar.
    Ayni koddan farkli isimli paketler uretmek icin kullanilir;
    projede kalici bir degisiklik yapmaz.
#>
[CmdletBinding()]
param(
    [string]$Surum,
    [string]$Ad = 'MN-Barcode',
    [string]$CiktiKlasoru
)

$ErrorActionPreference = 'Stop'

# Dosya adinda kullanilamayacak karakterleri ele
$gecersiz = [System.IO.Path]::GetInvalidFileNameChars()
if ($Ad.IndexOfAny($gecersiz) -ge 0) {
    Write-Host "HATA: '$Ad' dosya adi olarak kullanilamaz." -ForegroundColor Red
    exit 1
}

$kok = Split-Path -Parent $PSScriptRoot
if (-not $CiktiKlasoru) { $CiktiKlasoru = Join-Path $kok "yayin\$Ad" }
$proje = Join-Path $kok 'MN-Barcode.WinForms\MN-Barcode.WinForms.csproj'

Write-Host "`n=== MN-Barcode Yayin Paketi ===" -ForegroundColor Cyan
Write-Host "Proje : $proje"
Write-Host "Cikti : $CiktiKlasoru`n"

if (Test-Path $CiktiKlasoru) {
    Write-Host "Eski cikti temizleniyor..." -ForegroundColor Yellow
    Remove-Item $CiktiKlasoru -Recurse -Force
}

$argumanlar = @(
    'publish', $proje
    '-c', 'Release'
    '-o', $CiktiKlasoru
    '--nologo'
    "-p:PaketAdi=$Ad"
)
if ($Surum) {
    $argumanlar += "-p:Version=$Surum"
    Write-Host "Surum: $Surum" -ForegroundColor Cyan
}
Write-Host "Ad   : $Ad" -ForegroundColor Cyan

Write-Host "Derleniyor (ReadyToRun on derleme nedeniyle birkac dakika surebilir)...`n" -ForegroundColor Yellow
& dotnet @argumanlar
if ($LASTEXITCODE -ne 0) {
    Write-Host "`nDERLEME BASARISIZ." -ForegroundColor Red
    exit 1
}

# ── Kurulum yardimcilarini pakete ekle ───────────────────────────
Copy-Item (Join-Path $PSScriptRoot 'Yazici-Optimize.ps1') $CiktiKlasoru -Force
Copy-Item (Join-Path $PSScriptRoot 'Teshis.ps1')          $CiktiKlasoru -Force

$kurulumMetni = @'
MN-BARCODE KURULUM ADIMLARI
===========================

1) VERITABANI BILESENI (bir kez, zorunlu)
   Bu program SQL Server Express LocalDB kullanir.
   Kurulu degilse program acilista uyari verir.

   Kurulum: Microsoft SQL Server Express LocalDB (SqlLocalDB.msi)
   Indirme: Microsoft resmi sitesi > SQL Server Express > LocalDB

2) PROGRAMI CALISTIRIN
   Klasordeki .exe dosyasina cift tiklayin.
   Ilk aciliista veritabani otomatik olusturulur.

   Varsayilan giris : admin / admin123
   >>> ILK GIRISTEN SONRA SIFREYI MUTLAKA DEGISTIRIN <<<

3) ETIKET YAZICISI (etiket basacaksaniz)
   a) Yazicinin kendi surucusunu kurun.
   b) Yazici-Optimize.ps1 dosyasina sag tiklayin >
      "PowerShell ile calistir" > yonetici izni verin.
      Bu adim atlanirsa etiket baskilari cok gec calisir veya
      kuyrukta asili kalir.
   c) Programda: Barkod & Etiket > Yazici Ayarlari > yaziciyi secin.

4) YEDEKLEME (onemli)
   Veritabani su klasordedir:
   C:\Users\<kullanici>\MNBarcodeLocal.mdf

   Duzenli olarak bu dosyanin kopyasini alin.

SORUN GIDERME
-------------
Program acilmiyor / hata veriyor:
   Teshis.ps1 dosyasina sag tiklayin > "PowerShell ile calistir".
   Yonetici yetkisi gerekmez. Cikan raporun TAMAMINI teknik destege
   gonderin; sorunun nerede oldugunu dogrudan gosterir.

   Hata kaydi: %LOCALAPPDATA%\MN-Barcode\hata.log

"Windows bilgisayarinizi korudu" uyarisi cikiyorsa:
   Bu bir hata degil, imzasiz programlar icin cikan guvenlik uyarisidir.
   "Daha fazla bilgi" > "Yine de calistir" secin.
   Kalici cozum: exe'ye sag tik > Ozellikler > altta
   "Engellemeyi kaldir" kutusunu isaretleyin > Tamam.

"Veritabani bileseni kurulu degil" diyorsa:
   Adim 1'deki LocalDB kurulumu yapilmamis demektir.

Etiket bos cikiyor:
   Termal etiket kagidi kullandiginizdan emin olun.
   Tirnaginizla cizince koyu iz birakmiyorsa kagit termal degildir.
'@

Set-Content -Path (Join-Path $CiktiKlasoru 'KURULUM.txt') -Value $kurulumMetni -Encoding utf8

# ── Ozet ─────────────────────────────────────────────────────────
$dosyalar = Get-ChildItem $CiktiKlasoru -Recurse -File
$toplamMB = [math]::Round(($dosyalar | Measure-Object Length -Sum).Sum / 1MB, 1)

Write-Host "`n──────────── PAKET HAZIR ────────────" -ForegroundColor Green
Write-Host "  Klasor : $CiktiKlasoru"
Write-Host "  Boyut  : $toplamMB MB / $($dosyalar.Count) dosya`n"
$dosyalar | Sort-Object Length -Descending |
    ForEach-Object { "    {0,-24} {1,8:N1} MB" -f $_.Name, ($_.Length / 1MB) } |
    Write-Host

$exe = Join-Path $CiktiKlasoru "$Ad.exe"
if (Test-Path $exe) {
    $v = (Get-Item $exe).VersionInfo
    Write-Host "`n  Surum  : $($v.FileVersion)"
    Write-Host "  Urun   : $($v.ProductName) / $($v.CompanyName)"
    Write-Host "`nBu klasoru oldugu gibi musteri bilgisayarina kopyalayin.`n" -ForegroundColor Green
} else {
    Write-Host "`nUYARI: exe uretilemedi!`n" -ForegroundColor Red
    exit 1
}
