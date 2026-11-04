<#
WebGL か Android で書き出す。プロジェクトを別の場所（既定は %LOCALAPPDATA%\MemorialFloor\WebGL か Android）へ写し、
写した先で Unity をバッチモードで動かす。開いている Editor の書き出し先は切り替えないので、ほかの作業を止めない。
元のプロジェクトの素材集には書き込まない（Assets 直下の素材集は Git の管理外で、書き換えると戻せない）。

Assets は書き出しに要る物だけを写す（assets_for_build.py）。素材集の大半は使っていないので、写す量と
WebGL 向けに読み直す量が 1/8 ほどになる。日本語フォントは写した先でだけ、字を遊ぶときに作る形に変える。
3D の物に貼る絵は、写した先でだけ WebGL の上限を 1024 にする（WebGLBuild）。置き場所の上限（zip で 100 MB）に収めるため。
写した先の Library は使い回す。初回は全部を読み込むので長く、2 回目からは変わった物だけを読み直す。

書き出し先は元のプロジェクトの Builds/WebGL。置き場所へ上げる zip は、その隣の Builds/WebGL.zip に作る（zip_build.py）。
書き出しの設定は Assets/_Project/Scripts/Editor/Build/WebGLBuild.cs。
Android は手で入れる APK を Builds/Android/MemorialFrontLand.apk に作る。設定は同じ所の AndroidBuild.cs。
写した先は書き出し先ごとに分ける。同じ所で切り替えると、そのたびに全部を読み直す。

