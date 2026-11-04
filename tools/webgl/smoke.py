"""書き出した WebGL が、ブラウザでタイトルまで開けるかを確かめる。build.ps1 が、差し替える前の書き出しに掛ける。

手元のサーバー（serve.py）で返し、画面を出さない Edge で開く。ページは ?smoke を付けて開いたときだけ、
読み込みの進みと結果を題名に出す（ひな形の index.html）。題名は Edge の調べ口（/json/list）から読める。
開けたとみなすのは、Unity が起動してから数秒、ページにエラーが出ないこと。

空いている番号で開くので、保存の出どころがふだん（http://localhost:8123）と分かれ、手元の保存には触れない。
Edge の設定は使い捨てのフォルダに作り、InPrivate で開く。まっさらな設定でも、Edge は Windows のアカウントで
黙ってサインインするため。

使い方:
  python -I smoke.py <書き出し先のフォルダ>
"""
import json
import os
import shutil
import socket
import subprocess
import sys
import tempfile
import threading
import time
import urllib.request

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import serve  # noqa: E402

# 読み込み（約 80 MB）と、絵を CPU で描くので遅い起動を待つ上限
TIMEOUT_SECONDS = 300

BROWSERS = (
    r"%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe",
    r"%ProgramFiles%\Microsoft\Edge\Application\msedge.exe",
    r"%ProgramFiles%\Google\Chrome\Application\chrome.exe",
    r"%LocalAppData%\Google\Chrome\Application\chrome.exe",
)


def browser():
    for path in BROWSERS:
        path = os.path.expandvars(path)
        if os.path.isfile(path):
            return path
    return None


def free_port():
    with socket.socket() as s:
        s.bind(("127.0.0.1", 0))
        return s.getsockname()[1]


def wait_devtools(port):
    """ブラウザの調べ口が答えるまで待つ。最初に起動したプロセスは抜けることがあるので、プロセスでは見ない"""
    for _ in range(60):
        try:
            with urllib.request.urlopen("http://127.0.0.1:{}/json/version".format(port), timeout=2):
                return True
        except OSError:
            time.sleep(0.5)
    return False


def stop(process, profile):
    """ブラウザを止める。子のプロセスは親を辿れないものもあるので、使い捨ての設定のフォルダ名で探して止める"""
    subprocess.run(["taskkill", "/PID", str(process.pid), "/T", "/F"],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    command = ("Get-CimInstance Win32_Process | Where-Object {{ $_.CommandLine -like '*{}*' }} | "
               "ForEach-Object {{ Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }}").format(profile)
    subprocess.run(["powershell", "-NoProfile", "-Command", command],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


def titles(port):
    with urllib.request.urlopen("http://127.0.0.1:{}/json/list".format(port), timeout=5) as response:
        return [page.get("title", "") for page in json.load(response) if page.get("type") == "page"]


def check(folder):
    """開けたら None、開けなければ理由"""
    path = browser()
    if path is None:
        return "Edge も Chrome も見つからない"

    port = free_port()
    server = serve.serve(folder, port)
    threading.Thread(target=server.serve_forever, daemon=True).start()
    profile = tempfile.mkdtemp(prefix="memorialfloor-smoke-")
    devtools = free_port()
    process = subprocess.Popen(
        [path, "--headless=new", "--inprivate", "--remote-debugging-port={}".format(devtools),
         "--user-data-dir=" + profile,
         "--no-first-run", "--no-default-browser-check", "--disable-extensions", "--mute-audio",
         # 画面の無いブラウザでも WebGL 2 を使えるよう、CPU で描く
         "--use-angle=swiftshader", "--enable-unsafe-swiftshader",
         "--window-size=1280,800", "http://localhost:{}/?smoke".format(port)],
        stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    try:
        if not wait_devtools(devtools):
            return "ブラウザが起動しない"

        last = ""
        silent_since = None
        started = time.time()
        while time.time() - started < TIMEOUT_SECONDS:
            try:
                found = titles(devtools)
                silent_since = None
            except OSError:
                found = []
                silent_since = silent_since or time.time()
                if time.time() - silent_since > 15:
                    return "ブラウザが止まった（最後の題名: {}）".format(last or "なし")
            for title in found:
                if title.startswith("SMOKE-READY"):
                    print("開けた（タイトルまで {:.0f} 秒）".format(time.time() - started))
                    return None
                if title.startswith("SMOKE-ERROR"):
                    return title[len("SMOKE-ERROR"):].strip() or "エラー"
                if title.startswith("SMOKE-"):
                    last = title
            time.sleep(2)
        return "{} 秒で開ききらない（最後の題名: {}）".format(TIMEOUT_SECONDS, last or "なし")
    finally:
        stop(process, profile)
        server.shutdown()
        for _ in range(10):
            shutil.rmtree(profile, ignore_errors=True)
            if not os.path.exists(profile):
                break
            time.sleep(1)


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    reason = check(os.path.abspath(sys.argv[1]))
    if reason is None:
        return 0
    print("開けなかった: " + reason)
    return 1


if __name__ == "__main__":
    sys.exit(main())
