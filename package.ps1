$rootDir = $PSScriptRoot
if (!$rootDir) { $rootDir = Get-Location }

$publishDir = Join-Path $rootDir "bin\Release\net8.0-windows10.0.17763.0\win-x64\publish"
$tempDir = Join-Path $publishDir "temp_zip_content"
if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }
New-Item -ItemType Directory -Path $tempDir | Out-Null

$filesToZip = @(
    "Announcement.exe",
    "Announcement.dll",
    "Announcement.runtimeconfig.json",
    "Announcement.deps.json",
    "Designer.ico",
    "Microsoft.Toolkit.Uwp.Notifications.dll",
    "Microsoft.Windows.SDK.NET.dll",
    "WinRT.Runtime.dll",
    "MainPopup.png"
)

foreach ($f in $filesToZip) {
    Copy-Item (Join-Path $publishDir $f) (Join-Path $tempDir $f) -Force
}

$innerZipPath = Join-Path $publishDir "Announcement.zip"
if (Test-Path $innerZipPath) { Remove-Item $innerZipPath -Force }
Compress-Archive -Path (Join-Path $tempDir "*") -DestinationPath $innerZipPath -Force
Remove-Item $tempDir -Recurse -Force

$packageDir = Join-Path $publishDir "temp_package_content"
if (Test-Path $packageDir) { Remove-Item $packageDir -Recurse -Force }
New-Item -ItemType Directory -Path $packageDir | Out-Null

Copy-Item (Join-Path $rootDir "Get-Announcement.ps1") (Join-Path $packageDir "Get-Announcement.ps1") -Force
Copy-Item $innerZipPath (Join-Path $packageDir "Announcement.zip") -Force

$outerZipPath = Join-Path $publishDir "goodbye_tata.zip"
if (Test-Path $outerZipPath) { Remove-Item $outerZipPath -Force }
Compress-Archive -Path (Join-Path $packageDir "*") -DestinationPath $outerZipPath -Force
Remove-Item $packageDir -Recurse -Force

# Create Test Package
$testPackageDir = Join-Path $publishDir "temp_test_package_content"
if (Test-Path $testPackageDir) { Remove-Item $testPackageDir -Recurse -Force }
New-Item -ItemType Directory -Path $testPackageDir | Out-Null

$testScriptPath = Join-Path $rootDir "Get-Announcement-Test.ps1"
if (Test-Path $testScriptPath) {
    Copy-Item $testScriptPath (Join-Path $testPackageDir "Get-Announcement-Test.ps1") -Force
} else {
    Copy-Item (Join-Path $rootDir "Get-Announcement.ps1") (Join-Path $testPackageDir "Get-Announcement.ps1") -Force
}
Copy-Item $innerZipPath (Join-Path $testPackageDir "Announcement.zip") -Force

$testOuterZipPath = Join-Path $publishDir "goodbye_tata_test.zip"
if (Test-Path $testOuterZipPath) { Remove-Item $testOuterZipPath -Force }
Compress-Archive -Path (Join-Path $testPackageDir "*") -DestinationPath $testOuterZipPath -Force
Remove-Item $testPackageDir -Recurse -Force

Write-Host "Packaging Complete!"
