# Run as Administrator

$ErrorActionPreference = "Stop"

$installDir = "C:\ZKTecoGateway"
$dll = "$installDir\zkemkeeper.dll"

Write-Host "Installing ZKTeco Gateway..."

New-Item -ItemType Directory -Force -Path $installDir | Out-Null

Copy-Item ".\*" $installDir -Recurse -Force

Copy-Item "$installDir\lib\zkemkeeper.dll" "C:\Windows\System32\zkemkeeper.dll" -Force

C:\Windows\System32\regsvr32.exe /s "C:\Windows\System32\zkemkeeper.dll"

if ($LASTEXITCODE -ne 0) {
    throw "Failed to register zkemkeeper.dll"
}

Write-Host "ZKTeco COM component registered."
Write-Host "Installation complete."
