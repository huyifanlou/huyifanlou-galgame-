// 校园数据：单一数据源——渲染、碰撞、交互检测都从这里生成

export const BUILDINGS = [
  { id: 'teach', name: '教学楼', pos: [-12, -10], size: [10, 5, 6], wall: '#b39a7c', roof: '#45465a', cols: 6, rows: 4, face: 1, style: 'flat', wing: true, antenna: true },
  { id: 'dorm', name: '宿舍楼', pos: [12, -8], size: [8, 5.4, 5], wall: '#9aa0b0', roof: '#3c4252', cols: 5, rows: 4, face: 1, style: 'gable', tank: true },
  { id: 'library', name: '图书馆', pos: [0, -18], size: [12, 4.2, 6], wall: '#8f7a63', roof: '#4a3f56', cols: 7, rows: 3, face: 1, style: 'grand' },
  { id: 'canteen', name: '食堂', pos: [17, 9], size: [9, 3.4, 5], wall: '#b07a58', roof: '#5a3f3a', cols: 5, rows: 2, face: -1, style: 'gable', awning: true },
]

export const GROUNDS = [
  { id: 'playground', name: '操场', pos: [-21, 7.5], size: [16, 10] },
]

export const POND = { pos: [-15, -17], r: 3 }

// —— 植被：确定性随机 + 排除区，自然散布且每次加载布局一致 ——
const KEEPOUT = [
  ...BUILDINGS.map((b) => ({ x: b.pos[0], z: b.pos[1], hw: b.size[0] / 2 + 2.2, hd: b.size[2] / 2 + 2.2 })),
  { x: 0, z: 0, hw: 3.0, hd: 37 }, // 纵向路走廊
  { x: 0, z: 0, hw: 37, hd: 3.0 }, // 横向路走廊
  { x: GROUNDS[0].pos[0], z: GROUNDS[0].pos[1], hw: GROUNDS[0].size[0] / 2 + 1.6, hd: GROUNDS[0].size[1] / 2 + 1.6 },
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
export const TREES = scatter(22, rnd, { minGap: 2.6 }).map(([x, z], i) => [x, z, i % 3])
export const BUSHES = scatter(16, rnd, { minGap: 1.4, others: TREES }).map(([x, z]) => [x, z])
export const FLOWERS = scatter(12, rnd, { minGap: 1.2, others: [...TREES, ...BUSHES] }).map(([x, z]) => [x, z])

// 可交互点位（建筑正门 + 操场 + 池塘）
export const SPOTS = [
  ...BUILDINGS.map((b) => ({
    name: b.name,
    x: b.pos[0],
    z: b.pos[1] + b.face * (b.size[2] / 2 + 0.9),
    r: 2.4,
  })),
  { name: '操场', x: GROUNDS[0].pos[0] + GROUNDS[0].size[0] / 2 + 1.4, z: GROUNDS[0].pos[1], r: 2.6 },
  { name: '池塘', x: POND.pos[0] + POND.r + 1.2, z: POND.pos[1], r: 2.2 },
]
