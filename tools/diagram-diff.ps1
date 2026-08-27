<#
.SYNOPSIS
  依存の変化だけを1枚の図にする。人が目で確認するためのもの。

.DESCRIPTION
  docs/dependencies-diagrams/graph.txt の「指定した版」と「今の作業ツリー」を比べ、
  増えた依存・消えた依存・現れた型・消えた型だけを mermaid で出す。
  git diff では構造の変化が読み取れないため。

  Domain と Game が主役。Legacy はそれに触れている分だけ境界として出る。

  出力は docs/dependencies-diff-diagrams/ に置く。ここを見てから
  docs/dependencies-diagrams/ の現状図を更新する。

.PARAMETER Ref
  比較元。既定は HEAD。origin/main や特定のコミットも指定できる。

.PARAMETER Name
  出力ファイル名。既定は 日付-ref。

.EXAMPLE
  ./tools/diagram-diff.ps1
  ./tools/diagram-diff.ps1 -Ref origin/main -Name roots-registry
#>
param(
    [string]$Ref = "HEAD",
    [string]$Name = ""
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$graphPath = "docs/dependencies-diagrams/graph.txt"
$outDir = "docs/dependencies-diff-diagrams"

function Read-Graph([string]$text) {
    $layers = @{}
    $edges = New-Object 'System.Collections.Generic.HashSet[string]'
    $section = ""

    foreach ($raw in ($text -split "`n")) {
        $line = $raw.Trim()
        if ($line.Length -eq 0 -or $line.StartsWith("#")) { continue }
        if ($line.StartsWith("[")) { $section = $line; continue }

        if ($section -eq "[types]") {
            $p = $line -split ' '
            $layers[$p[0]] = $p[1]
        } elseif ($section -eq "[edges]") {
            [void]$edges.Add($line)
        }
    }

    return @{ Layers = $layers; Edges = $edges }
}

if (-not (Test-Path $graphPath)) {
    throw "$graphPath が無い。先に dotnet test tests/Domain.Tests/Domain.Tests.csproj を実行すること。"
}

$new = Read-Graph (Get-Content $graphPath -Raw)
$oldText = (git show "${Ref}:${graphPath}" 2>$null) -join "`n"
if (-not $oldText) { throw "$Ref に $graphPath が無い。" }
$old = Read-Graph $oldText

$added = @($new.Edges | Where-Object { -not $old.Edges.Contains($_) } | Sort-Object)
$removed = @($old.Edges | Where-Object { -not $new.Edges.Contains($_) } | Sort-Object)
$newTypes = @($new.Layers.Keys | Where-Object { -not $old.Layers.ContainsKey($_) } | Sort-Object)
$goneTypes = @($old.Layers.Keys | Where-Object { -not $new.Layers.ContainsKey($_) } | Sort-Object)

function Layer([string]$t) {
    if ($new.Layers.ContainsKey($t)) { return $new.Layers[$t] }
    if ($old.Layers.ContainsKey($t)) { return $old.Layers[$t] }
    return "Legacy"
}

# 変化に触れている型だけを図の対象にする。全体を出すと読めない
$focus = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($e in ($added + $removed)) {
    $p = $e -split ' -> '
    [void]$focus.Add($p[0]); [void]$focus.Add($p[1])
}
foreach ($t in ($newTypes + $goneTypes)) { [void]$focus.Add($t) }

$stamp = Get-Date -Format "yyyy-MM-dd"
if (-not $Name) { $Name = "$stamp-$($Ref -replace '[^A-Za-z0-9_.-]', '_')" }
$out = Join-Path $outDir "$Name.md"

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("<!-- tools/diagram-diff.ps1 が生成する。手で編集しない -->")
$lines.Add("")
$lines.Add("# 依存の差分  $Ref -> 作業ツリー  ($stamp)")
$lines.Add("")
$lines.Add("追加 $($added.Count) 本 / 削除 $($removed.Count) 本 / 現れた型 $($newTypes.Count) / 消えた型 $($goneTypes.Count)")
$lines.Add("")

if ($added.Count -eq 0 -and $removed.Count -eq 0 -and $newTypes.Count -eq 0 -and $goneTypes.Count -eq 0) {
    $lines.Add("構造の変化なし。")
} else {
    if ($newTypes.Count -gt 0) {
        $lines.Add("**現れた型**")
        foreach ($t in $newTypes) { $lines.Add("- ``$t`` ($(Layer $t))") }
        $lines.Add("")
    }
    if ($goneTypes.Count -gt 0) {
        $lines.Add("**消えた型**")
        foreach ($t in $goneTypes) { $lines.Add("- ``$t`` ($(Layer $t))") }
        $lines.Add("")
    }

    $lines.Add("太線が追加、点線が削除、細線は変わっていない依存。")
    $lines.Add("破線の枠が Domain、太い枠が Game、細い枠が Legacy。")
    $lines.Add('`+` が付いた型は新しく現れたもの、`-` が消えたもの。')  # 単一引用符。PowerShell はバッククォートを制御文字として食う
    $lines.Add("")
    $lines.Add('```mermaid')
    $lines.Add("graph LR")

    foreach ($t in ($focus | Sort-Object)) {
        if ($newTypes -contains $t) { $lines.Add("  $t[""+ $t""]") }
        elseif ($goneTypes -contains $t) { $lines.Add("  $t[""- $t""]") }
    }

    foreach ($e in $added) {
        $p = $e -split ' -> '
        $lines.Add("  $($p[0]) ==>|追加| $($p[1])")
    }
    foreach ($e in $removed) {
        $p = $e -split ' -> '
        $lines.Add("  $($p[0]) -.->|削除| $($p[1])")
    }

    # 変化した型どうしの、変わっていない依存を細線で添える。位置関係が分かるように
    foreach ($e in ($new.Edges | Sort-Object)) {
        if ($added -contains $e) { continue }
        $p = $e -split ' -> '
        if ($focus.Contains($p[0]) -and $focus.Contains($p[1])) {
            $lines.Add("  $($p[0]) --> $($p[1])")
        }
    }

    $lines.Add("  classDef domain stroke-dasharray:5;")
    $lines.Add("  classDef game stroke-width:3px;")

    foreach ($layer in @("Domain", "Game")) {
        $members = @($focus | Where-Object { (Layer $_) -eq $layer } | Sort-Object)
        if ($members.Count -gt 0) { $lines.Add("  class " + ($members -join ",") + " $($layer.ToLower());") }
    }

    $lines.Add('```')
    $lines.Add("")
    $lines.Add("この図を見て ``docs/dependencies-diagrams/`` の現状図を更新すること。")
}

if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
Set-Content -Path $out -Value ($lines -join "`r`n") -Encoding utf8

Write-Output "追加 $($added.Count) / 削除 $($removed.Count) / 現れた型 $($newTypes.Count) / 消えた型 $($goneTypes.Count)"
Write-Output "-> $out"
