$ErrorActionPreference = 'Stop'
$build = Join-Path $PSScriptRoot 'build'
$dist = Join-Path $PSScriptRoot 'dist'
# Each build gets its own payload directory; old files cannot leak into a release.
$payload = Join-Path $build ([Guid]::NewGuid().ToString('N'))
$hostDir = Join-Path $payload 'host'
New-Item -ItemType Directory -Force $hostDir, $dist | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'CopyMe') -Destination $payload -Recurse
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (!(Test-Path -LiteralPath $compiler)) { throw 'Install the Windows .NET Framework 4 development tools.' }
& $compiler /nologo /target:exe ('/out:' + (Join-Path $hostDir 'vlc-bridge.exe')) /reference:System.Web.Extensions.dll (Join-Path $PSScriptRoot 'host\Launcher.cs')
if ($LASTEXITCODE -ne 0) { throw 'Launcher build failed.' }
$archive = Join-Path $build ('payload-' + [Guid]::NewGuid().ToString('N') + '.zip')
Compress-Archive -Path (Join-Path $payload 'CopyMe'), $hostDir -DestinationPath $archive
$version = (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'CopyMe\manifest.json') -Raw | ConvertFrom-Json).version
$installer = Join-Path $dist ('VLC-Bridge-Setup-' + $version + '.exe')
& $compiler /nologo /target:winexe ('/out:' + $installer) /reference:System.Web.Extensions.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll ('/resource:' + $archive + ',payload.zip') (Join-Path $PSScriptRoot 'host\Setup.cs')
if ($LASTEXITCODE -ne 0) { throw 'Installer build failed.' }
Write-Output ('Built ' + $installer)
