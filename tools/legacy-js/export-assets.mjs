// 资产导出器：把 web 版 v0.40 的程序化像素贴图导出为 PNG，并生成 world.json
// 用法：node export-assets.mjs    →  ../assets/tex/*.png、../assets/world.json
// 说明：固定随机种子（20261015）——导出结果每次完全一致（画稿冻结、可复现）

import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { createCanvas, GlobalFonts } from '@napi-rs/canvas'

// —— 1) 固定随机种子（必须在导入 textures.js 之前替换 Math.random）——
function mulberry32(a) {
  return function () {
    a |= 0
    a = (a + 0x6d2b79f5) | 0
    let t = Math.imul(a ^ (a >>> 15), 1 | a)
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296
  }
}
Math.random = mulberry32(20261015)

// —— 2) 中文字体（招牌文字；缺失时文字会变方块）——
for (const p of ['C:/Windows/Fonts/msyh.ttc', 'C:/Windows/Fonts/msyhbd.ttc', 'C:/Windows/Fonts/simhei.ttf']) {
  try {
    const ok = GlobalFonts.registerFromPath(p, 'Microsoft YaHei')
    console.log('[font]', p, '→', ok)
  } catch (e) {
    console.warn('[font] 跳过', p, e.message)
  }
}

// —— 3) 浏览器 API 垫片：document.createElement('canvas') → @napi-rs/canvas ——
globalThis.document = {
  createElement(tag) {
    if (tag !== 'canvas') throw new Error('shim 仅支持 canvas，收到 ' + tag)
    return createCanvas(8, 8)
  },
}

// roundRect 兜底（操场贴图使用；较新版本 @napi-rs/canvas 已内置）
const proto = createCanvas(2, 2).getContext('2d').constructor.prototype
if (!proto.roundRect) {
  proto.roundRect = function (x, y, w, h, r) {
    r = Math.min(r, w / 2, h / 2)
    this.moveTo(x + r, y)
    this.arcTo(x + w, y, x + w, y + h, r)
    this.arcTo(x + w, y + h, x, y + h, r)
    this.arcTo(x, y + h, x, y, r)
    this.arcTo(x, y, x + w, y, r)
    this.closePath()
  }
}

const here = path.dirname(fileURLToPath(import.meta.url))
const root = path.resolve(here, '..')
// 双份输出：assets/ = 工作区存档（可 diff、可入库）；Game/Assets/Resources/ = Unity 运行时加载
const outDirs = [
  path.join(root, 'assets', 'tex'),
  path.join(root, 'Game', 'Assets', 'Resources', 'tex'),
]
for (const d of outDirs) fs.mkdirSync(d, { recursive: true })

const Traw = await import('./source/textures.js')   // 贴图生成器（下一阶段移植到 C# 后退役）
// 每张贴图独立种子：与 C# TextureLibrary 的"逐张 NewR()"一致 → 可逐像素对拍
const T = {}
for (const k of Object.keys(Traw)) {
  if (typeof Traw[k] !== 'function') continue
  T[k] = (...args) => { Math.random = mulberry32(20261015); return Traw[k](...args) }
}

// 世界数据来源：Unity 侧 C# 权威数据（CampusData.cs）导出的 JSON
// 流程：Unity 菜单「Campus/导出世界数据 JSON（对拍用）」→ Game/Build/world_csharp.json → 本导出器读取
const dumpPath = path.join(root, 'Game', 'Build', 'world_csharp.json')
if (!fs.existsSync(dumpPath)) {
  console.error('[export] 缺少 Game/Build/world_csharp.json —— 先在 Unity 运行 Campus/导出世界数据 JSON（对拍用）')
  process.exit(1)
}
const C = JSON.parse(fs.readFileSync(dumpPath, 'utf8'))

let count = 0
function save(tex, name) {
  const canvas = tex.image
  const buf = canvas.toBuffer('image/png')
  for (const d of outDirs) fs.writeFileSync(path.join(d, name), buf)
  count++
  console.log('   ', name, (buf.length / 1024).toFixed(1) + 'KB')
}

console.log('[1/6] 地面 / 路面 / 花 / 土 / 操场 / 池塘 / 球场')
save(T.grassTexture(1), 'grass.png')
save(T.pathTexture(false, 1), 'path.png')
save(T.flowerTexture(), 'flower.png')
save(T.dirtTexture(), 'dirt.png')
save(T.trackTexture(), 'track.png')
save(T.pondTexture(), 'pond.png')
save(T.courtTexture('basket'), 'court_basket.png')
save(T.courtTexture('tennis'), 'court_tennis.png')
save(T.roadTexture('mainstone'), 'road_mainstone.png')
save(T.roadTexture('cobble'), 'road_cobble.png')
save(T.roadBandTexture('z'), 'roadband_v.png')
save(T.roadBandTexture('x'), 'roadband_h.png')

console.log('[2/6] 天空（昼 / 夜）')
save(T.skyTexture('day'), 'sky_day.png')
save(T.skyTexture('night'), 'sky_night.png')

console.log('[3/6] 路灯 / 角色 / 门 / 雨棚 / 主角家 / 别墅瓦 / 校名牌')
save(T.lampGlowTexture(), 'lamp_glow.png')
save(T.characterSheet(), 'player.png')
save(T.doorTexture(), 'door.png')
save(T.stripesTexture(), 'awning.png')
save(T.villaTexture(), 'villa.png')
save(T.roofTexture('#3a434e'), 'roof_villa.png')
save(T.signTexture('华清大学'), 'sign_gate.png')

console.log('[4/6] 四栋建筑（昼 / 夜外墙 + 招牌 + 瓦片）')
for (const b of C.BUILDINGS) {
  save(T.buildingTexture(b.wall, b.trim, b.plinth, b.cols, b.rows, { banner: b.banner, lit: 0.05 }), `wall_${b.id}_day.png`)
  save(T.buildingTexture(b.wall, b.trim, b.plinth, b.cols, b.rows, { banner: b.banner, lit: 0.55 }), `wall_${b.id}_night.png`)
  if (b.wing) {
    save(T.buildingTexture(b.wall, b.trim, b.plinth, 3, 2, { lit: 0.05 }), `wing_${b.id}_day.png`)
    save(T.buildingTexture(b.wall, b.trim, b.plinth, 3, 2, { lit: 0.55 }), `wing_${b.id}_night.png`)
  }
  save(T.signTexture(b.name), `sign_${b.id}.png`)
  save(T.roofTexture(b.roof), `roof_${b.id}.png`)
}

console.log('[5/6] 远景建筑 26 栋')
C.DISTANT_BUILDINGS.forEach((b, i) => {
  const rows = Math.max(2, Math.round(b.h / 1.7))
  const cols = Math.max(2, Math.round(b.w / 3))
  save(T.buildingTexture(b.wall, b.trim, b.plinth, cols, rows, { lit: 0.16 }), `dist_${String(i).padStart(2, '0')}.png`)
})

// [6/6] world.json 已废弃（2026-10-16）：世界数据改由 C# CampusData.cs 权威维护，
// 本导出器只负责程序化贴图；对拍/备份 JSON 由 Unity 菜单「Campus/导出世界数据 JSON（对拍用）」生成。

console.log(`完成：共导出 ${count} 张贴图 → assets/tex/`)
