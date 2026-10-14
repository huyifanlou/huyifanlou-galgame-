// 校园数据：单一数据源——渲染、碰撞、交互检测都从这里生成

export const BUILDINGS = [
  { id: 'teach', name: '教学楼', pos: [-16, -12], size: [10, 5, 6], wall: '#d8c9a6', trim: '#f2ecdf', plinth: '#9a8a70', roof: '#7a4a3a', cols: 6, rows: 4, face: 1, style: 'flat', wing: true, antenna: true, banner: true, roofProps: true },
  { id: 'dorm', name: '宿舍楼', pos: [14, -10], size: [8, 5.4, 5], wall: '#c98d6b', trim: '#f0e2d0', plinth: '#8a6a52', roof: '#46536b', cols: 5, rows: 4, face: 1, style: 'gable', tank: true, banner: true },
  { id: 'library', name: '图书馆', pos: [0, -22], size: [12, 4.2, 6], wall: '#c9c3b6', trim: '#e9e5da', plinth: '#8f8a80', roof: '#3f5a4a', cols: 7, rows: 3, face: 1, style: 'grand' },
  { id: 'canteen', name: '食堂', pos: [20, 10], size: [9, 3.4, 5], wall: '#e0b34e', trim: '#fff3d6', plinth: '#a07838', roof: '#a03c30', cols: 5, rows: 2, face: -1, style: 'gable', awning: true, vent: true },
]

export const GROUNDS = [
  { id: 'playground', name: '操场', pos: [-21, 7.5], size: [16, 10] },
]

export const POND = { pos: [-25, -14], r: 3 }

// 岔路：从主路通往各建筑（与栅栏缺口对齐）
export const BRANCH_PATHS = [
  { x: -16, z: -5.4, len: 6.2 }, // 教学楼
  { x: 14, z: -4.6, len: 4.6 }, // 宿舍楼
  { x: 20, z: 4.6, len: 4.6 }, // 食堂（南侧）
]

// 栅栏段：沿两条主路两侧；缺口对齐十字路口 / 岔路 / 操场入口
export const FENCES = [
  // 纵路两侧（北端止于图书馆门前）
  { axis: 'z', at: -2.8, from: -19, to: -3.3 },
  { axis: 'z', at: 2.8, from: -19, to: -3.3 },
  { axis: 'z', at: -2.8, from: 3.3, to: 34 },
  { axis: 'z', at: 2.8, from: 3.3, to: 34 },
  // 横路北侧（缺口：教学楼岔路 / 宿舍楼岔路）
  { axis: 'x', at: -2.8, from: -34, to: -17.5 },
  { axis: 'x', at: -2.8, from: -14.5, to: -3.3 },
  { axis: 'x', at: -2.8, from: 3.3, to: 12.5 },
  { axis: 'x', at: -2.8, from: 15.5, to: 34 },
  // 横路南侧（缺口：操场入口 / 食堂岔路）
  { axis: 'x', at: 2.8, from: -34, to: -22.5 },
  { axis: 'x', at: 2.8, from: -19.5, to: -3.3 },
  { axis: 'x', at: 2.8, from: 3.3, to: 18.5 },
  { axis: 'x', at: 2.8, from: 21.5, to: 34 },
]

// —— 植被：确定性随机 + 排除区，自然散布且每次加载布局一致 ——
const KEEPOUT = [
  ...BUILDINGS.map((b) => ({ x: b.pos[0], z: b.pos[1], hw: b.size[0] / 2 + 2.2, hd: b.size[2] / 2 + 2.2 })),
  { x: 0, z: 0, hw: 3.0, hd: 37 },
  { x: 0, z: 0, hw: 37, hd: 3.0 },
  { x: GROUNDS[0].pos[0], z: GROUNDS[0].pos[1], hw: GROUNDS[0].size[0] / 2 + 1.6, hd: GROUNDS[0].size[1] / 2 + 1.6 },
  ...BRANCH_PATHS.map((p) => ({ x: p.x, z: p.z, hw: 2.6, hd: p.len / 2 + 1.2 })),
]
const POND_KEEP = { x: POND.pos[0], z: POND.pos[1], r: POND.r + 1.8 }
const SPAWN_KEEP = { x: 0, z: 4, r: 3.6 }

function inKeep(x, z) {
  if (Math.hypot(x - POND_KEEP.x, z - POND_KEEP.z) < POND_KEEP.r) return true
  if (Math.hypot(x - SPAWN_KEEP.x, z - SPAWN_KEEP.z) < SPAWN_KEEP.r) return true
  for (const k of KEEPOUT) {
    if (Math.abs(x - k.x) < k.hw && Math.abs(z - k.z) < k.hd) return true
  }
  return false
}

function lcgRand(seed) {
  let s = seed
  return () => (s = (s * 1664525 + 1013904223) >>> 0) / 4294967296
}

