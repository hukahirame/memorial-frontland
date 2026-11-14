#!/bin/sh
# docs/private は暗号で git に入れる（[D-25]）。git-crypt の鍵を入れていない clone では、足したファイルが平文のまま入る。
# コミットと push の前に、これから入りうる docs/private の中身を見て、暗号でないものがあれば止める（終了コード 2）。
# Windows でも Linux でも動くよう、git の別名から git の sh で走らせる（.claude/settings.json）。

grep -Eq 'git( +-[^ ]+( +[^ -][^ ]*)?)* +(commit|push)' || exit 0
cd "$(git rev-parse --show-toplevel)" || exit 0

encrypted() {
    [ "$(head -c 10 | od -An -c | tr -d ' \n')" = '\0GITCRYPT\0' ]
}

plain=$({
    # 次のコミットに入る物
    git ls-files -s -- docs/private | while read -r _ sha _ path; do
        git cat-file blob "$sha" | encrypted || echo "$path"
    done
    # まだ push していないコミットに入っている物
    for commit in $(git rev-list HEAD --not --remotes -- docs/private 2>/dev/null); do
        git ls-tree -r "$commit" -- docs/private | while read -r _ _ sha path; do
            git cat-file blob "$sha" | encrypted || echo "$path"
        done
    done
    # 鍵の無い clone では、作業中のファイルを足した時点で平文が入る
    if [ -z "$(git config --get filter.git-crypt.clean)" ]; then
        git ls-files -co --exclude-standard -- docs/private | while read -r path; do
            [ -f "$path" ] && { encrypted < "$path" || echo "$path"; }
        done
    fi
} | sort -u)

[ -z "$plain" ] && exit 0
echo "docs/private に暗号になっていないファイルがある。git-crypt の鍵を入れた手元でしか入れられない（[D-25]）:" >&2
echo "$plain" >&2
exit 2
