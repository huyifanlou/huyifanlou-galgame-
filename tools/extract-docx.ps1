param([Parameter(Mandatory=$true)][string]$Src,
      [Parameter(Mandatory=$true)][string]$Out)
# 把 docx 正文抽成纯文本（一行一段），供剧本扩写比对用。UTF-8 无 BOM 输出，避免 GBK 控制台崩。
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($Src)
$entry = $zip.GetEntry('word/document.xml')
if ($null -eq $entry) { throw "document.xml not found in $Src" }
$stream = $entry.Open()
$reader = New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::UTF8)
$xmlText = $reader.ReadToEnd()
$reader.Close(); $stream.Close(); $zip.Dispose()
$xmlText = $xmlText -replace '<w:p [^>]*>', "`n" -replace '</w:p>', "`n"
$xmlText = $xmlText -replace '<w:tab[^>]*/>', "`t" -replace '<w:br[^>]*/>', "`n"
$xmlText = $xmlText -replace '<[^>]+>', ''
$xmlText = $xmlText.Replace('&lt;','<').Replace('&gt;','>').Replace('&quot;','"').Replace('&apos;',"'").Replace('&amp;','&')
$clean = New-Object System.Collections.Generic.List[string]
foreach ($l in ($xmlText -split "`r?`n")) { $t = $l.Trim(); if ($t.Length -gt 0) { $clean.Add($t) } }
[System.IO.File]::WriteAllLines($Out, $clean, (New-Object System.Text.UTF8Encoding($false)))