function scatter(count, rnd, { bound = 28, minGap = 0, others = [] } = {}) {
  const out = []
  let guard = 0
  while (out.length < count && guard++ < count * 80) {
    const x = (rnd() * 2 - 1) * bound
    const z = (rnd() * 2 - 1) * bound
    if (inKeep(x, z)) continue
    let ok = true
    for (const p of others) {
      if (Math.hypot(x - p[0], z - p[1]) < minGap) { ok = false; break }
    }
    if (ok) for (const p of out) {
      if (Math.hypot(x - p[0], z - p[1]) < minGap) { ok = false; break }
    }
    if (ok) out.push([Math.round(x * 10) / 10, Math.round(z * 10) / 10])
  }
  return out
}

const rnd = lcgRand(20261013)
// 树种分布：0-3 = 不同绿色（普通/高瘦/矮胖/黄绿），4 = 樱花，5 = 金叶，6 = 松树，7 = 柏树，8 = 大树
const TREE_TYPES = [0, 6, 2, 0, 5, 1, 8, 2, 7, 0, 2, 4, 0, 1, 5, 2, 6, 0, 2, 4, 8, 6, 2, 5, 0, 7, 0, 2, 4, 7, 2, 6, 8, 5]
export const TREES = scatter(34, rnd, { minGap: 2.2 }).map(([x, z], i) => [x, z, TREE_TYPES[i % TREE_TYPES.length]])
export const BUSHES = scatter(26, rnd, { minGap: 1.2, others: TREES }).map(([x, z], i) => [x, z, i % 4])
export const FLOWERS = scatter(30, rnd, { minGap: 1.0, others: [...TREES, ...BUSHES] }).map(([x, z]) => [x, z])
// 土斑（点缀在草地上，不与路面冲突：渲染层级低于路面）
// 注：曾用"草地色块"圆盘（GROUND_PATCHES）做地面深浅变化，因贴地叠层与主草地纹理密度不一致、远处呈现圆形淡影，已移除；地面层次改由草地纹理内置的草丛色块承担
export const DIRT_PATCHES = scatter(7, rnd, { minGap: 4 }).map(([x, z]) => [x, z])

// 路灯（沿主路两侧，灯臂朝路面；晚上自动点亮 + 地面光池）
// 注意避开操场(x-29..-13, z2.5..12.5)与池塘(center -25,-14 r3)——横路南侧西端灯移至 x=-32 绕开操场
export const LAMPS = [
  ...[-14, -4, 6, 16, 26].map((z) => [-3.7, z, 0]), // 竖路西侧，灯臂朝东
  ...[-9, 1, 11, 21, 31].map((z) => [3.7, z, Math.PI]), // 竖路东侧，灯臂朝西
  ...[-26, -14, 14, 26].map((x) => [x, -3.7, -Math.PI / 2]), // 横路北侧，灯臂朝南
  ...[-32, -8, 8, 20].map((x) => [x, 3.7, Math.PI / 2]), // 横路南侧，灯臂朝北
]

// ── 边界环境（遮挡空旷校界：远景建筑环 + 假山群 + 边界林带）──
// 远景建筑环：26 栋两圈（内圈 44-58 / 外圈 58-72），四种风格（平顶/坡顶/塔楼/公共）+ 六套配色
const DISTANT_PALETTE = [
  { wall: '#c4b494', roof: '#7a4a3a', trim: '#e8e0d0', plinth: '#8f7f65' }, // 奶油+砖红
  { wall: '#b8a98e', roof: '#46536b', trim: '#ddd5c8', plinth: '#7d6f58' }, // 灰褐+藏青
  { wall: '#a89a82', roof: '#3f5a4a', trim: '#d5cfc2', plinth: '#75684f' }, // 灰绿
  { wall: '#c9b897', roof: '#a03c30', trim: '#eee6d6', plinth: '#8a7a5c' }, // 暖黄+红顶
  { wall: '#cfc4ae', roof: '#5a6273', trim: '#f0e9dc', plinth: '#95876d' }, // 米白+蓝灰
  { wall: '#b0937a', roof: '#7a4a3a', trim: '#e3d6c4', plinth: '#84684f' }, // 砖棕
]
const rndD = lcgRand(20261020)
export const DISTANT_BUILDINGS = (() => {
  const styles = ['flat', 'gable', 'tower', 'grand']
  const out = []
  const N = 26
  for (let i = 0; i < N; i++) {
    const a = (i / N) * Math.PI * 2 + (rndD(100) / 100 - 0.5) * 0.14
    const inner = i % 2 === 0
    const r = inner ? 50 + rndD(1000) / 100 : 62 + rndD(1200) / 100
    const style = inner ? styles[rndD(2)] : styles[rndD(4)] // 内圈只放低层（平顶/坡顶），塔楼与公共建筑在外圈
    const w = style === 'tower' ? 7 + rndD(3) : style === 'grand' ? 14 + rndD(5) : 8 + rndD(7)
    const h = style === 'tower' ? 12 + rndD(7) : style === 'grand' ? 6 + rndD(3) : 4.5 + rndD(3.5)
    const d = 6 + rndD(4)
    out.push({
      x: Math.round(Math.sin(a) * r * 10) / 10,
      z: Math.round(Math.cos(a) * r * 10) / 10,
      w,
      h,
      d,
      style,
      ...DISTANT_PALETTE[(i * 2 + rndD(2)) % 6],
    })
  }
  return out
})()

