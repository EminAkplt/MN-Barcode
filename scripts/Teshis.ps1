<#
.SYNOPSIS
    MN-Barcode calismayan bir bilgisayarda sorunu tespit eder.

.DESCRIPTION
    Musteri bilgisayarinda calistirin. Yonetici yetkisi GEREKMEZ.
    Ciktinin tamamini teknik destege gonderin.

.EXAMPLE
    .\Teshis.ps1
#>

$ErrorActionPreference = 'Continue'

function Baslik($m) { Write-Host "`n=== $m ===" -ForegroundColor Cyan }
function Iyi($m)    { Write-Host "  [OK]    $m" -ForegroundColor Green }
function Kotu($m)   { Write-Host "  [SORUN] $m" -ForegroundColor Red }
function Bilgi($m)  { Write-Host "  $m" }

Write-Host "`nMN-BARCODE TESHIS RAPORU" -ForegroundColor White
Write-Host ("Tarih: " + (Get-Date -Format 'dd.MM.yyyy HH:mm'))

# ── 1. Sistem ───────────────────────────────────────────────────
Baslik "SISTEM"
$os = Get-CimInstance Win32_OperatingSystem
Bilgi "Windows  : $($os.Caption) ($($os.OSArchitecture))"
Bilgi "Kullanici: $env:USERDOMAIN\$env:USERNAME"
Bilgi "Bilgisayar: $env:COMPUTERNAME"
if ($os.OSArchitecture -notlike '*64*') {
    Kotu "32 bit Windows! Uretilen exe 64 bit, bu makinede CALISMAZ."
} else {
    Iyi "64 bit Windows"
}
Bilgi "Bos RAM  : $([math]::Round($os.FreePhysicalMemory/1KB)) MB"

# ── 2. LocalDB kurulu mu ────────────────────────────────────────
Baslik "VERITABANI BILESENI (LocalDB)"
$exeYolu = $null
$cmd = Get-Command sqllocaldb.exe -ErrorAction SilentlyContinue
if ($cmd) { $exeYolu = $cmd.Source }

if (-not $exeYolu) {
    foreach ($kok in @($env:ProgramFiles, ${env:ProgramFiles(x86)})) {
        if (-not $kok) { continue }
        $temel = Join-Path $kok 'Microsoft SQL Server'
        if (-not (Test-Path $temel)) { continue }
        foreach ($s in Get-ChildItem $temel -Directory -ErrorAction SilentlyContinue) {
            $aday = Join-Path $s.FullName 'Tools\Binn\sqllocaldb.exe'
            if (Test-Path $aday) { $exeYolu = $aday; break }
        }
        if ($exeYolu) { break }
    }
}

if (-not $exeYolu) {
    Kotu "sqllocaldb.exe BULUNAMADI -> LocalDB KURULU DEGIL."
    Kotu "COZUM: SQL Server Express LocalDB (SqlLocalDB.msi) kurun."
    Write-Host "`nTeshis burada bitti - once LocalDB kurulmali.`n" -ForegroundColor Yellow
    return
}
Iyi "sqllocaldb.exe bulundu: $exeYolu"

# ── 3. Ornek durumu ─────────────────────────────────────────────
Baslik "LOCALDB ORNEGI"
$liste = & $exeYolu info 2>&1
Bilgi "Kayitli ornekler: $($liste -join ', ')"

if ($liste -notmatch 'MSSQLLocalDB') {
    Kotu "MSSQLLocalDB ornegi yok. Olusturmayi deneyin:"
    Bilgi "    sqllocaldb create MSSQLLocalDB"
} else {
    Iyi "MSSQLLocalDB ornegi kayitli"
    $detay = & $exeYolu info MSSQLLocalDB 2>&1
    $detay | ForEach-Object { Bilgi "  $_" }

    if ($detay -match 'State:\s*Stopped') {
        Bilgi "Ornek durmus, baslatiliyor..."
        $b = & $exeYolu start MSSQLLocalDB 2>&1
        Bilgi "  $b"
    }
}

# ── 4. Gercek baglanti denemesi ─────────────────────────────────
Baslik "BAGLANTI TESTI"
$cs = "Data Source=(localdb)\mssqllocaldb;Initial Catalog=master;Integrated Security=true;Connect Timeout=30;"
try {
    $c = New-Object System.Data.SqlClient.SqlConnection $cs
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $c.Open()
    $sw.Stop()
    Iyi "Baglanti BASARILI ($($sw.ElapsedMilliseconds) ms)"
    $cmd2 = $c.CreateCommand()
    $cmd2.CommandText = "SELECT @@VERSION"
    Bilgi "Surum: $(($cmd2.ExecuteScalar() -split "`n")[0].Trim())"
    $c.Close()
} catch {
    Kotu "Baglanti BASARISIZ"
    Kotu $_.Exception.Message
    if ($_.Exception.InnerException) { Kotu "Ic hata: $($_.Exception.InnerException.Message)" }
}

# ── 5. Uygulama veritabani ──────────────────────────────────────
Baslik "MN-BARCODE VERITABANI"
try {
    $c = New-Object System.Data.SqlClient.SqlConnection "Data Source=(localdb)\mssqllocaldb;Initial Catalog=master;Integrated Security=true;"
    $c.Open()
    $q = $c.CreateCommand()
    $q.CommandText = "SELECT name FROM sys.databases WHERE name = 'MNBarcodeLocal'"
    $bulundu = $q.ExecuteScalar()
    if ($bulundu) {
        Iyi "MNBarcodeLocal veritabani var"
        $q.CommandText = "SELECT physical_name FROM sys.master_files WHERE database_id = DB_ID('MNBarcodeLocal')"
        $r = $q.ExecuteReader()
        while ($r.Read()) { Bilgi "  Dosya: $($r[0])" }
        $r.Close()
    } else {
        Bilgi "MNBarcodeLocal yok - program ilk acilista olusturacak (normal)"
    }
    $c.Close()
} catch {
    Kotu "Kontrol edilemedi: $($_.Exception.Message)"
}

# ── 6. Yazma izni ───────────────────────────────────────────────
Baslik "KULLANICI KLASORU YAZMA IZNI"
try {
    $test = Join-Path $env:USERPROFILE "mnbarcode_yazma_testi.tmp"
    'test' | Out-File $test -Encoding ascii
    Remove-Item $test -Force
    Iyi "$env:USERPROFILE klasorune yazilabiliyor"
} catch {
    Kotu "$env:USERPROFILE klasorune YAZILAMIYOR - veritabani olusturulamaz!"
    Kotu $_.Exception.Message
}

# ── 7. Uygulama hata kaydi ──────────────────────────────────────
Baslik "UYGULAMA HATA KAYDI"
$log = Join-Path $env:LOCALAPPDATA 'MN-Barcode\hata.log'
if (Test-Path $log) {
    Bilgi "Kayit: $log"
    Write-Host "`n--- SON 40 SATIR ---" -ForegroundColor Yellow
    Get-Content $log -Tail 40 | ForEach-Object { Write-Host "  $_" }
} else {
    Bilgi "Hata kaydi yok ($log)"
    Bilgi "Program hic acilmadiysa bu normaldir."
}

Write-Host "`n=== TESHIS BITTI ===" -ForegroundColor Cyan
Write-Host "Bu ciktinin TAMAMINI kopyalayip teknik destege gonderin.`n"
