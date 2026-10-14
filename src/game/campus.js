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
// 树种分布：0-3 = 不同绿色（普通/高瘦/矮胖/黄绿），4 = 樱花，5 = 金叶
const TREE_TYPES = [0, 1, 2, 0, 5, 1, 0, 2, 1, 0, 2, 4, 0, 1, 5, 2, 1, 0, 2, 4, 1, 0, 2, 5, 0, 1, 0, 2, 4, 1, 2, 0, 1, 5]
export const TREES = scatter(34, rnd, { minGap: 2.2 }).map(([x, z], i) => [x, z, TREE_TYPES[i % TREE_TYPES.length]])
export const BUSHES = scatter(26, rnd, { minGap: 1.2, others: TREES }).map(([x, z], i) => [x, z, i % 4])
export const FLOWERS = scatter(22, rnd, { minGap: 1.0, others: [...TREES, ...BUSHES] }).map(([x, z]) => [x, z])
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
