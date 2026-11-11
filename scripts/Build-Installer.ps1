param([switch]$Publish)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$version = '0.2.0-preview.2'
$tag = "v$version"
$repo = 'MeysamBahadori/LangCue'
$installer = Join-Path $root "artifacts\installer\LangCue-Setup-$version-win-x64.exe"

Push-Location $root
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Install the .NET 10 SDK first.' }
    # Inno Setup can be installed per user (winget default) or for all users.
    $isccCandidates = @(
        (Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
    )
    $iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $iscc) { throw 'Inno Setup 6 compiler (ISCC.exe) not found. Install Inno Setup 6 and retry.' }

    & dotnet restore LangCue.slnx
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
    & dotnet publish src/LangCue/LangCue.csproj `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -o artifacts/publish/win-x64
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

    & $iscc 'installer\LangCue.iss'
    if ($LASTEXITCODE -ne 0) { throw 'Inno Setup failed.' }
    if (-not (Test-Path $installer)) { throw "Installer missing: $installer" }
    Write-Host "Installer created: $installer" -ForegroundColor Green

    if ($Publish) {
        if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
            throw 'Install GitHub CLI (gh) and sign in with gh auth login.'
        }
        & gh auth status
        if ($LASTEXITCODE -ne 0) { throw 'GitHub CLI authentication failed.' }
        & gh release view $tag --repo $repo 2>$null
        if ($LASTEXITCODE -eq 0) { throw "Release $tag already exists. Will not overwrite it." }

        & gh release create $tag $installer `
            --repo $repo `
            --target main `
            --title "LangCue $version" `
            --prerelease `
            --notes-file 'docs\release-notes-v0.2.0-preview.2.md'
        if ($LASTEXITCODE -ne 0) { throw 'GitHub Release creation failed.' }
        Write-Host "Release published: https://github.com/$repo/releases/tag/$tag" -ForegroundColor Green
    }
}
finally { Pop-Location }
