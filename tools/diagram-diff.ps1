<#
.SYNOPSIS
  型・メンバ・依存の変化だけを1枚の図にする。人が目で確認するためのもの。

.DESCRIPTION
  docs/dependencies-diagrams/graph.txt の「指定した版」と「今の作業ツリー」を比べ、
  変化に触れている型だけを出す。git diff では
  「どのクラスを参照しなくなったか」が読み取れないため。

  記法は .claude/skills/class-diff-diagram/SKILL.md に定義がある。
    色    緑=追加  赤=削除  灰=変わっていない（辺と箱の枠）
    線種  太線=関連（フィールドで保持）  点線=依存（signature に出るだけ）
    メンバの文字色  緑=追加  赤=削除  橙=変更

  classDiagram ではなく graph LR を使う。classDiagram は辺の色も太さも
  指定できず（linkStyle が無く、themeCSS は securityLevel に止められる）、
  変化と関係の種類を別の軸に置けないため。
  代償としてメンバの区画が無いので、下線1行で代用している。

.PARAMETER Ref
  比較元。既定は HEAD。

.PARAMETER Name
  出力ファイル名。既定は 日付-ref。

.PARAMETER BaseFile
  比較元の graph.txt をファイルで渡す。比較元にまだ素データが無い場合に使う。
  -Ref を併せて渡すと見出しに使う。

.EXAMPLE
  ./tools/diagram-diff.ps1 -Name roots-registry
  ./tools/diagram-diff.ps1 -BaseFile ../old/graph.txt -Ref c2fcb5d -Name roots-registry
#>
param(
    [string]$Ref = "HEAD",
    [string]$Name = "",
    [string]$BaseFile = ""
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# 状態と表示を1つの文字列に詰めるときの区切り。`u{1} は PowerShell 7 の記法で
# 5.1 では展開されないため [char] を使う
$Sep = [char]1

$graphPath = "docs/dependencies-diagrams/graph.txt"
$outDir = "docs/dependencies-diff-diagrams"

function New-OrdinalMap {
    # PowerShell の @{} は大文字小文字を区別しない。roots と Roots が同じ鍵になり、
    # 別のメンバを「変更」と誤判定するため、必ず序数比較の辞書を使う
    return New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::Ordinal)
}

function Read-Graph([string]$text) {
    $layers = New-OrdinalMap
    $kinds = New-OrdinalMap
    $edges = New-OrdinalMap   # "A|B" -> assoc / dep
    $members = New-OrdinalMap # 型 -> (メンバ名 -> 表示の配列)
    $section = ""

    foreach ($raw in ($text -split "`n")) {
        $line = $raw.Trim()
        if ($line.Length -eq 0 -or $line.StartsWith("#")) { continue }
        if ($line.StartsWith("[")) { $section = $line; continue }

        switch ($section) {
            "[types]" {
                $p = $line -split ' '
                $layers[$p[0]] = $p[1]
                $kinds[$p[0]] = $p[2]
            }
            "[edges]" {
                $p = $line -split ' '
                $edges[($p[0] + "|" + $p[2])] = $p[3]
            }
            "[members]" {
                $p = $line -split '\|', 3
                if (-not $members.ContainsKey($p[0])) { $members[$p[0]] = New-OrdinalMap }
                if (-not $members[$p[0]].ContainsKey($p[1])) { $members[$p[0]][$p[1]] = @() }
                $members[$p[0]][$p[1]] += $p[2]
            }
        }
    }

    return @{ Layers = $layers; Kinds = $kinds; Edges = $edges; Members = $members }
}

if (-not (Test-Path $graphPath)) {
    throw "$graphPath が無い。先に dotnet test tests/Domain.Tests/Domain.Tests.csproj を実行すること。"
}

$new = Read-Graph (Get-Content $graphPath -Raw)

if ($BaseFile) {
    if (-not (Test-Path $BaseFile)) { throw "$BaseFile が無い。" }
    $oldText = Get-Content $BaseFile -Raw
    if ($Ref -eq "HEAD") { $Ref = (Split-Path $BaseFile -Leaf) }
} else {
    $oldText = (git show "${Ref}:${graphPath}" 2>$null) -join "`n"
    if (-not $oldText) { throw "$Ref に $graphPath が無い。-BaseFile で渡すこともできる。" }
}
$old = Read-Graph $oldText

