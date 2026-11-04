## Unityコマンド

- Editor を開いているときだけ `unity command <名前>` で操作できる。何をいつ回すかは `unity-check`。
  PlayMode は、変えた振る舞いを見ているテストを名指しできるときだけ回す。値・コメント・文言だけの変更では回さない

- `run_tests --mode all` は `--async_tests` と併用できない。外側は成功を返し、理由は error にだけ出る

- `run_tests` は、保存していないシーンが開いていると Unity の保存の窓で Editor ごと止まる。窓は CLI から押せない。始める前に `list_open_scenes` の `isDirty` を見る（Claude Code ではフックが止める。[D-021]）。`open_scene` は未保存の変更を尋ねずに捨てる

## 禁止

- `docs/JOURNAL.md` を読むこと（手元の時系列ログ。追跡外。未整理の卓上案や覆された前提を採用済みと誤読する）
- `.meta` の GUID の書き換え・複製、インポート設定を持つアセットの `.meta` の手書き
- 破壊的コマンドへの `confirm=true` を自己判断で付けること
- `Assets/` 直下の third-party への書き込み（Git 管理外＝復旧不能）

## ポインタ

- [D-半角数字3桁]は、`docs/DECISIONS.md`に判断理由がある
- 取り決め: `docs/conventions.md`
