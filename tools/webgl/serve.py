"""書き出した WebGL を手元のブラウザで開く。

ブラウザは、ファイルを直に開いたページから Build の下を読み込ませない。Unity の WebGL は
サーバー越しでないと開けないので、この小さなサーバーを立てて開く。書き出し先の「遊ぶ.cmd」が、窓を出さない
pythonw でこれを裏に立てる。

裏で動くので、閉じる窓が無い。ページは開いているあいだ 20 秒ごとに /__alive へ便りを送り（ひな形の index.html）、
サーバーは便りも読み込みも IDLE_SECONDS 来なくなったら自分で止まる。ゲームのページを閉じれば数分で止まり、
ブラウザが開かなかったときも止まる。Claude アプリの確かめ用（.claude/launch.json）はアプリが止めるので、--stay で止まらない。

どの返事にも Cache-Control: no-store を付ける。書き出し直すと同じ名前のファイルの中身だけが変わり、
古いファイルがブラウザに残っていると新旧が混ざって開けなくなる。

保存はブラウザのページの出どころ（http://localhost:8123）ごとに分かれる。番号を変えると保存が見えなくなるので、
番号はいつも同じにする。すでに使われていれば、そこで動いているサーバーで開く。

使い方:
  pythonw -I serve.py [書き出し先のフォルダ] [--port 番号] [--no-browser] [--stay] [--idle 秒]
"""
import functools
import http.server
import os
import socket
import sys
import threading
import time
import webbrowser

PORT = 8123

# ページが開いているあいだに送ってくる便りの道
ALIVE = "/__alive"

# 便りも読み込みも来なくなってから止まるまで（秒）。隠れたタブの便りはブラウザが 1 分に 1 回まで間引くので、それより長くする
IDLE_SECONDS = 180

# 止めるかを見る間隔（秒）
WATCH_SECONDS = 5


class Handler(http.server.SimpleHTTPRequestHandler):
    extensions_map = {
        **http.server.SimpleHTTPRequestHandler.extensions_map,
        ".wasm": "application/wasm",
        ".unityweb": "application/octet-stream",
    }

    def do_GET(self):
        self.server.last_seen = time.monotonic()
        if self.path.split("?")[0] == ALIVE:
            self.send_response(204)
            self.end_headers()
            return
        super().do_GET()

    def do_HEAD(self):
        self.server.last_seen = time.monotonic()
        super().do_HEAD()

    def end_headers(self):
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    # 読み込みのたびに行を出さない
    def log_message(self, format, *args):
        pass


class Server(http.server.ThreadingHTTPServer):
    """
    番号がほかに使われていれば、付けずに OSError にする。http.server は SO_REUSEADDR を付けるが、Windows ではこれで
    動いているサーバーと同じ番号に重ねて付いてしまい、どちらが答えるか定まらなくなる
    """
    allow_reuse_address = False

    def server_bind(self):
        if hasattr(socket, "SO_EXCLUSIVEADDRUSE"):
            self.socket.setsockopt(socket.SOL_SOCKET, socket.SO_EXCLUSIVEADDRUSE, 1)
        super().server_bind()


def serve(folder, port):
    """folder を 127.0.0.1:port で返すサーバー。手元からだけ開ける（外に開くと Windows のファイアウォールが尋ねてくる）"""
    server = Server(("127.0.0.1", port), functools.partial(Handler, directory=folder))
    server.last_seen = time.monotonic()
    return server


def stop_when_idle(server, idle):
    """便りも読み込みも idle 秒来なければ止める"""
    while True:
        time.sleep(WATCH_SECONDS)
        if time.monotonic() - server.last_seen > idle:
            server.shutdown()
            return


def say(text):
    """窓があれば出す。窓の無い pythonw では出さない"""
    if sys.stdout is not None:
        print(text, flush=True)


def fail(text):
    """開けないことを知らせる。窓の無い pythonw では print が見えないので、Windows の知らせの箱で出す"""
    if sys.stdout is not None:
        print(text, flush=True)
        return
    try:
        import ctypes
        ctypes.windll.user32.MessageBoxW(None, text, "Memorial FrontLand", 0x10)
    except Exception:
        pass


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")

    args = sys.argv[1:]
    port = PORT
    idle = IDLE_SECONDS
    for name in ("--port", "--idle"):
        if name in args:
            at = args.index(name)
            value = int(args[at + 1])
            del args[at:at + 2]
            if name == "--port":
                port = value
            else:
                idle = value
    browser = "--no-browser" not in args
    stay = "--stay" in args
    args = [a for a in args if a not in ("--no-browser", "--stay")]

    here = os.path.dirname(os.path.abspath(__file__))
    folder = os.path.abspath(args[0] if args else os.path.join(here, "..", "..", "Builds", "WebGL"))
    if not os.path.isfile(os.path.join(folder, "index.html")):
        fail("書き出しが無い: " + folder)
        return 1

    url = "http://localhost:{}/".format(port)

    # 書き出しごとに違う URL で開く。キャッシュを禁じていなかった前のサーバーで開いたページがブラウザに残っていても、使わせない
    page = "{}?v={}".format(url, int(os.path.getmtime(os.path.join(folder, "index.html"))))
    try:
        server = serve(folder, port)
    except OSError:
        say("{} はもう動いているので、そこで開いた。".format(url))
        if browser:
            webbrowser.open(page)
        return 0

    if stay:
        say("{} で開いた。この窓を閉じると止まる。".format(url))
    else:
        say("{} で開いた。ゲームのページを閉じて {} 分ほどで止まる。".format(url, round(idle / 60)))
        threading.Thread(target=stop_when_idle, args=(server, idle), daemon=True).start()

    if browser:
        webbrowser.open(page)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    return 0


if __name__ == "__main__":
    sys.exit(main())
