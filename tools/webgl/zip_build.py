r"""書き出した WebGL のフォルダを、置き場所へ上げる zip にする。

中の道は / で区切る。PowerShell 5.1 で作る zip（Compress-Archive も ZipFile も）は \ で区切り、
Linux のサーバーで展開すると道が崩れる。

使い方:
  python -I zip_build.py <書き出し先のフォルダ> <zip の道>
"""
import os
import sys
import zipfile

# 手元で開くためだけに書き出しへ置く物（build.ps1）。置き場所には要らない
LOCAL_ONLY = {"遊ぶ.cmd", "serve.py"}


def main():
    source, destination = sys.argv[1], sys.argv[2]

    # 書き終えてから差し替える。途中で止まっても前の zip が残る
    partial = destination + ".part"
    with zipfile.ZipFile(partial, "w", zipfile.ZIP_DEFLATED) as archive:
        for folder, folders, files in os.walk(source):
            folders.sort()
            for name in sorted(files):
                if folder == source and name in LOCAL_ONLY:
                    continue
                path = os.path.join(folder, name)
                archive.write(path, os.path.relpath(path, source).replace(os.sep, "/"))
    os.replace(partial, destination)

    print("{:10.1f} MB  {}".format(os.path.getsize(destination) / 1024 / 1024, os.path.basename(destination)))


main()
