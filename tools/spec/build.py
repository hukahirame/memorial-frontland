# -*- coding: utf-8 -*-
"""企画（docs/private/concept.md）・仕様書（docs/systems/*.md）・判断の記録（docs/DECISIONS.md）から、
管理のページ .spec/admin.html を作る。docs は暗号で git に入れる（[D-25]）ので、鍵の無い手元では作らずに終わる。

ページは見え方で、何も書き足さない。中身は仕様書・判断の記録・原簿から毎回読む。
直すときは元の文書を直して、作り直す。
文書どうしの食い違い（無いリンク先、棄却した設計判断を指す所、棄却でない設計判断の範囲に無い道、前提の答えの違い）と、
仕様書の欠け（足りない欄、仕様書の無い関心事のフォルダ）は、問題として出し、終了コード 1 で終わる。
作るたびに Cloudflare にも上げ、上げてあるページを手元と同じにする（[D-41]）。上げられなかったときも終了コード 1。

    python tools/spec/build.py
"""
from __future__ import annotations

import csv
import json
import re
import shutil
import subprocess
import sys
from datetime import datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SYSTEMS = ROOT / "docs" / "systems"
DECISIONS = ROOT / "docs" / "DECISIONS.md"
PRIVATE_DECISIONS = ROOT / "docs" / "private" / "DECISIONS.md"  # 公開しない中身の設計判断。鍵の無い手元では読めない
ITEMS = ROOT / "Assets" / "_Project" / "Resources" / "ItemData.csv"
RECIPES = ROOT / "Assets" / "_Project" / "Data" / "Recipes"
CONCEPT = ROOT / "docs" / "private" / "concept.md"  # 企画。暗号で git に入れる（[D-25]）ので、鍵の無い手元では読めない
TEMPLATE = Path(__file__).with_name("template.html")
OUT = ROOT / ".spec" / "admin.html"

BULLET = re.compile(r"^(\s*)[-*] (.*)$")
LINK = re.compile(r"\[([^\]]+)\]\(([^)\s]+)\)")
ADR_REF = re.compile(r"\[(D-\d+)\](?!\()")
PREMISE = re.compile(r"(D-\d+)\(([a-z])\)")
OPTION = re.compile(r"(?<![\w\]])\(([a-z])\)")  # 選択肢の頭の (a)。D-26(b) のような前提の指し方は数えない
# 効いている仕組みを範囲の道から引く。関心事はフォルダ名を小文字にしたもの（[D-20]）
SCOPE_SYSTEM = re.compile(r"docs/systems/(\w+)\.md|(?:Scripts/(?:Domain|Game)|LegacyScripts)/(\w+)/")
SEPARATOR = re.compile(r"^:?-+:?$")
# 関心事のフォルダを置く層。その直下のフォルダが関心事で、仕様書の id はフォルダ名を小文字にしたもの（[D-20]）
LAYERS = [ROOT / "Assets" / "_Project" / "Scripts" / "Domain", ROOT / "Assets" / "_Project" / "Scripts" / "Game",
          ROOT / "Assets" / "LegacyScripts"]
SPEC_KEYS = ("id", "name", "touches")  # 仕様書の frontmatter に要る鍵
SPEC_SECTIONS = ("受け入れ条件", "やらないこと", "未決")  # どの仕様書にも要る欄


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig").replace("\r\n", "\n")


def readable(path: Path) -> bool:
    """docs は git-crypt の暗号で入れてある。鍵の無い clone では暗号のまま置かれるので、無いものと同じに扱う"""
    if not path.exists():
        return False
    with path.open("rb") as f:
        return not f.read(10).startswith(b"\x00GITCRYPT")


def table_cells(line: str) -> list[str] | None:
    """表の行なら欄の並び。区切りの行は空の並び。表でなければ None"""
    text = line.strip()
    if not text.startswith("|"):
        return None
    cells = [cell.strip() for cell in text.strip("|").split("|")]
    if all(SEPARATOR.match(cell) for cell in cells):
        return []
    return cells


