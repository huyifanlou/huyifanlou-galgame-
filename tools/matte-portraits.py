# 把立绘抠成透明 PNG，产出 Portraits/{id}-cut.png（对话立绘用），源 JPG 不动（自选菜单用）。
#
# 全程在无损生成原图（vibe_images/*.png）上做，最后按入库 jpg 的同公式裁切补白到 1024×1536：
#   1 边界 flood-fill 近白背景；
#   2 水印黑框按逐张标定的区域强制透明；
#   3 封闭白底洞（腿间/臂间白三角）用逐张标定的种子点泛填——安全性靠连通性：洞与衣物被深色轮廓隔开
#     互不连通，泛填到轮廓即停；无损源上容差 ±2 足以穿过背景噪声且进不了 255 的衣物白。
#     （JPEG 上做过均值/容差判据均失败：背景与衣物白均值差 0.1~0.9 灰阶互相重叠、轮廓抗锯齿会漏。）
#   4 漂浮白点＝近白连通块且邻域 mostly 已透明；
#   5 背景边界 1~2px 去白边 feather。
# 瑶清寒只有 jpg（背景与白衫同色）：不做洞判定，改清「独立小近白连通块」（裙侧白线碎屑），她的缩小在 jpg 侧完成。
# 换立绘后需重新标定 HOLE_SEEDS / WM_REGION（对着深色底看图点洞）。
#
# 用法: python tools/matte-portraits.py [阈值min通道,默认240]
import sys, glob, os
from collections import deque
from PIL import Image

THR = int(sys.argv[1]) if len(sys.argv) > 1 else 240
OVERRIDE = {"jingying": 232, "jingnan": 234}
DST = "Game/Assets/Resources/Portraits"
SRC = {
    "fanlou": "vibe_images/fanlou-wan-A.png",
    "jingnan": "vibe_images/jingnan-wan-D.png",
    "jingying": glob.glob("vibe_images/jingying-dialogue-v2_*.png")[0],
    "liliya": glob.glob("vibe_images/liliya-dialogue-v2_*.png")[0],
    "ziwei": glob.glob("vibe_images/ziwei-dialogue-unified_*.png")[0],
    "yaoqinghan": os.path.join(DST, "yaoqinghan-dialogue.jpg"),
}
# 种子点＝源图宽高归一化坐标
HOLE_SEEDS = {
    "fanlou": [(0.49, 0.62), (0.40, 0.42)],
    "jingnan": [(0.50, 0.66), (0.37, 0.35), (0.51, 0.61), (0.43, 0.94), (0.57, 0.92)],
    "ziwei": [(0.752, 0.736)],
}
# 水印黑框区域（归一化 x0,y0,x1,y1）
WM_REGION = {
    "fanlou": (0.62, 0.80, 1.0, 1.0),
    "jingnan": (0.62, 0.80, 1.0, 1.0),
    "liliya": (0.0, 0.975, 1.0, 1.0),
}


def components(mask, w, h):
    seen = bytearray(w * h)
    for start in range(w * h):
        if not mask[start] or seen[start]:
            continue
        comp = [start]
        seen[start] = 1
        dq = deque([start])
        while dq:
            i = dq.popleft()
            x, y = i % w, i // w
            for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if 0 <= nx < w and 0 <= ny < h:
                    j = ny * w + nx
                    if mask[j] and not seen[j]:
                        seen[j] = 1
                        dq.append(j)
                        comp.append(j)
        yield comp


