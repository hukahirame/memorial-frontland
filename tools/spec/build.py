# -*- coding: utf-8 -*-
"""企画（docs/private/concept.md）・仕様書（docs/systems/*.md）・判断の記録（docs/DECISIONS.md）から、
仕様のページ .spec/spec.html を作る。企画は git に入れないので、無ければ「この手元に無い」と出す。

ページは見え方で、何も書き足さない。中身は仕様書・判断の記録・原簿から毎回読む。
直すときは元の文書を直して、作り直す。
文書どうしの食い違い（無いリンク先、廃止した ADR を指す所、採用中の ADR の scope に無い道）は
問題として出し、終了コード 1 で終わる。

    python tools/spec/build.py
"""
from __future__ import annotations

import csv
import json
import re
import sys
from datetime import datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SYSTEMS = ROOT / "docs" / "systems"
DECISIONS = ROOT / "docs" / "DECISIONS.md"
ITEMS = ROOT / "Assets" / "_Project" / "Resources" / "ItemData.csv"
RECIPES = ROOT / "Assets" / "_Project" / "Data" / "Recipes"
CONCEPT = ROOT / "docs" / "private" / "concept.md"  # 企画。git に入れない（[D-025]）ので、無い手元もある
TEMPLATE = Path(__file__).with_name("template.html")
OUT = ROOT / ".spec" / "spec.html"

BULLET = re.compile(r"^(\s*)[-*] (.*)$")
LINK = re.compile(r"\[([^\]]+)\]\(([^)\s]+)\)")
ADR_REF = re.compile(r"\[(D-\d{3})\](?!\()")
SEPARATOR = re.compile(r"^:?-+:?$")


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig").replace("\r\n", "\n")


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
            if block and block["type"] == "p":
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


ADR_PARTS = ("背景", "検討した選択肢", "決定", "帰結")
ADR_STATES = (("Accepted", "active"), ("Proposed", "proposed"), ("Superseded", "superseded"), ("Retired", "retired"))
NEW_LINE = re.compile(r"^(\s{2,}|\([a-z]\)|[-*] |\d+\. )")
# [D-xxx] を探す文書。DECISIONS.md の中の参照と、作業の記録（STATE.md）は昔の判断を語るので見ない
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


def decisions() -> dict:
    adrs = {}
    for chunk in re.split(r"\n(?=## \[D-\d{3}\])", read(DECISIONS)):
        head = re.match(r"## \[(D-\d{3})\] (.+)", chunk)
        if not head:
            continue
        field = lambda name: (m.group(1).strip() if (m := re.search(rf"^{name}: (.+)$", chunk, re.M)) else "")
        status = field("status")
        state = next((s for word, s in ADR_STATES if status.startswith(word)), "active")
        parts = {}
        for i, name in enumerate(ADR_PARTS):
            rest = "|".join(re.escape(n) for n in ADR_PARTS[i + 1:]) or "$^"
            m = re.search(rf"\*\*{name}\*\*\s*[—-]\s*(.*?)(?=\n\*\*(?:{rest})\*\*|\Z)", chunk, re.S)
            if m:
                parts[name] = paragraphs(m.group(1))
        adrs[head.group(1)] = {
            "title": head.group(2).strip(),
            "status": status,
            "state": state,
            "date": field("date"),
            "scope": [p.strip() for p in field("scope").split(",") if p.strip()],
            "decision": " ".join(" ".join(lines) for lines in parts.get("決定", [])),
            "parts": parts,
            "refs": [],
        }
    return adrs


def adr_problems(adrs: dict) -> list[str]:
    """採用中の ADR の scope に無い道と、廃止・置き換え済み・無い ADR を指している所。指している所は adrs に控える"""
    problems = []
    for adr_id, adr in adrs.items():
        if adr["state"] not in ("active", "proposed"):
            continue
        for scope in adr["scope"]:
            found = any(ROOT.glob(scope)) if any(c in scope for c in "*?[") else (ROOT / scope).exists()
            if not found:
                problems.append(f"採用中の [{adr_id}] の scope にある {scope} が無い")
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
            if adr["state"] in ("superseded", "retired"):
                problems.append(f"{name} が{'置き換え済み' if adr['state'] == 'superseded' else '廃止'}の [{adr_id}] を指している")
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
    adrs = decisions()
    rows = index_rows()
    order = [row["id"] for row in rows]
    ids = set(order)
    problems = []
    systems = []
    for row in rows:
        path = SYSTEMS / f"{row['id']}.md"
        if not path.exists():
            problems.append(f"INDEX.md の {row['id']} に仕様書が無い")
            continue
        meta, body = split_frontmatter(read(path))
        sections = parse_body(body)
        links = set()
        for section in sections:
            if section["title"] != "相互作用":
                continue
            for text in texts([section]):
                links.update(Path(target).stem for _, target in LINK.findall(text) if target.endswith(".md"))
        for text in texts(sections):
            for _, target in LINK.findall(text):
                name = target.split("#")[0]
                if name.endswith(".md") and not (SYSTEMS / name).resolve().exists():
                    problems.append(f"{row['id']}.md のリンク先 {name} が無い")
        systems.append({**row, "meta": meta, "sections": sections,
                        "links": [i for i in order if i in links and i != row["id"]]})
    for path in sorted(SYSTEMS.glob("*.md")):
        if path.stem != "INDEX" and path.stem not in ids:
            problems.append(f"{path.name} が INDEX.md に無い")
    problems += adr_problems(adrs)

    data = {
        "built": datetime.now().strftime("%Y-%m-%d %H:%M"),
        "concept": {"source": str(CONCEPT.relative_to(ROOT)).replace("\\", "/"),
                    "sections": parse_body(read(CONCEPT)) if CONCEPT.exists() else None},
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
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
