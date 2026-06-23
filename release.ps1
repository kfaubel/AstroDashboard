param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('major', 'minor', 'patch')]
    [string]$BumpType = 'patch',

    [Parameter(Mandatory=$false)]
    [string]$ProjectFile = 'AstroDashboard.csproj',

    [Parameter(Mandatory=$false)]
    [string]$DefaultVersion = '0.1.0'
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$csprojPath = Join-Path $scriptRoot $ProjectFile

if (-not (Test-Path $csprojPath)) {
    Write-Host "Error: Could not find project file at $csprojPath" -ForegroundColor Red
    exit 1
}

[xml]$projectXml = Get-Content $csprojPath
$propertyGroup = $projectXml.Project.PropertyGroup | Select-Object -First 1

if (-not $propertyGroup) {
    $propertyGroup = $projectXml.CreateElement('PropertyGroup')
    [void]$projectXml.Project.AppendChild($propertyGroup)
}

if ($propertyGroup.Version) {
    $currentVersion = $propertyGroup.Version
} else {
    $currentVersion = $DefaultVersion
    $versionNode = $projectXml.CreateElement('Version')
    $versionNode.InnerText = $currentVersion
    [void]$propertyGroup.AppendChild($versionNode)
}

if ($currentVersion -notmatch '^\d+\.\d+\.\d+$') {
    Write-Host "Error: Version must be semantic format x.y.z. Found: $currentVersion" -ForegroundColor Red
    exit 1
}

Write-Host "Current version: $currentVersion" -ForegroundColor Cyan

$versionParts = $currentVersion.Split('.')
$major = [int]$versionParts[0]
$minor = [int]$versionParts[1]
$patch = [int]$versionParts[2]

switch ($BumpType) {
    'major' { $major++; $minor = 0; $patch = 0 }
    'minor' { $minor++; $patch = 0 }
    'patch' { $patch++ }
}

$newVersion = "$major.$minor.$patch"
$newVersionFull = "$major.$minor.$patch.0"
$tagName = "v$newVersion"

$assemblyVersion = $propertyGroup.AssemblyVersion
if (-not $assemblyVersion) {
    $assemblyVersion = $projectXml.CreateElement('AssemblyVersion')
    [void]$propertyGroup.AppendChild($assemblyVersion)
}

$fileVersion = $propertyGroup.FileVersion
if (-not $fileVersion) {
    $fileVersion = $projectXml.CreateElement('FileVersion')
    [void]$propertyGroup.AppendChild($fileVersion)
}

Write-Host "New version: $newVersion" -ForegroundColor Green
Write-Host "Tag: $tagName" -ForegroundColor Green

$confirm = Read-Host "`nContinue? (y/n)"
if ($confirm -ne 'y' -and $confirm -ne 'Y') {
    Write-Host "Cancelled" -ForegroundColor Yellow
    exit 0
}

Write-Host "`nUpdating $csprojPath..." -ForegroundColor Cyan
$propertyGroup.Version = $newVersion
$propertyGroup.AssemblyVersion = $newVersionFull
$propertyGroup.FileVersion = $newVersionFull
$projectXml.Save($csprojPath)
Write-Host "Version updated" -ForegroundColor Green

Write-Host "`nRunning Release build validation..." -ForegroundColor Cyan
dotnet restore
dotnet build "$csprojPath" -c Release
Write-Host "Release build validation passed" -ForegroundColor Green

Write-Host "`nGit operations..." -ForegroundColor Cyan
git add $csprojPath
git commit -m "Bump version to $newVersion"
git tag -a $tagName -m "Release $newVersion"

$currentBranch = (git rev-parse --abbrev-ref HEAD).Trim()
git push origin $currentBranch
git push origin $tagName

Write-Host "`nRelease $newVersion complete!" -ForegroundColor Green
Write-Host "Pushed branch $currentBranch and tag $tagName" -ForegroundColor Cyan