// 假山群：校区外角落的岩石堆（尺寸按 scale 缩放）
export const ROCKERIES = [
  [-38, 30, 1.4],
  [36, 36, 1.2],
  [42, -33, 1.5],
  [-45, -10, 1.1],
  [-34, 42, 1.3],
  [44, 12, 1.0],
  [-20, 46, 1.2],
  [22, -44, 1.3],
  [10, 38, 1.1],
  [-28, 40, 1.2],
  [30, -40, 1.0],
  [-40, 22, 1.15],
  [44, -10, 1.25],
  [-8, -42, 1.05],
]

// 边界林带：校界外 33-48 半径的密植树圈（90 棵，避开主路走廊与远景建筑）
const rndBorder = lcgRand(20261015)
export const BORDER_TREES = (() => {
  const out = []
  let guard = 0
  while (out.length < 90 && guard++ < 1800) {
    const a = (rndBorder(3600) / 3600) * Math.PI * 2
    const r = 33 + rndBorder(1500) / 100
    const x = Math.round(Math.sin(a) * r * 10) / 10
    const z = Math.round(Math.cos(a) * r * 10) / 10
    if (Math.abs(x) < 4.5 && Math.abs(z) < 44) continue // 竖路走廊
    if (Math.abs(z) < 4.5 && Math.abs(x) < 44) continue // 横路走廊
    if (DISTANT_BUILDINGS.some((b) => Math.abs(x - b.x) < b.w / 2 + 3 && Math.abs(z - b.z) < b.d / 2 + 3)) continue // 避让远景建筑
    const vv = rndBorder(12)
    out.push([x, z, vv < 6 ? vv : vv < 8 ? 6 : vv < 10 ? 7 : 8]) // 常规树 + 松/柏/大树混植
  }
  return out
})()

// 边界灌木：校界外 32-46 半径散布（100 丛，填充树线间隙）
const rndBush2 = lcgRand(20261016)
export const BORDER_BUSHES = (() => {
  const out = []
  let guard = 0
  while (out.length < 100 && guard++ < 1600) {
    const a = (rndBush2(3600) / 3600) * Math.PI * 2
    const r = 32 + rndBush2(1400) / 100
    const x = Math.round(Math.sin(a) * r * 10) / 10
    const z = Math.round(Math.cos(a) * r * 10) / 10
    if (Math.abs(x) < 3.5 && Math.abs(z) < 44) continue
    if (Math.abs(z) < 3.5 && Math.abs(x) < 44) continue
    out.push([x, z])
  }
  return out
})()

// 小石头：校区内 12 块 + 环带 22 块
export const SMALL_ROCKS = scatter(12, rnd, { minGap: 2.5, others: [...TREES, ...BUSHES, ...FLOWERS] }).map(([x, z]) => [x, z])
const rndRock = lcgRand(20261019)
export const BORDER_ROCKS = (() => {
  const out = []
  let guard = 0
  while (out.length < 22 && guard++ < 800) {
    const a = (rndRock(3600) / 3600) * Math.PI * 2
    const r = 33 + rndRock(1400) / 100
    const x = Math.round(Math.sin(a) * r * 10) / 10
    const z = Math.round(Math.cos(a) * r * 10) / 10
    if (Math.abs(x) < 3.5 && Math.abs(z) < 44) continue
    if (Math.abs(z) < 3.5 && Math.abs(x) < 44) continue
    out.push([x, z])
  }
  return out
})()

// 池塘芦苇：沿池沿一圈 14 丛（细杆丛，黄绿三色）
const rndReed = lcgRand(20261018)
export const REEDS = (() => {
  const out = []
  for (let i = 0; i < 14; i++) {
    const a = (i / 14) * Math.PI * 2 + (rndReed(100) / 100 - 0.5) * 0.35
    const r = 3.75 + rndReed(50) / 100
    out.push([
      Math.round((POND.pos[0] + Math.cos(a) * r) * 100) / 100,
      Math.round((POND.pos[1] + Math.sin(a) * r) * 100) / 100,
      0.7 + rndReed(70) / 100,
      rndReed(3),
    ])
  }
  return out
})()

// 可交互点位（建筑正门 + 操场 + 池塘）
export const SPOTS = [
  ...BUILDINGS.map((b) => ({
    name: b.name,
    x: b.pos[0],
    z: b.pos[1] + b.face * (b.size[2] / 2 + 0.9),
    r: 2.4,
  })),
  { name: '操场', x: GROUNDS[0].pos[0] + GROUNDS[0].size[0] / 2 - 2.4, z: GROUNDS[0].pos[1], r: 2.6 },
  { name: '池塘', x: POND.pos[0] + POND.r + 1.2, z: POND.pos[1], r: 2.2 },
]