def join(before: str, after: str) -> str:
    """折り返した行をつなぐ。英数どうしのあいだだけ空白を挟む"""
    if not before:
        return after
    spaced = before[-1].isascii() and before[-1].isalnum() and after[:1].isascii() and after[:1].isalnum()
    return before + (" " if spaced else "") + after


def split_frontmatter(text: str) -> tuple[dict, str]:
    if not text.startswith("---\n"):
        return {}, text
    end = text.find("\n---\n", 4)
    if end < 0:
        return {}, text
    meta = {}
    for line in text[4:end].split("\n"):
        key, sep, value = line.partition(":")
        if sep:
            meta[key.strip()] = value.strip()
    return meta, text[end + 5:]


def parse_body(body: str) -> list[dict]:
    """## を節、### を節の中の見出しにして、箇条書き・表・地の文・コードに分ける"""
    sections: list[dict] = []
    current: dict | None = None
    block: dict | None = None
    fence: list[str] | None = None
    for raw in body.split("\n"):
        line = raw.rstrip()
        if fence is not None:
            if line.strip().startswith("```"):
                current["blocks"].append({"type": "code", "text": "\n".join(fence)})
                fence = None
            else:
                fence.append(line)
            continue
        if line.startswith("## "):
            current = {"title": line[3:].strip(), "blocks": []}
            sections.append(current)
            block = None
            continue
        if current is None:
            continue
        if line.strip().startswith("```"):
            fence = []
            block = None
            continue
        if line.startswith("### "):
            current["blocks"].append({"type": "h", "text": line[4:].strip()})
            block = None
            continue
        if not line.strip():
            if block and block["type"] in ("p", "table"):
                block = None
            continue
        cells = table_cells(line)
        if cells is not None:
            if block and block["type"] == "table":
                if cells:
                    block["rows"].append(cells)
            else:
                block = {"type": "table", "head": cells, "rows": []}
                current["blocks"].append(block)
            continue
        bullet = BULLET.match(line)
        if bullet:
            if not (block and block["type"] == "list"):
                block = {"type": "list", "items": []}
                current["blocks"].append(block)
            depth = 0 if len(bullet.group(1)) < 2 else 1
            block["items"].append({"text": bullet.group(2).strip(), "depth": depth})
            continue
        if raw[:1].isspace() and block and block["type"] == "list":
            last = block["items"][-1]
            last["text"] = join(last["text"], line.strip())
            continue
        if block and block["type"] == "p":
            block["text"] = join(block["text"], line.strip())
        else:
            block = {"type": "p", "text": line.strip()}
            current["blocks"].append(block)
    return sections


def texts(sections: list[dict]) -> list[str]:
    out = []
    for section in sections:
        for block in section["blocks"]:
            if block["type"] == "list":
                out.extend(item["text"] for item in block["items"])
            elif block["type"] == "table":
                out.extend(cell for row in [block["head"], *block["rows"]] for cell in row)
            elif block["type"] in ("p", "h"):
                out.append(block["text"])
    return out


def concerns() -> set[str]:
    """関心事の名前。層の直下にあり、C# のソースを持つフォルダの名前を小文字にしたもの"""
    return {folder.name.lower() for layer in LAYERS if layer.is_dir()
            for folder in layer.iterdir() if folder.is_dir() and any(folder.rglob("*.cs"))}


def index_rows() -> list[dict]:
    rows = []
    for line in read(SYSTEMS / "INDEX.md").split("\n"):
        cells = table_cells(line)
        if not cells or len(cells) < 3:
            continue
        link = LINK.fullmatch(cells[0])
        if link:
            rows.append({"id": Path(link.group(2)).stem, "name": cells[1], "summary": cells[2]})
    return rows


