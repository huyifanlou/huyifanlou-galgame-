param([string]$Editor = $env:UNITY_EDITOR_PATH)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (!$Editor) { $Editor = 'D:\AI\Unity\Editor\2022.3.62f3c1\Editor\Unity.exe' }
if (!(Test-Path -LiteralPath $Editor)) { throw '请安装Unity中国版2022.3.62f3c1并设置UNITY_EDITOR_PATH。' }
if (Get-Process Unity -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $Editor }) {
    throw '请先关闭正在使用此编辑器的Unity工程，再运行批量构建。'
}
$project = Join-Path $taskRoot 'Game'
$logs = Join-Path $taskRoot 'output/reproduction'
New-Item -ItemType Directory -Path $logs -Force | Out-Null
foreach ($step in @(@('Bootstrap.SetupProjectBatch','setup.log'), @('Bootstrap.BuildWindowsBatch','build.log'))) {
    $logPath = Join-Path $logs $step[1]
    $arguments = @('-batchmode','-quit','-projectPath',('"' + $project + '"'),'-executeMethod',$step[0],'-logFile',('"' + $logPath + '"'))
    $batch = Start-Process -FilePath $Editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $batch.WaitForExit()
    $batch.Refresh()
    if ($batch.ExitCode -ne 0) { throw "Unity步骤失败：$($step[0])，查看 $logPath" }
}
$exe = Join-Path $project 'Build/Win/Campus.exe'
if (!(Test-Path -LiteralPath $exe)) { throw '未找到构建产物Campus.exe。' }
Write-Output "构建成功：$exe"
