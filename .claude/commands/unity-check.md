---
description: Unity の状態確認 → 再コンパイル → テスト実行
argument-hint: [テストフィルタ]
---

Unity Editor に対して以下を順に実行し、結果を報告する。

1. `unity command editor_status` で status が ready、compiling と
   domainReloadInProgress が false であることを確認する。
   Editor が応答しない場合はここで中断し、起動を依頼すること。

2. `unity command recompile` を実行し、`unity command recompile_status` を
   completed か up_to_date になるまでポーリングする。
   domain reload 中の接続エラーは想定内なので無視してよい。
   failed=true なら errors 配列を読んで報告し、テストには進まない。

3. テストを editor と playmode で別々に実行する。`--mode all` は
   `--async_tests` に対応していない（error に理由が入るが、外側の
   「成功」は true のままなので必ず error を読むこと）。

       unity command run_tests --mode editor   --async_tests true
       unity command run_tests --mode playmode --async_tests true

   どちらも `unity command test_status` を completed になるまでポーリングする。
   $ARGUMENTS が指定されていれば `--filter $ARGUMENTS` を付ける。
   `--timeout` は付けないこと。CLI 側のキャンセルで Pipeline が詰まる。

   test_status が running のまま `editor_status` の playMode が stopped なら
   詰まっている。`unity command cancel_tests` を叩いてから再実行すれば戻る。
   Editor プロセスを殺してはならない。

   失敗があればフィルタを絞って再実行し、詳細を取得すること。

4. `unity command console --level error --tail 30` でエラーを確認する。

各段階の結果を簡潔にまとめること。冗長な実況は不要。