ADR_PARTS = ("背景", "検討した選択肢", "決定", "帰結", "経緯")
ADR_STATES = ("確定", "保留中", "棄却")
NEW_LINE = re.compile(r"^(\s{2,}|\([a-z]\)|[-*] |\d+\. )")
# [D-番号] を探す文書。DECISIONS.md の中の参照と、作業の記録（STATE.md）は昔の判断を語るので見ない
REFERRERS = ("docs/systems/*.md", "docs/conventions.md", "docs/CONTRIBUTING.md", "README.md",
             "CLAUDE.md", "AGENTS.md", "FRICTIONS.md", ".editorconfig", ".claude/commands/*.md",
             "Assets/_Project/**/*.cs", "Assets/LegacyScripts/**/*.cs")


def paragraphs(text: str) -> list[list[str]]:
    """段落ごとの行。折り返しはつなぎ、(a)・字下げ・箇条書きで始まる行は行のまま残す"""
    out = []
    for chunk in re.split(r"\n\s*\n", text.strip()):
        lines: list[str] = []
        for raw in chunk.split("\n"):
            if not raw.strip():
                continue
            if lines and not NEW_LINE.match(raw):
                lines[-1] = join(lines[-1], raw.strip())
            else:
                lines.append(raw.strip())
        if lines:
            out.append(lines)
    return out


def adr_entry(chunk: str, private: bool) -> tuple[str, dict] | None:
    head = re.match(r"## \[(D-\d+)\] (.+)", chunk)
    if not head:
        return None
    field = lambda name: (m.group(1).strip() if (m := re.search(rf"^{name}:[ \t]*(.*)$", chunk, re.M)) else "")
    status = field("状態")
    state = next((s for s in ADR_STATES if status.startswith(s)), "")
    chosen = re.fullmatch(r"\(([a-z])\)", field("選択"))
    parts = {}
    for i, name in enumerate(ADR_PARTS):
        rest = "|".join(re.escape(n) for n in ADR_PARTS[i + 1:]) or "$^"
        m = re.search(rf"\*\*{name}\*\*\s*[—-]\s*(.*?)(?=\n\*\*(?:{rest})\*\*|\Z)", chunk, re.S)
        if m:
            parts[name] = paragraphs(m.group(1))
    choices = " ".join(" ".join(lines) for lines in parts.get("検討した選択肢", []))
    return head.group(1), {
        "title": head.group(2).strip(),
        "state": state,
        "note": status[len(state):].strip().removeprefix("（").removesuffix("）"),
        "chosen": chosen.group(1) if chosen else "",
        "options": sorted(set(OPTION.findall(choices))),  # ページで選べる案
        "notice": bool(field("注意")),
        "premise": field("前提"),
        "premises": [{"id": adr, "option": option} for adr, option in PREMISE.findall(field("前提"))],
        "date": field("日付"),
        "scope": [p.strip() for p in field("範囲").split(",") if p.strip()],
        "decision": " ".join(" ".join(lines) for lines in parts.get("決定", [])),
        # 人に見てほしいこと。注意: の行と、前提の答えが変わったこと（adr_problems が足す）
        "attention": [note] if (note := field("注意")) else [],
        "parts": parts,
        "private": private,
        "refs": [],
        "dependents": [],
        "systems": [],
    }


def decisions() -> tuple[dict, list[str]]:
    adrs, problems = {}, []
    for path, private in ((DECISIONS, False), (PRIVATE_DECISIONS, True)):
        if not readable(path):
            continue
        for chunk in re.split(r"\n(?=## \[D-\d+\])", read(path)):
            entry = adr_entry(chunk, private)
            if entry:
                if entry[0] in adrs:
                    problems.append(f"[{entry[0]}] が 2 か所にある")
                adrs[entry[0]] = entry[1]
    return dict(sorted(adrs.items(), key=lambda pair: int(pair[0][2:]))), problems


