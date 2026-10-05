param([switch]$Test)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$fw = "$env:SystemRoot\Microsoft.NET\Framework64\v4.0.30319"
$csc = "$fw\csc.exe"
$refs = @("$fw\System.dll", "$fw\System.Core.dll", "$fw\System.Drawing.dll", "$fw\System.Windows.Forms.dll")
$ico = "$root\app.ico"
$aout = "$root\游戏快捷键锁.exe"
$tout = "$root\_selftest.exe"

& "$root\gen_icon.ps1" -Out $ico | Out-Null

$src = @(Get-ChildItem "$root\src\*.cs" | ForEach-Object { $_.FullName })

$args1 = @("/nologo", "/target:winexe", "/optimize+", "/platform:anycpu", "/codepage:65001", ("/win32icon:" + $ico), ("/out:" + $aout))
foreach ($r in $refs) { $args1 += ("/reference:" + $r) }
$args1 += $src
& $csc @args1
if ($LASTEXITCODE -ne 0) { throw "app compile failed ($LASTEXITCODE)" }
Write-Output ("APP OK -> " + $aout + "  " + (Get-Item $aout).Length + " bytes")

if ($Test) {
    $tsrc = @()
    foreach ($f in $src) { if ((Split-Path $f -Leaf) -ne "Program.cs") { $tsrc += $f } }
    $tsrc += "$root\tests\TestMain.cs"
    $args2 = @("/nologo", "/target:exe", "/platform:anycpu", "/codepage:65001", ("/out:" + $tout))
    foreach ($r in $refs) { $args2 += ("/reference:" + $r) }
    $args2 += $tsrc
    & $csc @args2
    if ($LASTEXITCODE -ne 0) { throw "test compile failed ($LASTEXITCODE)" }
    Write-Output "TESTS BUILD OK"
}