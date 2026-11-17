param([string]$PlayerDirectory = '', [int]$TimeoutSeconds = 90)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (!$PlayerDirectory) { $PlayerDirectory = Join-Path $taskRoot 'Game/Build/Win' }
$PlayerDirectory = (Resolve-Path -LiteralPath $PlayerDirectory).Path
$exe = Join-Path $PlayerDirectory 'Campus.exe'
$reviewDir = Join-Path $taskRoot 'output/reproduction'
New-Item -ItemType Directory -Path $reviewDir -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $reviewDir 'shots/148-title-settings-fix') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $reviewDir 'shots/147-ghost-hud-title') -Force | Out-Null
$id = 'submission-' + [Guid]::NewGuid().ToString('N')
$log = Join-Path $reviewDir ($id + '.log')
$clock = [Diagnostics.Stopwatch]::StartNew()
$process = Start-Process -FilePath $exe -ArgumentList @('-menuroutecheck','-qaid',$id,'-logFile',('"' + $log + '"'),'-screen-width','1280','-screen-height','720','-screen-fullscreen','0') -WorkingDirectory $reviewDir -WindowStyle Hidden -PassThru
if (!$process.WaitForExit($TimeoutSeconds * 1000)) { $process.Kill(); throw '独立验收超时。' }
$process.Refresh()
$clock.Stop()
$contents = Get-Content -LiteralPath $log -Raw
if ($process.ExitCode -ne 0 -or $contents -notmatch '\[MenuRouteCheck\] PASS' -or $contents -match 'NullReferenceException|InvalidOperationException|Assertion failed') { throw "验收失败，查看 $log" }
Write-Output ('PASS: actual quit exit=0; elapsed={0:N2}s; log={1}' -f $clock.Elapsed.TotalSeconds,$log)