# --- 型 ---
$newTypes = @($new.Layers.Keys | Where-Object { -not $old.Layers.ContainsKey($_) } | Sort-Object)
$goneTypes = @($old.Layers.Keys | Where-Object { -not $new.Layers.ContainsKey($_) } | Sort-Object)

# --- 辺 ---
$addedEdges = @($new.Edges.Keys | Where-Object { -not $old.Edges.ContainsKey($_) } | Sort-Object)
$goneEdges = @($old.Edges.Keys | Where-Object { -not $new.Edges.ContainsKey($_) } | Sort-Object)

# 両側にあるが種類が違う辺。引数で使うだけだった型をフィールドで持つようになった、
# あるいはその逆。状態の移動そのものなので、増減とは別に1本で示す
$changedEdges = @($new.Edges.Keys |
    Where-Object { $old.Edges.ContainsKey($_) -and $old.Edges[$_] -ne $new.Edges[$_] } | Sort-Object)

# --- メンバ ---
$memberRows = New-OrdinalMap      # 型 -> 表示行の配列
foreach ($type in ($new.Members.Keys + $old.Members.Keys | Sort-Object -Unique -CaseSensitive)) {

    # 丸ごと現れた型・消えた型は、全メンバを記号なしで並べる。
    # 何が増えたかではなく「その型が何を提供するか」が読みたいため
    if ($newTypes -contains $type -or $goneTypes -contains $type) {
        $src = if ($newTypes -contains $type) { $new.Members[$type] } else { $old.Members[$type] }
        $all = New-Object System.Collections.Generic.List[string]
        foreach ($k in ($src.Keys | Sort-Object -CaseSensitive)) {
            foreach ($d in $src[$k]) { $all.Add("none$Sep$d") }
        }
        if ($all.Count -gt 0) { $memberRows[$type] = $all }
        continue
    }

    $n = if ($new.Members.ContainsKey($type)) { $new.Members[$type] } else { New-OrdinalMap }
    $o = if ($old.Members.ContainsKey($type)) { $old.Members[$type] } else { New-OrdinalMap }

    $rows = New-Object System.Collections.Generic.List[string]

    foreach ($memberName in ($n.Keys + $o.Keys | Sort-Object -Unique -CaseSensitive)) {
        $nd = @(if ($n.ContainsKey($memberName)) { $n[$memberName] } else { @() })
        $od = @(if ($o.ContainsKey($memberName)) { $o[$memberName] } else { @() })

        $added = @($nd | Where-Object { $od -notcontains $_ })
        $removed = @($od | Where-Object { $nd -notcontains $_ })
        if ($added.Count -eq 0 -and $removed.Count -eq 0) { continue }

        # 対応付けを誤らないよう、両側ちょうど1つのときだけ「変更」にまとめる。
        # 状態は表示と分けて持つ。描画時に色へ変える
        if ($added.Count -eq 1 -and $removed.Count -eq 1) {
            $rows.Add("chg$Sep$($added[0])")
        } else {
            foreach ($d in $added) { $rows.Add("add$Sep$d") }
            foreach ($d in $removed) { $rows.Add("del$Sep$d") }
        }
    }

    if ($rows.Count -gt 0) { $memberRows[$type] = $rows }
}

$changedMemberTypes = @($memberRows.Keys | Where-Object { $newTypes -notcontains $_ -and $goneTypes -notcontains $_ })

$changeCount = $addedEdges.Count + $goneEdges.Count + $changedEdges.Count + $newTypes.Count + $goneTypes.Count + $changedMemberTypes.Count
if ($changeCount -eq 0) {
    # 変化が無ければファイルを作らない。空の図が溜まると読む気が失せる
    Write-Output "構造の変化なし。ファイルは作らない。"
    exit 0
}

# --- 図に出す型 ---
$focus = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($e in ($addedEdges + $goneEdges + $changedEdges)) {
    $p = $e -split '\|'
    [void]$focus.Add($p[0]); [void]$focus.Add($p[1])
}
foreach ($t in ($newTypes + $goneTypes + $changedMemberTypes)) { [void]$focus.Add($t) }