使い方: powershell -ExecutionPolicy Bypass -File tools/webgl/build.ps1 [-Target Android]
#>
param(
    [ValidateSet('WebGL', 'Android')]
    [string]$Target = 'WebGL',
    [string]$Workspace,
    [string]$Output
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if (-not $Workspace) { $Workspace = Join-Path $env:LOCALAPPDATA "MemorialFloor\$Target" }
if (-not $Output) { $Output = if ($Target -eq 'WebGL') { Join-Path $project 'Builds\WebGL' } else { Join-Path $project 'Builds\Android\MemorialFrontLand.apk' } }

$version = (Select-String -Path (Join-Path $project 'ProjectSettings\ProjectVersion.txt') -Pattern '^m_EditorVersion: (.+)$').Matches[0].Groups[1].Value
$unity = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"
if (-not (Test-Path $unity)) { throw "Unity $version が無い: $unity" }

function Mirror([string]$name) {
    robocopy (Join-Path $project $name) (Join-Path $Workspace $name) /MIR /NFL /NDL /NJH /NJS /NP /R:1 /W:1 | Out-Null

    # robocopy は 8 未満が成功
    if ($LASTEXITCODE -ge 8) { throw "$name を写せなかった（robocopy の終了コード $LASTEXITCODE）" }
}

# 1 か所だけにある文字列を置き換える。無ければ止める。元の資産が変わって変え方が効かなくなったのに気づくため
function Replace-Once([string]$text, [string]$from, [string]$to, [string]$what) {
    $at = $text.IndexOf($from)
    if ($at -lt 0) { throw "$what に「$($from.Trim())」が無い" }
    if ($text.IndexOf($from, $at + 1) -ge 0) { throw "$what に「$($from.Trim())」が 2 つ以上ある" }

    return $text.Substring(0, $at) + $to + $text.Substring($at + $from.Length)
}

New-Item -ItemType Directory -Force -Path $Workspace | Out-Null
"== 写す: $Workspace"
python -I (Join-Path $PSScriptRoot 'assets_for_build.py') $project $Workspace
if ($LASTEXITCODE -ne 0) { throw "Assets を写せなかった" }
Mirror 'Packages'
Mirror 'ProjectSettings'

# 日本語フォントは全字を 8192 四方の絵に焼いた物（書き出しで約 70 MB）。字を遊ぶときに作る形にし、
# 書き出すときに焼いた字を捨てる（TMP_PreBuildProcessor）。字は元のフォントから要る分だけ 2048 四方の絵に作り、溢れたら絵を足す
$font = Join-Path $Workspace 'Assets\TextMesh Pro\Resources\Fonts & Materials\komorebi-gothic SDF Complete.asset'
$what = 'フォントの資産'
$text = [IO.File]::ReadAllText($font)
$text = Replace-Once $text "`n  m_SourceFontFile: {fileID: 0}" "`n  m_SourceFontFile: {fileID: 12800000, guid: beddfa6435155aa46ba18b5fb6b663dd, type: 3}" $what
$text = Replace-Once $text "`n  m_AtlasPopulationMode: 0" "`n  m_AtlasPopulationMode: 1" $what
$text = Replace-Once $text "`n  m_IsMultiAtlasTexturesEnabled: 0" "`n  m_IsMultiAtlasTexturesEnabled: 1" $what
$text = Replace-Once $text "`n  m_ClearDynamicDataOnBuild: 0" "`n  m_ClearDynamicDataOnBuild: 1" $what
$text = Replace-Once $text "`n  m_AtlasWidth: 8192" "`n  m_AtlasWidth: 2048" $what
$text = Replace-Once $text "`n  m_AtlasHeight: 8192" "`n  m_AtlasHeight: 2048" $what
[IO.File]::WriteAllText($font, $text, (New-Object Text.UTF8Encoding $false))

# 写した先の目印。WebGLBuild はこれがあるときだけ、3D の物に貼る絵の取り込み設定を書き換えて縮める
if ($Target -eq 'WebGL') {
    Set-Content -Path (Join-Path $Workspace 'WebGLWorkspace.txt') -Value '書き出し用に写したプロジェクト。tools/webgl/build.ps1 が作る' -Encoding UTF8
}

"== 書き出す: $Output"
$log = Join-Path $Workspace 'build.log'
$arguments = @(
    '-batchmode', '-quit',
    '-projectPath', "`"$Workspace`"",
    '-buildTarget', $Target,
    '-executeMethod', "MemorialFloor.Editor.${Target}Build.Build",
    $(if ($Target -eq 'WebGL') { '-webglOutput' } else { '-androidOutput' }), "`"$Output`"",
    '-logFile', "`"$log`""
)
$started = Get-Date
$exitCode = (Start-Process -FilePath $unity -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden).ExitCode

# 常駐のコンパイラ（VBCSCompiler）は、同じ版の Unity の全プロセスで 1 つを使い回す。写しの置き場を読めない所から起動したものが
# 残っていると、写しの応答ファイルを開けずにコンパイルが止まる（error CS2011）。止めれば、この書き出しが起動し直す
if ($exitCode -ne 0 -and (Select-String -Path $log -Pattern 'error CS2011' -Quiet)) {
    "== 常駐のコンパイラが写しを読めない。止めてやり直す"
    Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -like "*\$version\Editor\Data\DotNetSdkRoslyn\VBCSCompiler*" } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force }
    $exitCode = (Start-Process -FilePath $unity -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden).ExitCode
}
$minutes = [math]::Round(((Get-Date) - $started).TotalMinutes, 1)

$summary = Select-String -Path $log -Pattern "${Target}Build: " | Select-Object -Last 1
if ($summary) { $summary.Line.Trim() }
if ($exitCode -ne 0) { throw "書き出しに失敗した（終了コード $exitCode、$minutes 分）。$log を見ること" }

"== 終わり（$minutes 分）"
if ($Target -eq 'Android') {
    "{0,10:N1} MB  {1}" -f ((Get-Item $Output).Length / 1MB), $Output
    return
}

Get-ChildItem -Path (Join-Path $Output 'Build') | ForEach-Object { "{0,10:N1} MB  {1}" -f ($_.Length / 1MB), $_.Name }

# 置き場所へはこの zip を上げる（上限 100 MB）
python -I (Join-Path $PSScriptRoot 'zip_build.py') $Output "$Output.zip"
if ($LASTEXITCODE -ne 0) { throw "zip にできなかった" }
