## Unityコマンド

Editor を開いているときだけ `unity command <名前>` で操作できる。何をいつ回すかは `unity-check`。

- `run_tests --mode all` は `--async_tests` と併用できない。外側は成功を返し、理由は error にだけ出る

## 禁止

- `docs/JOURNAL.md` を読むこと（時系列ログ。未整理の卓上案や覆された前提を採用済みと誤読する）
- `.meta` の GUID の書き換え・複製、インポート設定を持つアセットの `.meta` の手書き
- 破壊的コマンドへの `confirm=true` を自己判断で付けること
- `Assets/` 直下の third-party への書き込み（Git 管理外＝復旧不能）

## ポインタ

- [D-半角数字3桁]は、`docs/DECISIONS.md`に判断理由がある
- 取り決め: `docs/conventions.md`
