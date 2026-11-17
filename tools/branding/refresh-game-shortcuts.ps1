param([switch]$VerifyOnly)
$ErrorActionPreference='Stop'
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$exe=Join-Path $workspace 'Game/Build/Win/Campus.exe'
$icon=Join-Path $workspace 'Game/Build/Win/jingying-profile.ico'
if(!(Test-Path -LiteralPath $exe) -or !(Test-Path -LiteralPath $icon)){throw 'Build the current game and icon first.'}
$ws=New-Object -ComObject WScript.Shell
$directories=@([Environment]::GetFolderPath('Desktop'),[Environment]::GetFolderPath('CommonDesktopDirectory'),(Join-Path $env:APPDATA 'Microsoft/Internet Explorer/Quick Launch/User Pinned/TaskBar')) | Select-Object -Unique
$results=@()
foreach($directory in $directories){
 if(!(Test-Path -LiteralPath $directory)){continue}
 foreach($file in Get-ChildItem -LiteralPath $directory -Filter '*.lnk'){
  $shortcut=$ws.CreateShortcut($file.FullName)
  if($shortcut.TargetPath -ine $exe -and !($file.Name -eq '他山之石.lnk' -and [IO.Path]::GetFileName($shortcut.TargetPath) -eq 'Campus.exe')){continue}
  $old=$shortcut.IconLocation
  if(!$VerifyOnly){$shortcut.TargetPath=$exe;$shortcut.WorkingDirectory=Split-Path $exe;$shortcut.IconLocation=$icon+',0';$shortcut.Save();$shortcut=$ws.CreateShortcut($file.FullName)}
  if($shortcut.IconLocation -notmatch 'jingying-profile\.ico,0$'){throw ('Old shortcut icon still referenced: '+$file.FullName)}
  $results += [PSCustomObject]@{Shortcut=$file.FullName;Target=$shortcut.TargetPath;PreviousIcon=$old;Icon=$shortcut.IconLocation;WorkingDirectory=$shortcut.WorkingDirectory}
 }
}
if($results.Count -eq 0){throw 'No matching game shortcut found.'}
if(!$VerifyOnly){
 Add-Type -TypeDefinition @"
using System;using System.Runtime.InteropServices;
public static class GameIconRefresh144 {
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)] public static extern void SHChangeNotify(uint change,uint flags,string first,string second);
}
"@
 foreach($result in $results){[GameIconRefresh144]::SHChangeNotify(0x2000,0x1005,$result.Shortcut,$null)}
 [GameIconRefresh144]::SHChangeNotify(0x08000000,0,$null,$null)
 $refresh=Join-Path $env:WINDIR 'System32/ie4uinit.exe'
 if(Test-Path -LiteralPath $refresh){Start-Process -FilePath $refresh -ArgumentList '-show' -WindowStyle Hidden -Wait}
}
$results
