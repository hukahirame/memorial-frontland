"""WebGL の書き出しに要る物だけを、写した先の Assets に置く。

自作の物（_Project・LegacyScripts・WebGLTemplates）は丸ごと写す。素材集（Assets 直下のほかの物）からは、
書き出しに入るシーン・Resources・ProjectSettings から GUID の参照をたどって届くファイルと、コードとシェーダーだけを写す。
コードとシェーダーは GUID ではなく名前やコンパイルでつながるので、素材集のものも全部写す。
3D モデルに埋め込まれた素材は、絵を読み込みのときに名前で探すのでたどれない。いまは使っていない（--report で数える）。

使い方:
  python -I assets_for_build.py <元のプロジェクト> <写す先> --report   要る量とたどれない参照を数えるだけ
  python -I assets_for_build.py <元のプロジェクト> <写す先>            写す先の Assets を合わせる
"""
import os
import re
import shutil
import sys

# 書き出しに入れない物。HeroEditor の一覧は Resources にあり、装備の絵を丸ごと引き込む。
# 主人公は自分の絵を部品に直に持ち、一覧は見た目を作り直すエディタの道具からしか使われない
LEFT_OUT = {
    "Assets/HeroEditor/FantasyHeroes/Resources/SpriteCollection.asset",
    "Assets/HeroEditor/FantasyHeroes/Resources/IconCollection.asset",
    "Assets/HeroEditor/FantasyHeroes/Resources/FirearmCollection.asset",
}

# 丸ごと写す自作の物
OURS = ("_Project", "LegacyScripts", "WebGLTemplates")

# 参照ではなく名前やコンパイルでつながる物。素材集のものも全部写す
CODE = (".cs", ".asmdef", ".asmref", ".dll", ".jslib", ".jspre", ".rsp", ".shader", ".cginc", ".hlsl",
        ".compute", ".glslinc", ".shadergraph", ".shadersubgraph", ".raytrace")

# 中身が丸ごと書き出しに入る、または名前で読まれるフォルダ
WHOLE_FOLDERS = ("Resources", "StreamingAssets", "Plugins")

MODELS = (".fbx", ".obj", ".dae", ".blend", ".3ds", ".max", ".ma", ".mb")

GUID = re.compile(rb"guid: ([0-9a-f]{32})")
MATERIALS = re.compile(rb"m_Materials:\r?\n((?:\s+- \{[^\n]*\}\r?\n)+)")
IMPORTED = re.compile(rb"- \{fileID: -?\d+, guid: ([0-9a-f]{32}), type: 3\}")


def relative(root, path):
    return os.path.relpath(path, root).replace("\\", "/")


def files_under(root, folder):
    for dirpath, _, names in os.walk(os.path.join(root, folder)):
        for name in names:
            yield relative(root, os.path.join(dirpath, name))


def build_index(root):
    """GUID から道へ"""
    owner = {}
    for path in files_under(root, "Assets"):
        if not path.endswith(".meta"):
            continue
        with open(os.path.join(root, path), "rb") as f:
            for line in f:
                if line.startswith(b"guid: "):
                    owner[line[6:38].decode()] = path[:-5]
                    break
    return owner


def read_yaml(root, path):
    full = os.path.join(root, path)
    if not os.path.isfile(full):
        return None
    with open(full, "rb") as f:
        data = f.read()
    return data if data.startswith(b"%YAML") else None


def references(root, path):
    """ファイルとその .meta が指す GUID。YAML でない物は .meta だけを見る"""
    found = set()
    for data in (read_yaml(root, path), _read(root, path + ".meta")):
        if data:
            found.update(match.decode() for match in GUID.findall(data))
    return found


def _read(root, path):
    try:
        with open(os.path.join(root, path), "rb") as f:
            return f.read()
    except OSError:
        return None


def is_ours(path):
    parts = path.split("/")
    return len(parts) > 2 and parts[1] in OURS