def adr_problems(adrs: dict) -> list[str]:
    """棄却でない設計判断の範囲に無い道、選択肢の不足、前提の答えの違い、棄却した・無い設計判断を指している所。指している所は adrs に控える"""
    problems = []
    for adr_id, adr in adrs.items():
        if not adr["state"]:
            problems.append(f"[{adr_id}] の状態が確定・保留中・棄却のどれでもない")
            continue
        if adr["state"] == "棄却":
            continue
        if adr["state"] == "確定" and not adr["chosen"]:
            problems.append(f"確定の [{adr_id}] に選択が無い")
        if len(adr["options"]) < 2:
            problems.append(f"[{adr_id}] の選択肢が 2 つ未満。選ぶものが無い問いは設計判断にしない")
        for scope in adr["scope"]:
            found = any(ROOT.glob(scope)) if any(c in scope for c in "*?[") else (ROOT / scope).exists()
            if not found:
                problems.append(f"[{adr_id}] の範囲にある {scope} が無い")
        if adr["premise"] and not adr["premises"]:
            problems.append(f"[{adr_id}] の前提が D-26(b) の形でない")
        for premise in adr["premises"]:
            base = adrs.get(premise["id"])
            if base is None:
                problems.append(f"[{adr_id}] の前提 [{premise['id']}] が判断の記録に無い")
                continue
            base["dependents"].append(adr_id)
            if base["state"] == "棄却":
                adr["attention"].append(f"前提の [{premise['id']}] が棄却された")
                problems.append(f"[{adr_id}] の前提 [{premise['id']}] が棄却された")
            elif base["chosen"] != premise["option"]:
                now = f"({base['chosen']})" if base["chosen"] else "未決"
                adr["attention"].append(f"前提は [{premise['id']}] の ({premise['option']}) だが、今の答えは {now}")
                problems.append(f"[{adr_id}] の前提は [{premise['id']}] の ({premise['option']}) だが、今の答えは {now}")
    files = sorted({path for pattern in REFERRERS for path in ROOT.glob(pattern) if path.is_file()})
    for path in files:
        name = str(path.relative_to(ROOT)).replace("\\", "/")
        text = path.read_text(encoding="utf-8-sig", errors="replace")
        for adr_id in sorted(set(ADR_REF.findall(text))):
            adr = adrs.get(adr_id)
            if adr is None:
                problems.append(f"{name} の [{adr_id}] が判断の記録に無い")
                continue
            adr["refs"].append(name)
            if adr["state"] == "棄却":
                problems.append(f"{name} が棄却の [{adr_id}] を指している")
    return problems


def items() -> dict:
    with ITEMS.open(encoding="utf-8-sig", newline="") as source:
        rows = [row for row in csv.reader(source) if any(cell.strip() for cell in row)]
    recipes = []
    for asset in sorted(RECIPES.glob("*.asset")):
        text = read(asset)
        product = re.search(r"^\s*productId: (.+)$", text, re.M)
        parts = re.findall(r"- itemId: (\S+)\s+displayName: \"?([^\"\n]*)\"?\s+amount: (\d+)", text)
        recipes.append({
            "product": product.group(1).strip() if product else asset.stem,
            "ingredients": [{"id": i, "name": n, "amount": int(a)} for i, n, a in parts],
        })
    return {"source": str(ITEMS.relative_to(ROOT)).replace("\\", "/"), "head": rows[0], "rows": rows[1:],
            "recipes": recipes, "recipeSource": str(RECIPES.relative_to(ROOT)).replace("\\", "/")}