for cid, path in SRC.items():
    thr = OVERRIDE.get(cid, THR)
    im = Image.open(path).convert("RGB")
    w, h = im.size
    px = im.load()
    bg = bytearray(w * h)

    def flood(seed_pixels, cond):
        dq = deque()
        for x, y in seed_pixels:
            i = y * w + x
            if not bg[i] and cond(x, y):
                bg[i] = 1
                dq.append((x, y))
        while dq:
            x, y = dq.popleft()
            for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if 0 <= nx < w and 0 <= ny < h:
                    j = ny * w + nx
                    if not bg[j] and cond(nx, ny):
                        bg[j] = 1
                        dq.append((nx, ny))

    flood([(x, 0) for x in range(w)] + [(x, h - 1) for x in range(w)]
          + [(0, y) for y in range(h)] + [(w - 1, y) for y in range(h)],
          lambda x, y: min(px[x, y]) >= thr)

    wm = 0
    if cid in WM_REGION:
        rx0, ry0, rx1, ry1 = WM_REGION[cid]
        dark = bytearray(w * h)
        for y in range(int(ry0 * h), h):
            for x in range(int(rx0 * w), int(rx1 * w)):
                if max(px[x, y]) < 210:
                    dark[y * w + x] = 1
        for comp in components(dark, w, h):
            xs = [i % w for i in comp]
            ys = [i // w for i in comp]
            for y in range(max(0, min(ys) - 6), min(h, max(ys) + 7)):
                for x in range(max(0, min(xs) - 6), min(w, max(xs) + 7)):
                    if not bg[y * w + x]:
                        bg[y * w + x] = 1
                        wm += 1

    cs = [px[x, y] for x in (0, w - 1) for y in (0, h - 1)]
    bgc = tuple(sorted(c)[1] for c in zip(*cs))

    def near_bg(x, y, tol):
        r, g, b = px[x, y]
        return abs(r - bgc[0]) <= tol and abs(g - bgc[1]) <= tol and abs(b - bgc[2]) <= tol

    holes = 0
    for fx, fy in HOLE_SEEDS.get(cid, []):
        sx, sy = int(fx * w), int(fy * h)
        before = sum(bg)
        flood([(sx, sy)], lambda x, y: near_bg(x, y, 8))
        holes += sum(bg) - before

    cand = bytearray(w * h)
    for y in range(h):
        for x in range(w):
            i = y * w + x
            if not bg[i] and min(px[x, y]) >= thr - 6:
                cand[i] = 1
    specks = 0
    for comp in components(cand, w, h):
        area = len(comp)
        bgtouch = 0
        sr = sg = sb = 0
        for i in comp:
            x, y = i % w, i // w
            r, g, b = px[x, y]
            sr += r; sg += g; sb += b
            for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if not (0 <= nx < w and 0 <= ny < h) or bg[ny * w + nx]:
                    bgtouch += 1
        # 瑶清寒：独立小连通块且均值纯白才是碎屑（皮肤暖白高光均值偏暖，不吃）
        pure_white = max(abs(sr / area - 255), abs(sg / area - 255), abs(sb / area - 255)) <= 2
        if bgtouch / (4 * area) >= 0.35 or (cid == "yaoqinghan" and area < 2000 and pure_white):
            specks += area
            for i in comp:
                bg[i] = 1

    out = Image.new("RGBA", (w, h))
    op = out.load()
    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y]
            if bg[y * w + x]:
                op[x, y] = (r, g, b, 0)
                continue
            a = 255
            near = (x > 0 and bg[y * w + x - 1]) or (x < w - 1 and bg[y * w + x + 1]) \
                or (y > 0 and bg[(y - 1) * w + x]) or (y < h - 1 and bg[(y + 1) * w + x])
            if near:
                m = min(r, g, b)
                if m > thr - 40:
                    a = int(max(0, min(255, (thr - m) / 40 * 255)))
            op[x, y] = (r, g, b, a)

    if im.size != (1024, 1536):
        alpha = out.getchannel("A")
        bbox = alpha.getbbox()
        m = int(0.03 * h)
        crop = out.crop((max(0, bbox[0] - m), max(0, bbox[1] - m),
                         min(w, bbox[2] + m), min(h, bbox[3] + m)))
        scale = 1536 / crop.height
        if crop.width * scale > 1024:
            scale = 1024 / crop.width
        crop = crop.resize((round(crop.width * scale), round(crop.height * scale)), Image.LANCZOS)
        canvas = Image.new("RGBA", (1024, 1536), (0, 0, 0, 0))
        canvas.paste(crop, ((1024 - crop.width) // 2, (1536 - crop.height) // 2))
        out = canvas
    dst = os.path.join(DST, cid + "-cut.png")
    out.save(dst)
    kept = sum(1 for v in out.getchannel("A").getdata() if v > 0)
    print(cid, "kept %.1f%%" % (100 * kept / (1024 * 1536)),
          "wm %d holes %d specks %d" % (wm, holes, specks), "src", os.path.basename(path))