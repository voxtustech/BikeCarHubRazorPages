<#
.SYNOPSIS
  Copies the images that exist only on the old image host into the local image folder.

.DESCRIPTION
  Reads Docs/missing-images.txt (one "kind<TAB>/images/..." line per file) and downloads each file from
  the old host into BCH/Home/Images/_remote/<same path>. The site looks there before falling back to the
  remote host, so once this has run the old host can be switched off without breaking images.
  Files already downloaded are skipped, so the script can be run again after a failure.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\Download-MissingImages.ps1
#>
param(
    [string]$BaseUrl = "https://api.bikecarhub.com",
    [string]$ListFile = (Join-Path $PSScriptRoot "..\Docs\missing-images.txt"),
    [string]$ImageRoot = (Join-Path $PSScriptRoot "..\BCH\Home\Images")
)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$mirrorRoot = [IO.Path]::GetFullPath((Join-Path $ImageRoot "_remote"))
$ok = 0; $skipped = 0; $failed = @()

foreach ($line in Get-Content -LiteralPath $ListFile -Encoding UTF8) {
    $parts = $line.Split("`t")
    if ($parts.Count -lt 2) { continue }
    $path = $parts[1].Trim()
    if (-not $path.StartsWith("/images/", [StringComparison]::OrdinalIgnoreCase)) { continue }

    $relative = $path.Substring("/images/".Length) -replace "/", "\"
    $target = [IO.Path]::GetFullPath((Join-Path $mirrorRoot $relative))
    if (-not $target.StartsWith($mirrorRoot + "\", [StringComparison]::OrdinalIgnoreCase)) { $failed += "$path (outside image folder)"; continue }
    if (Test-Path -LiteralPath $target) { $skipped++; continue }

    $url = $BaseUrl.TrimEnd("/") + (($path.Split("/") | ForEach-Object { [Uri]::EscapeDataString($_) }) -join "/")
    try {
        New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
        Invoke-WebRequest -Uri $url -OutFile $target -UseBasicParsing -TimeoutSec 60
        $ok++
    }
    catch {
        if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Force }
        $failed += "$path ($($_.Exception.Message))"
    }
}

Write-Host "Downloaded: $ok  Already present: $skipped  Failed: $($failed.Count)"
if ($failed.Count -gt 0) {
    $report = Join-Path $PSScriptRoot "missing-images-failed.txt"
    $failed | Set-Content -LiteralPath $report -Encoding UTF8
    Write-Host "Failed paths written to $report"
}