function Layer([string]$t) {
    if ($new.Layers.ContainsKey($t)) { return $new.Layers[$t] }
    if ($old.Layers.ContainsKey($t)) { return $old.Layers[$t] }
    return "Legacy"
}

# 属性と操作を分ける。括弧があれば操作
function IsOperation([string]$row) { return (Display $row) -match '\(' }

function State([string]$row) { return $row.Split($Sep)[0] }
function Display([string]$row) { return $row.Split($Sep)[1] }

# 行ごとの色。graph LR のノードラベルは htmlLabels が効くので span で着色できる。
# 記号ではなく色にしているのは、記号だと行頭の可視性記号と混ざって読みにくいため。
#
# style の引用符は必ずシングルにする。ラベルは ["..."] で囲むので、
# 中にダブルクォートがあるとそこでラベルが閉じたと解釈されて崩れる。
#
# classDef に color: を書かないこと。mermaid が !important を付けるため、
# ここの span が上書きされて全部同じ色になる。代わりに全ての行を span で包み、
# 変化のない行にも既定色を明示する。テーマに左右されなくなる
# 素データは C# のまま <> を持つ。ラベルに直接入れると HTML のタグと解釈されるので、
# 描画するここで逃がす。生データを描画の都合で汚さないための分担
function Wrap([string]$text, [string]$color) {
    $safe = $text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
    return "<span style='color:$color'>$safe</span>"
}

function Paint([string]$row) {
    $text = Display $row
    switch (State $row) {
        "add" { return (Wrap $text "#137333") }
        "del" { return (Wrap $text "#c5221f") }
        "chg" { return (Wrap $text "#b06000") }
        default { return (Wrap $text "#202124") }
    }
}

function NodeLabel([string]$type) {
    # メンバが無い箱も span で包む。包まないとテーマ既定の色になり、灰色に見える
    if (-not $memberRows.ContainsKey($type)) { return (Wrap $type "#202124") }

    $rows = @($memberRows[$type])
    $attrs = @($rows | Where-Object { -not (IsOperation $_) })
    $ops = @($rows | Where-Object { IsOperation $_ })

    # 区切り線の長さを一番長い行に合わせる。固定長だと短くなりがちで区切りに見えない。
    # アンダースコアは他のラテン文字と幅が近いので、文字数を合わせれば幅もおおよそ合う。
    # 長さは span を外した表示部分で測る
    $texts = @($rows | ForEach-Object { Display $_ }) + @($type)
    $longest = ($texts | Measure-Object -Property Length -Maximum).Maximum
    $rule = "_" * $longest

    $parts = New-Object System.Collections.Generic.List[string]
    $parts.Add((Wrap $type "#202124"))
    if ($attrs.Count -gt 0) { $parts.Add((Wrap $rule "#5f6368")); foreach ($a in $attrs) { $parts.Add((Paint $a)) } }
    if ($ops.Count -gt 0) { $parts.Add((Wrap $rule "#5f6368")); foreach ($o in $ops) { $parts.Add((Paint $o)) } }

    return ($parts -join "<br/>")
}

$stamp = Get-Date -Format "yyyy-MM-dd"
if (-not $Name) { $Name = "$stamp-$($Ref -replace '[^A-Za-z0-9_.-]', '_')" }
$out = Join-Path $outDir "$Name.md"

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("<!-- tools/diagram-diff.ps1 が生成する。手で編集しない -->")
$lines.Add("")
$lines.Add("# 依存の差分  $Ref -> 作業ツリー  ($stamp)")
$lines.Add("")
$lines.Add("型 +$($newTypes.Count) / -$($goneTypes.Count)　　辺 +$($addedEdges.Count) / -$($goneEdges.Count) / 種類変化 $($changedEdges.Count)　　メンバが動いた型 $($changedMemberTypes.Count)")
$lines.Add("")
$lines.Add("**色が変化** — 緑が追加、赤が削除、橙が関連と依存の入れ替わり、灰が変わっていない")
$lines.Add("**線種が関係** — 太線が関連（フィールドで保持）、点線が依存（signature に出るだけ）")
# 単一引用符。PowerShell はバッククォートを制御文字として食う
$lines.Add('緑の枠が現れた型、赤の枠が消えた型。塗りは白で統一。')
$lines.Add('メンバは文字色で示す。緑が追加、赤が削除、橙が変更。')
$lines.Add("")
$lines.Add('```mermaid')
$lines.Add("graph LR")

