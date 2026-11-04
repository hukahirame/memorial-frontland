# run_tests の直前に、開いているシーンに保存していない変更が無いかを確かめる。
# あればテストを始めさせずに止め、理由をエージェントへ返す。
#
# テストを始めると Unity Test Framework が保存するかを尋ねる窓を出し、
# 人が押すまで Editor の主スレッドが止まる。窓は CLI からは押せず、
# そのあいだ unity command はすべて時間切れになる。
#
# 呼び出しは .claude/settings.json の PreToolUse フックから。
# if 条件で run_tests のときだけ発火するため、引数の解析は不要。
# 止めるときは終了コード 2 で終え、stderr がエージェントに渡る。

$ErrorActionPreference = 'SilentlyContinue'

# フック経由で起動されると既定が CP932 になり、CLI の出力と止める理由が化けるため明示する
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding $false

function Stop-Run([string[]]$lines) {
    [Console]::Error.WriteLine($lines -join "`n")
    exit 2
}

# 窓で止まっている Editor は答えないので、フックの timeout より先に諦める
$out  = (& unity command --timeout 10 --result-only list_open_scenes 2>&1 | ForEach-Object { "$_" }) -join "`n"
$code = $LASTEXITCODE

if ($code -ne 0) {
    Stop-Run @(
        '開いているシーンを確かめられなかったので、run_tests を止めた。',
        $out,
        'Editor が答えないときは、保存の確認などの窓が出て止まっていることがある。窓は CLI からは押せないので、人に Editor の画面を見てもらう。'
    )
}

# CLI は案内の行（<claude-code-hint ... />）を JSON の前にも後にも付けることがあるので、最初の { から最後の } までを読む。
# 読めないまま通すと確かめずに始めてしまうので、読めなければ止める
$start = $out.IndexOf('{')
$end   = $out.LastIndexOf('}')
try { $listed = $out.Substring($start, $end - $start + 1) | ConvertFrom-Json } catch { $listed = $null }
if ($null -eq $listed -or $null -eq $listed.scenes) {
    Stop-Run @(
        '開いているシーンの一覧を読めなかったので、run_tests を止めた。',
        $out
    )
}

$dirty = @($listed.scenes | Where-Object { $_.isDirty })
if ($dirty.Count -eq 0) { exit 0 }

$scenes = $dirty | ForEach-Object {
    if ($_.path) { "  - $($_.path)" } else { "  - $($_.name)（一度も保存していないシーン）" }
}
Stop-Run (@('開いているシーンに保存していない変更があるので、run_tests を止めた。') + $scenes + @(
    'このままテストを始めると、Unity が保存するかを尋ねる窓（Scene(s) Have Been Modified）を出し、人が押すまで Editor が止まる。窓は CLI からは押せない。',
    '自分が加えた変更なら、unity command save_scene --path <パス> で保存してから回す。',
    '心当たりが無ければ、ほかのセッションか人が作業している途中の変更かもしれないので、持ち主を確かめる。open_scene で開き直すと、未保存の変更を尋ねずに捨てる。'
))
