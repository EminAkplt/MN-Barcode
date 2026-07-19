<#
.SYNOPSIS
    MN-Barcode etiket yazicisi optimizasyonu. Yonetici olarak calistirilmalidir.

.DESCRIPTION
    Termal etiket yazicilarinda baski gecikmesinin iki ana sebebini giderir:

    1) ENABLE_BIDI (cift yonlu iletisim)
       Surucu cihazdan "is bitti" cevabi bekler. Ucuz termal yaziciların cogu bu
       cevabi dondurmez; is kuyrukta "Printing, Retained" durumunda asili kalir ve
       sonraki her baski bu isin arkasinda sıraya girer. Kapatilir.

    2) DIRECT (dogrudan yaziciya yazdirma)
       Varsayilanda her is once diske spool edilir, sonra cihaza gonderilir.
       Etiket baskisinda bu gereksiz gecikmedir. Acilir.

    Ayrica kuyrukta asili kalmis isleri temizler.

.EXAMPLE
    .\Yazici-Optimize.ps1
    Kurulu tum yazicilari listeler, secim ister.

.EXAMPLE
    .\Yazici-Optimize.ps1 -PrinterName "Xlife D82"
    Belirtilen yaziciyi dogrudan optimize eder.
#>
[CmdletBinding()]
param(
    [string]$PrinterName
)

$ErrorActionPreference = 'Stop'

# ── Yonetici kontrolu ────────────────────────────────────────────
$kimlik = [Security.Principal.WindowsIdentity]::GetCurrent()
$rol    = New-Object Security.Principal.WindowsPrincipal($kimlik)
if (-not $rol.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "HATA: Bu betik yonetici yetkisi gerektirir." -ForegroundColor Red
    Write-Host "Sag tik -> 'PowerShell ile calistir (Yonetici)' secin." -ForegroundColor Yellow
    exit 1
}

# ── Yazici secimi ────────────────────────────────────────────────
if (-not $PrinterName) {
    $liste = @(Get-Printer | Where-Object { $_.PortName -notmatch '^(nul:|PORTPROMPT:)$' })
    if ($liste.Count -eq 0) { Write-Host "Kurulu fiziksel yazici bulunamadi." -ForegroundColor Red; exit 1 }

    Write-Host "`nKurulu yazicilar:" -ForegroundColor Cyan
    for ($i = 0; $i -lt $liste.Count; $i++) {
        "  [{0}] {1}  ({2})" -f $i, $liste[$i].Name, $liste[$i].PortName | Write-Host
    }
    $sec = Read-Host "`nOptimize edilecek yazicinin numarasi"
    if ($sec -notmatch '^\d+$' -or [int]$sec -ge $liste.Count) { Write-Host "Gecersiz secim." -ForegroundColor Red; exit 1 }
    $PrinterName = $liste[[int]$sec].Name
}

Write-Host "`nHedef yazici: $PrinterName" -ForegroundColor Cyan

$mevcut = Get-CimInstance Win32_Printer -Filter "Name='$($PrinterName -replace "'","''")'" -ErrorAction SilentlyContinue
if (-not $mevcut) { Write-Host "Yazici bulunamadi: $PrinterName" -ForegroundColor Red; exit 1 }

# ── Bit maskeleri (winspool PRINTER_ATTRIBUTE_*) ─────────────────
$DIRECT            = 0x2
$DO_COMPLETE_FIRST = 0x200
$ENABLE_BIDI       = 0x800

$eski = [int]$mevcut.Attributes
Write-Host "Mevcut Attributes : $eski"

# Istenen durum: DIRECT acik, BIDI ve DO_COMPLETE_FIRST kapali. Digerleri korunur.
$hedef = ($eski -bor $DIRECT) -band (-bnot $ENABLE_BIDI) -band (-bnot $DO_COMPLETE_FIRST)
Write-Host "Hedef Attributes  : $hedef"

if ($eski -eq $hedef) {
    Write-Host "Yazici zaten optimize durumda." -ForegroundColor Green
} else {
    # Yontem 1: printui (hizli, bazen sessizce basarisiz olur)
    $null = rundll32 printui.dll,PrintUIEntry /Xs /n "$PrinterName" attributes +direct -enablebidi -docompletefirst 2>&1
    Start-Sleep -Seconds 2
    $simdi = [int](Get-CimInstance Win32_Printer -Filter "Name='$($PrinterName -replace "'","''")'").Attributes

    # Yontem 2: WMI ile dogrudan yaz (printui tutmadiysa)
    if ($simdi -ne $hedef) {
        Write-Host "printui yetersiz kaldi, WMI ile yaziliyor..." -ForegroundColor Yellow
        try {
            $p = Get-CimInstance Win32_Printer -Filter "Name='$($PrinterName -replace "'","''")'"
            Set-CimInstance -InputObject $p -Property @{ Attributes = [uint32]$hedef }
            Start-Sleep -Seconds 1
        } catch {
            Write-Host "WMI yazma hatasi: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# ── Asili kalmis isleri temizle ──────────────────────────────────
$isler = @(Get-PrintJob -PrinterName $PrinterName -ErrorAction SilentlyContinue)
if ($isler.Count -gt 0) {
    Write-Host "`nKuyrukta $($isler.Count) asili is bulundu, temizleniyor..." -ForegroundColor Yellow
    foreach ($is in $isler) {
        Remove-PrintJob -PrinterName $PrinterName -ID $is.Id -ErrorAction SilentlyContinue
    }
}

# ── Sonuc ────────────────────────────────────────────────────────
Start-Sleep -Seconds 1
$son = [int](Get-CimInstance Win32_Printer -Filter "Name='$($PrinterName -replace "'","''")'").Attributes

Write-Host "`n──────────── SONUC ────────────" -ForegroundColor Cyan
"  Attributes                 : $son"
"  DIRECT (dogrudan yaziciya) : {0}" -f $(if ($son -band $DIRECT)            { "ACIK   [OK]" } else { "kapali [BASARISIZ]" })
"  ENABLE_BIDI (cift yonlu)   : {0}" -f $(if ($son -band $ENABLE_BIDI)       { "ACIK   [BASARISIZ]" } else { "kapali [OK]" })
"  DO_COMPLETE_FIRST          : {0}" -f $(if ($son -band $DO_COMPLETE_FIRST) { "ACIK   [BASARISIZ]" } else { "kapali [OK]" })

if (($son -band $DIRECT) -and -not ($son -band $ENABLE_BIDI)) {
    Write-Host "`nYazici optimize edildi. Baski gecikmesi giderilmis olmali." -ForegroundColor Green
} else {
    Write-Host "`nBazi ayarlar uygulanamadi. Yazici sürücüsü bu ayarlari kilitlemis olabilir." -ForegroundColor Yellow
    Write-Host "Elle: Denetim Masasi > Aygitlar ve Yazicilar > $PrinterName > Yazici ozellikleri" -ForegroundColor Yellow
    Write-Host "  - Baglanti Noktalari sekmesi > 'Cift yonlu destegi etkinlestir' isaretini KALDIR" -ForegroundColor Yellow
    Write-Host "  - Gelismis sekmesi > 'Dogrudan yaziciya yazdir' secenegini SEC" -ForegroundColor Yellow
}
Write-Host ""