foreach ($layer in @("Domain", "Game", "Legacy")) {
    $inLayer = @($focus | Where-Object { (Layer $_) -eq $layer } | Sort-Object)
    if ($inLayer.Count -eq 0) { continue }

    $lines.Add("  subgraph $layer")
    foreach ($t in $inLayer) { $lines.Add("    $t[""$(NodeLabel $t)""]") }
    $lines.Add("  end")
}

$lines.Add("")

# 辺は 追加 -> 削除 -> 種類変化 -> 不変 の順に出す。
# linkStyle が添字指定なので順序が意味を持つ
function Arrow([string]$kind, [string]$label) {
    if ($label) {
        if ($kind -eq "assoc") { return "==>|$label|" }
        return "-.->|$label|"
    }
    if ($kind -eq "assoc") { return "==>" }
    return "-.->"
}

$i = 0
$addIdx = @(); $remIdx = @(); $chgIdx = @(); $keepIdx = @()

foreach ($e in $addedEdges) {
    $p = $e -split '\|'
    $lines.Add("  $($p[0]) $(Arrow $new.Edges[$e] '✚') $($p[1])")
    $addIdx += $i; $i++
}
foreach ($e in $goneEdges) {
    $p = $e -split '\|'
    $lines.Add("  $($p[0]) $(Arrow $old.Edges[$e] '✖') $($p[1])")
    $remIdx += $i; $i++
}
# 種類が変わった辺は1本。線種は変化後、色は橙
foreach ($e in $changedEdges) {
    $p = $e -split '\|'
    $lines.Add("  $($p[0]) $(Arrow $new.Edges[$e] '⟳') $($p[1])")
    $chgIdx += $i; $i++
}
foreach ($e in ($new.Edges.Keys | Sort-Object)) {
    if ($addedEdges -contains $e -or $changedEdges -contains $e) { continue }
    $p = $e -split '\|'
    if (-not ($focus.Contains($p[0]) -and $focus.Contains($p[1]))) { continue }

    $lines.Add("  $($p[0]) $(Arrow $new.Edges[$e] '') $($p[1])")
    $keepIdx += $i; $i++
}

$lines.Add("")
if ($addIdx.Count -gt 0) { $lines.Add("  linkStyle " + ($addIdx -join ",") + " stroke:#137333") }
if ($remIdx.Count -gt 0) { $lines.Add("  linkStyle " + ($remIdx -join ",") + " stroke:#c5221f") }
if ($chgIdx.Count -gt 0) { $lines.Add("  linkStyle " + ($chgIdx -join ",") + " stroke:#b06000") }
if ($keepIdx.Count -gt 0) { $lines.Add("  linkStyle " + ($keepIdx -join ",") + " stroke:#9aa0a6") }

# 塗りは白で統一し、増減は枠の色で示す。塗り分けると中の色付き文字と競合して読みにくい。
# color: は書かない。mermaid が !important を付けてラベル内の span を潰すため
$lines.Add("  classDef default fill:#ffffff,stroke:#5f6368")
if ($newTypes.Count -gt 0) {
    $lines.Add("  classDef added fill:#ffffff,stroke:#137333,stroke-width:4px")
    $lines.Add("  class " + (($newTypes | Sort-Object) -join ",") + " added")
}
if ($goneTypes.Count -gt 0) {
    $lines.Add("  classDef gone fill:#ffffff,stroke:#c5221f,stroke-width:4px,stroke-dasharray:6 4")
    $lines.Add("  class " + (($goneTypes | Sort-Object) -join ",") + " gone")
}

$lines.Add('```')
$lines.Add("")
$lines.Add("この図を見て ``docs/dependencies-diagrams/`` の現状図を更新すること。")

if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
Set-Content -Path $out -Value ($lines -join "`r`n") -Encoding utf8

Write-Output "型 +$($newTypes.Count) / -$($goneTypes.Count)　辺 +$($addedEdges.Count) / -$($goneEdges.Count) / 種類変化 $($changedEdges.Count)　メンバが動いた型 $($changedMemberTypes.Count)"
Write-Output "-> $out"
