#!/bin/sh
# docs は暗号で git に入れる（[D-25]。どのファイルかは .gitattributes の filter=git-crypt）。git-crypt の鍵を入れていない clone では、足したファイルが平文のまま入る。
# コミットと push の前に、これから入りうる暗号の対象を見て、暗号でないものがあれば止める（終了コード 2）。
# Windows でも Linux でも動くよう、git の別名から git の sh で走らせる（.claude/settings.json）。

grep -Eq 'git( +-[^ ]+( +[^ -][^ ]*)?)* +(commit|push)' || exit 0
cd "$(git rev-parse --show-toplevel)" || exit 0

encrypted() {
    [ "$(head -c 10 | od -An -c | tr -d ' \n')" = '\0GITCRYPT\0' ]
}

# 標準入力の道のうち、暗号の対象のもの。$1 を渡すと、そのコミットの .gitattributes で決める
crypted() {
    git check-attr ${1:+--source="$1"} --stdin filter | sed -n 's/: filter: git-crypt$//p'
}

plain=$({
    # 次のコミットに入る物
    git ls-files | crypted | while read -r path; do
        git cat-file blob ":$path" | encrypted || echo "$path"
    done
    # まだ push していないコミットに入っている物。暗号の対象は、そのコミットの時点のもの
    for commit in $(git rev-list HEAD --not --remotes 2>/dev/null); do
        git ls-tree -r --name-only "$commit" | crypted "$commit" | while read -r path; do
            git cat-file blob "$commit:$path" | encrypted || echo "$path"
        done
    done
    # 鍵の無い clone では、作業中のファイルを足した時点で平文が入る
    if [ -z "$(git config --get filter.git-crypt.clean)" ]; then
        git ls-files -co --exclude-standard | crypted | while read -r path; do
            [ -f "$path" ] && { encrypted < "$path" || echo "$path"; }
        done
    fi
} | sort -u)

[ -z "$plain" ] && exit 0
echo "暗号で入れるはずのファイルが暗号になっていない。git-crypt の鍵を入れた手元でしか入れられない（[D-25]）:" >&2
echo "$plain" >&2
exit 2
