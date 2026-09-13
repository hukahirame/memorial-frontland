---
description: 変更の中身に合わせて、Unity の再コンパイルとテストを回す
argument-hint: [テストフィルタ]
---

変更の中身から回すものを決め、Unity Editor に対して順に実行し、結果を報告する。
叩くときの罠は CLAUDE.md の「Unity のテスト」にある。

## 何を回すか

`git status --short` で変更を見て、当てはまる行をすべて足し合わせる。

| 変えたもの | 回すもの | 理由 |
|---|---|---|
| docs / tools / `tests/` だけ | 何も回さない。そう報告して終わる | Unity は関係しない |
| `Assets/_Project/Scripts/Domain/`、`Assets/_Project/Tests/Domain/` | 再コンパイル | ロジックは `dotnet test` が同じソースで見ている。dotnet は最新の C# で通すので、Unity の C# で通るかだけを見る |
| Game / Legacy の `.cs` | 再コンパイル、EditMode、PlayMode | クラス名やフィールドの変更がシーンの参照を切ることがある。遷移は Unity でしか見られない |
| シーン / prefab / その他のアセット | EditMode | 参照切れは AssetIntegrityTests でしか見られない |

## 手順

1. `unity command editor_status` で status が ready、compiling と
   domainReloadInProgress が false であることを確認する。
   応答しない場合はここで中断し、起動を依頼する。

2. `unity command recompile` を実行し、`unity command recompile_status` を
   completed か up_to_date になるまでポーリングする。
   domain reload 中の接続エラーは想定内なので無視してよい。
   failed=true なら errors 配列を読んで報告し、テストには進まない。

3. 表で決まったモードのテストを1つずつ実行し、それぞれ `unity command test_status` を
   completed になるまでポーリングする。

       unity command run_tests --mode editor   --async_tests true
       unity command run_tests --mode playmode --async_tests true

   $ARGUMENTS が指定されていれば `--filter $ARGUMENTS` を付ける。
   失敗があればフィルタを絞って再実行し、詳細を取得する。

4. `unity command console --level error --tail 30` でエラーを確認する。

何を回し、何を回さなかったかと、各段階の結果を簡潔にまとめる。冗長な実況は不要。