def main() -> int:
    sys.stdout.reconfigure(encoding="utf-8")
    if not (readable(DECISIONS) and readable(SYSTEMS / "INDEX.md")):
        print("docs が暗号のままで読めない。git-crypt の鍵を入れてから作る（git-crypt unlock <鍵>）")
        return 1
    adrs, problems = decisions()
    rows = index_rows()
    order = [row["id"] for row in rows]
    ids = set(order)
    folders = concerns()
    systems = []
    for row in rows:
        path = SYSTEMS / f"{row['id']}.md"
        if not path.exists():
            problems.append(f"INDEX.md の {row['id']} に仕様書が無い")
            continue
        meta, body = split_frontmatter(read(path))
        sections = parse_body(body)
        if meta.get("id") != row["id"]:
            problems.append(f"{row['id']}.md の id が {meta.get('id') or '無い'}。ファイル名と同じにする")
        problems += [f"{row['id']}.md の frontmatter に {key} が無い" for key in SPEC_KEYS if key not in meta]
        titles = {section["title"] for section in sections}
        problems += [f"{row['id']}.md に ## {title} が無い" for title in SPEC_SECTIONS if title not in titles]
        links = set()
        for section in sections:
            if section["title"] != "受け入れ条件":
                continue
            for text in texts([section]):
                links.update(Path(target).stem for _, target in LINK.findall(text) if target.endswith(".md"))
        for text in texts(sections):
            for _, target in LINK.findall(text):
                name = target.split("#")[0]
                if name.endswith(".md") and not (SYSTEMS / name).resolve().exists():
                    problems.append(f"{row['id']}.md のリンク先 {name} が無い")
        systems.append({**row, "meta": meta, "sections": sections, "built": row["id"] in folders,
                        "links": [i for i in order if i in links and i != row["id"]]})
    specs = {path.stem for path in SYSTEMS.glob("*.md") if path.stem != "INDEX"}
    for name in sorted(specs - ids):
        problems.append(f"{name}.md が INDEX.md に無い")
    for name in sorted(folders - specs):
        problems.append(f"関心事のフォルダ {name} に仕様書が無い。docs/systems/{name}.md を足す")
    problems += adr_problems(adrs)
    for adr in adrs.values():
        hits = {(m.group(1) or m.group(2)).lower() for scope in adr["scope"] if (m := SCOPE_SYSTEM.search(scope))}
        adr["systems"] = [i for i in order if i in hits]

    data = {
        "built": datetime.now().strftime("%Y-%m-%d %H:%M"),
        "concept": {"source": str(CONCEPT.relative_to(ROOT)).replace("\\", "/"),
                    "sections": parse_body(read(CONCEPT)) if readable(CONCEPT) else None},
        "systems": systems,
        "adrs": adrs,
        "items": items(),
        "problems": problems,
    }
    payload = json.dumps(data, ensure_ascii=False).replace("</", "<\\/")
    page = read(TEMPLATE).replace("/*DATA*/null", payload, 1)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(page, encoding="utf-8", newline="\n")
    print(f"{OUT.relative_to(ROOT)} を作った（仕組み {len(systems)}・判断 {len(adrs)}）")
    for problem in problems:
        print("  問題: " + problem)
    failure = upload()
    print("Cloudflare に上げた" if failure is None
          else f"  上げられなかった: {failure}（ログインが切れていたら npx --yes wrangler@4.20.0 login）")
    return 1 if problems or failure else 0


def upload() -> str | None:
    """上げられなかったら理由を返す。"""
    npx = shutil.which("npx")
    if not npx:
        return "npx が無い"
    command = [npx, "--yes", "wrangler@4.20.0", "deploy", "--config", "tools/spec/wrangler.jsonc"]
    try:
        run = subprocess.run(command, cwd=ROOT, stdin=subprocess.DEVNULL, capture_output=True,
                             text=True, encoding="utf-8", errors="replace", timeout=300)
    except subprocess.TimeoutExpired:
        return "5 分たっても終わらなかった"
    if run.returncode == 0:
        return None
    lines = [line.strip() for line in (run.stdout + run.stderr).splitlines() if line.strip()]
    return " / ".join(lines[-3:]) or f"wrangler が {run.returncode} で終わった"


if __name__ == "__main__":
    sys.exit(main())
