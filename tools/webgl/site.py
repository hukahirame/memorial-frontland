"""書き出した WebGL を、Cloudflare に上げる形にして別のフォルダへ写す。上げるのは wrangler.jsonc。

Cloudflare は 1 つのファイルを 25 MiB までしか置けず、書き出しの .data はそれを超える。超えるファイルは
PART_BYTES ごとに <名前>.0, <名前>.1 … へ分け、parts.json に分けた数・元の大きさ・中身の要約を書く。
元の名前で求められたら worker.js がつないで返すので、書き出しの index.html は変えずに済む。
手元で開くためだけの物（遊ぶ.cmd と serve.py）は写さない。

使い方:
  python -I site.py [書き出し先のフォルダ] [写す先のフォルダ]
  既定は Builds/WebGL と Builds/WebGL.site
"""
import hashlib
import json
import os
import shutil
import sys

# Cloudflare の上限（25 MiB）より少し小さくする
PART_BYTES = 24 * 1024 * 1024

# 手元で開くためだけに書き出しへ置く物（build.ps1）
LOCAL_ONLY = {"遊ぶ.cmd", "serve.py"}


def split(path, target):
    """path を target.0, target.1 … に分け、分けた数と中身の要約を返す"""
    digest = hashlib.sha256()
    count = 0
    with open(path, "rb") as whole:
        while True:
            chunk = whole.read(PART_BYTES)
            if not chunk:
                break
            digest.update(chunk)
            with open("{}.{}".format(target, count), "wb") as part:
                part.write(chunk)
            count += 1
    return count, digest.hexdigest()[:32]


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")

    builds = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Builds")
    source = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else os.path.join(builds, "WebGL"))
    destination = os.path.abspath(sys.argv[2] if len(sys.argv) > 2 else os.path.join(builds, "WebGL.site"))
    if not os.path.isfile(os.path.join(source, "index.html")):
        print("書き出しが無い: " + source)
        return 1

    # 写す先は丸ごと作り直すので、書き出しと重なる所は断る
    inside = os.path.commonpath([source, destination])
    if inside in (source, destination):
        print("写す先が書き出しと重なっている: " + destination)
        return 1

    # 前の書き出しのファイルを残さない。名前が中身から作られるので、残すと使わないファイルが溜まって一緒に上がる
    if os.path.exists(destination):
        shutil.rmtree(destination)

    parts = {}
    total = 0
    for folder, folders, files in os.walk(source):
        folders.sort()
        for name in sorted(files):
            if folder == source and name in LOCAL_ONLY:
                continue
            path = os.path.join(folder, name)
            relative = os.path.relpath(path, source)
            target = os.path.join(destination, relative)
            os.makedirs(os.path.dirname(target), exist_ok=True)

            size = os.path.getsize(path)
            total += size
            if size <= PART_BYTES:
                shutil.copyfile(path, target)
                continue

            count, digest = split(path, target)
            parts["/" + relative.replace(os.sep, "/")] = {"parts": count, "size": size, "hash": digest}
            print("{:10.1f} MB  {} を {} つに分けた".format(size / 1024 / 1024, relative, count))

    with open(os.path.join(destination, "parts.json"), "w", encoding="utf-8") as index:
        json.dump(parts, index, ensure_ascii=False, indent=1)

    print("{:10.1f} MB  {}".format(total / 1024 / 1024, destination))
    return 0


if __name__ == "__main__":
    sys.exit(main())