def reachable(root, owner):
    """書き出しに入る物から GUID でたどれる物すべて。自作の物も含む"""
    roots = set()
    with open(os.path.join(root, "ProjectSettings", "EditorBuildSettings.asset"), "rb") as f:
        roots.update(match.decode().strip() for match in re.findall(rb"enabled: 1\s+path: (\S[^\r\n]*)", f.read()))

    for name in os.listdir(os.path.join(root, "ProjectSettings")):
        if name.endswith(".asset"):
            data = _read(root, "ProjectSettings/" + name) or b""
            roots.update(owner[g.decode()] for g in GUID.findall(data) if g.decode() in owner)

    for path in files_under(root, "Assets"):
        if path.endswith(".meta") or path in LEFT_OUT:
            continue
        folders = path.split("/")[:-1]
        if any(folder in WHOLE_FOLDERS for folder in folders) or path.endswith(CODE) or path.endswith("/link.xml"):
            roots.add(path)

    needed = set()
    queue = list(roots)
    while queue:
        path = queue.pop()
        if path in needed or path in LEFT_OUT:
            continue
        needed.add(path)
        if os.path.isdir(os.path.join(root, path)):
            # フォルダを指す参照（スプライトアトラスなど）は、中身を丸ごと入れる
            queue.extend(p for p in files_under(root, path) if not p.endswith(".meta"))
            continue
        queue.extend(owner[guid] for guid in references(root, path) if guid in owner)
    return needed


def plan(root):
    """写す先の Assets に置くファイル（.meta を含む）"""
    owner = build_index(root)
    files = {path for path in reachable(root, owner) if path.startswith("Assets/") and os.path.isfile(os.path.join(root, path))}
    for folder in OURS:
        files.update(p for p in files_under(root, "Assets/" + folder) if not p.endswith(".meta"))

    out = set()
    for path in files:
        out.add(path)
        out.add(path + ".meta")
        parent = os.path.dirname(path)
        while parent != "Assets":
            out.add(parent + ".meta")
            parent = os.path.dirname(parent)
    return {path for path in out if os.path.isfile(os.path.join(root, path))}, owner


def report(root, files, owner):
    def pack(path):
        parts = path.split("/")
        return parts[1] if len(parts) > 2 else "(Assets 直下)"

    kept, whole = {}, {}
    for path in files_under(root, "Assets"):
        size = os.path.getsize(os.path.join(root, path))
        whole[pack(path)] = whole.get(pack(path), 0) + size
        if path in files:
            kept[pack(path)] = kept.get(pack(path), 0) + size
    for name in sorted(whole, key=lambda p: -whole[p]):
        print("{:>9.1f} MB / {:>9.1f} MB  {}".format(kept.get(name, 0) / 2**20, whole[name] / 2**20, name))
    print("写す: {:.1f} MB / {:.1f} MB".format(sum(kept.values()) / 2**20, sum(whole.values()) / 2**20))

    # たどれない参照: 描く物が 3D モデルに埋め込まれた素材を使う。中身が YAML でない .asset（地形・焼き込み）
    embedded, binary = set(), []
    for path in files:
        if path.endswith(".asset") and not path.endswith(".meta") and read_yaml(root, path) is None:
            binary.append(path)
        data = read_yaml(root, path) if not path.endswith(".meta") else None
        for block in MATERIALS.finditer(data or b""):
            for guid in IMPORTED.findall(block.group(1)):
                if owner.get(guid.decode(), "").lower().endswith(MODELS):
                    embedded.add(owner[guid.decode()])
    print("埋め込み素材を使うモデル: {}  YAML でない .asset: {}".format(len(embedded), len(binary)))
    for path in sorted(embedded) + sorted(binary):
        print("  " + path)


def mirror(root, workspace, files):
    """写す先の Assets を files と同じにする"""
    copied = 0
    for path in sorted(files):
        source, target = os.path.join(root, path), os.path.join(workspace, path)
        if os.path.isfile(target):
            a, b = os.stat(source), os.stat(target)
            if a.st_size == b.st_size and int(a.st_mtime) == int(b.st_mtime):
                continue
        os.makedirs(os.path.dirname(target), exist_ok=True)
        shutil.copy2(source, target)
        copied += 1

    removed = 0
    for dirpath, _, names in os.walk(os.path.join(workspace, "Assets"), topdown=False):
        for name in names:
            if relative(workspace, os.path.join(dirpath, name)) not in files:
                os.remove(os.path.join(dirpath, name))
                removed += 1
        if dirpath != os.path.join(workspace, "Assets") and not os.listdir(dirpath):
            os.rmdir(dirpath)
    print("Assets: {} 個を写し、{} 個を消した（{} 個）".format(copied, removed, len(files)))


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    root, workspace = os.path.abspath(sys.argv[1]), os.path.abspath(sys.argv[2])
    files, owner = plan(root)
    if "--report" in sys.argv:
        report(root, files, owner)
    else:
        mirror(root, workspace, files)


if __name__ == "__main__":
    main()
