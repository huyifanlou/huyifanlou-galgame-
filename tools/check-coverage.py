"""原作底本 -> 扩写稿 覆盖校验与台词计数。

用法：
    python tools/check-coverage.py <起始章> <结束章> <扩写稿.md> [...] [> 输出.txt]

判定：去掉全部标点与空白后做子串匹配。
  [缺]   = 一个字都没出现（真漏）
  [近]   = 只差"的/地/得"或阿拉伯数字与汉字数字（可接受的规范化差异）
只校验正文，扩写稿里 "## 台词分布" 之后的清单一律不参与匹配。
"""
import re
import sys
import pathlib

ORIG = pathlib.Path("Scenario/原作-我的人生是否有问题/原作全文.md")
OUT = pathlib.Path("Scenario/第一卷-初遇/_coverage.txt")

PUNCT = re.compile(r"[，。！？；：、…—－\-·「」『』“”\"'‘’（）()\[\]【】《》<>～~,.!?;:*&#%$@^|/\\_+=\s|｜†‡※→①②③④⑤⚠️]")
DIGITS = {"0": "零", "1": "一", "2": "二", "3": "三", "4": "四", "5": "五",
          "6": "六", "7": "七", "8": "八", "9": "九", "10": "十"}


def norm(s: str) -> str:
    return PUNCT.sub("", s)


def loose(s: str) -> str:
    s = norm(s).replace("地", "的").replace("得", "的")
    for k, v in DIGITS.items():
        s = s.replace(k, v)
    return s


def load_chapters(lo: int, hi: int) -> list[tuple[int, str]]:
    text = ORIG.read_text(encoding="utf-8")
    heads = [(m.start(), int(m.group(1)), m.end()) for m in
             re.finditer(r"^## 第(\d+)章　(.+)$", text, re.M)]
    out: list[tuple[int, str]] = []
    for i, (start, num, body) in enumerate(heads):
        if not (lo <= num <= hi):
            continue
        end = heads[i + 1][0] if i + 1 < len(heads) else len(text)
        for ln in text[body:end].splitlines():
            ln = ln.strip()
            if not ln or ln.startswith("<sub>") or ln.startswith("#") or ln.startswith("---"):
                continue
            out.append((num, ln))
    return out


SPEECH = re.compile(r"^(※?)\s*(?:\*\*)?([^：「【】\n]{1,14})：\s*(?:（[^）]*）)?「")


def classify(lines):
    o = n = inner = stage = narr = 0
    for ln in lines:
        m = SPEECH.match(ln)
        if m:
            if m.group(1):
                n += 1
            else:
                o += 1
            continue
        if re.match(r"^[^【]*※?【内心】", ln):
            inner += 1
        elif re.match(r"^[^【]*※?【演出】", ln):
            stage += 1
        elif re.match(r"^[^【]*※?【旁白】", ln):
            narr += 1
    return dict(orig=o, new=n, inner=inner, stage=stage, narr=narr)


def fmt(c, label):
    t = c["orig"] + c["new"]
    x = t / c["orig"] if c["orig"] else 0
    return (f"{label}：原句 {c['orig']} + 新写 {c['new']} = {t}（{x:.2f} 倍）"
            f" ｜ 内心 {c['inner']} ｜ 演出 {c['stage']} ｜ 旁白 {c['narr']}")


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    lo, hi = int(sys.argv[1]), int(sys.argv[2])
    files = sys.argv[3:]
    orig = load_chapters(lo, hi)
    raw = []
    for f in files:
        t = pathlib.Path(f).read_text(encoding="utf-8")
        raw.append(re.split(r"^## 台词分布", t, 1, flags=re.M)[0])
    body = "\n".join(raw)
    hay, hay_loose = norm(body), loose(body)

    # 第 1~4 章当初是照"美化版 docx"扩写的，docx 已经润过字（的/地/得、错字、语气）。
    # 那四章里对不上原作全文的行，先在这里查一遍是不是"docx 就是这么写的"。
    alt_path = pathlib.Path("Scenario/原作-我的人生是否有问题/参照-美化版docx抽取.txt")
    ALT = norm(alt_path.read_text(encoding="utf-8")) if alt_path.exists() else None
    alt = 0

    hit = near = miss = 0
    buf = [f"=== 原作第 {lo}~{hi} 章 底本行 {len(orig)}"]
    for num, ln in orig:
        if norm(ln) in hay:
            hit += 1
        elif loose(ln) in hay_loose:
            near += 1
            buf.append(f"  [近] ch{num} {ln}")
        elif num <= 4 and ALT and norm(ln) in ALT:
            alt += 1
            buf.append(f"  [d] ch{num} {ln}   <- 只在美化版 docx 里有对应写法（的/地/得、错字、润色）")
        else:
            miss += 1
            buf.append(f"  [缺] ch{num} {ln}")
    buf.insert(1, f"=== 命中 {hit} ｜ 近似 {near} ｜ 美化版写法差异 {alt} ｜ 未命中 {miss}")

    for f, t in zip(files, raw):
        lines = t.splitlines()
        buf.append(f"\n--- {f}")
        buf.append("  " + fmt(classify(lines), "全章"))
        cur, label = None, None
        for ln in lines:
            if ln.startswith("## 场"):
                if cur is not None:
                    buf.append("    " + label + " -> " + fmt(classify(cur), ""))
                label, cur = ln.replace("## ", ""), []
            if cur is not None:
                cur.append(ln)
        if cur is not None:
            buf.append("    " + label + " -> " + fmt(classify(cur), ""))

    OUT.write_text("\n".join(buf) + "\n", encoding="utf-8")
    print("written", OUT)


main()
