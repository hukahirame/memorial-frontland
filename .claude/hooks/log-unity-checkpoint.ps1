# エージェントが Unity を操作する直前の作業ツリー状態を記録する。
# 前回の記録から変化していれば、その差はエージェント以外（人間 or Unity 自身）が
# 加えたものとして読める。
#
# 呼び出しは .claude/settings.json の PreToolUse フックから。
# if 条件で unity command のときだけ発火するため、引数の解析は不要。
#
# 前回と内容が同じ場合は何も書かない（連続コマンドでログが膨れないように）。

$ErrorActionPreference = 'SilentlyContinue'

# フック経由で起動されると既定が CP932 になり git の出力が化けるため明示する
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$log   = Join-Path $PSScriptRoot '..\editor-changes.log'
$state = Join-Path $PSScriptRoot '..\.unity-checkpoint-state'
$root  = Join-Path $PSScriptRoot '..\..'

Push-Location $root

$head   = (git log --oneline -1) -join ''
$status = @(git status --short -- Assets)

$fingerprint = "$head`n" + ($status -join "`n")
$sha = [System.BitConverter]::ToString(
    [System.Security.Cryptography.SHA256]::Create().ComputeHash(
        [System.Text.Encoding]::UTF8.GetBytes($fingerprint))).Replace('-','')

$prev = ''
if (Test-Path $state) { $prev = (Get-Content $state -Raw).Trim() }

if ($sha -ne $prev) {
    $stamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $lines = @()
    $lines += "=== $stamp  Unity 操作前 ==="
    $lines += "HEAD: $head"
    if ($status.Count -eq 0) {
        $lines += "Assets: 差分なし"
    } else {
        $lines += "Assets:"
        $lines += ($status | ForEach-Object { "  $_" })
    }
    $lines += ""
    Add-Content -Path $log -Value $lines -Encoding utf8
    Set-Content -Path $state -Value $sha -Encoding ascii
}

Pop-Location