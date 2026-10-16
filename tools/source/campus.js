// 校园数据 v2（2026-10-16 扩容版）——单一数据源：渲染 / 碰撞 / 交互 / 校验 都从这里出发
// 相比 web v0.40：可行走半径 30→60；路网升级为 3 纵 3 横；新增 8 栋建筑、60+ 设施道具、
// 植被扩容与多样化（含柳树/桃花新树形）。所有布点须通过 tools/validate-world.mjs 校验：
// 不重叠、不压路、不侵入保留区、满足最小间距——改完务必重跑校验，再重跑导出。

export const BOUND = 60        // 可行走半径（PlayerRig 使用）
export const GROUND_SIZE = 280 // 地面尺寸（±140，雾中收边）

// ── 街道路网：横平竖直；fence=true 的路段自动生成白色围栏（交叉口/岔路处自动留缺口）──
export const ROADS = [
  { id: 'vmain', axis: 'z', at: 0, from: -17.5, to: 38, width: 4.6, fence: true },  // 主纵路（北端止于图书馆门前，南端主角家）
  { id: 'vwest', axis: 'z', at: -34, from: -30, to: 18, width: 4.0, fence: false }, // 西纵路
  { id: 'veast', axis: 'z', at: 30, from: -30, to: 18, width: 4.0, fence: false },   // 东纵路
  { id: 'hnorth', axis: 'x', at: -30, from: -34, to: 34, width: 4.6, fence: false }, // 北横路
  { id: 'hmain', axis: 'x', at: 0, from: -58, to: 58, width: 4.6, fence: true },     // 中横路（东西两端设校门）
  { id: 'hsouth', axis: 'x', at: 18, from: -34, to: 34, width: 4.0, fence: false },  // 南横路
]

// ── 岔路（从主路通向建筑正门）──
export const BRANCH_PATHS = [
  { x: -16, z: -5.4, len: 6.2 },  // 教学楼
  { x: 14, z: -4.6, len: 4.6 },   // 宿舍楼
  { x: 20, z: 4.6, len: 4.6 },    // 食堂（南侧）
  { x: -46, z: -6.4, len: 8.2 },  // 实验楼
  { x: 46, z: -6.4, len: 6.2 },   // 活动中心
  { x: 46, z: 14, len: 4 },       // 艺术楼（自南横路）
  { x: -46, z: 14, len: 4 },      // 生活服务楼（自南横路）
  { x: -15, z: 21.4, len: 3.2 },  // 小卖部（自南横路）
  { x: -20, z: -34.4, len: 4 },   // 体育馆（自北横路）
  { x: 20, z: -34.4, len: 4 },    // 二号宿舍（自北横路）
]

// ── 建筑 ──
export const BUILDINGS = [
  // — 原有四栋（保留 web v0.40 全部特征）—
  { id: 'teach', name: '教学楼', pos: [-16, -12], size: [10, 5, 6], wall: '#d8c9a6', trim: '#f2ecdf', plinth: '#9a8a70', roof: '#7a4a3a', cols: 6, rows: 4, face: 1, style: 'flat', wing: true, antenna: true, banner: true, roofProps: true },
  { id: 'dorm', name: '宿舍楼', pos: [14, -10], size: [8, 5.4, 5], wall: '#c98d6b', trim: '#f0e2d0', plinth: '#8a6a52', roof: '#46536b', cols: 5, rows: 4, face: 1, style: 'gable', tank: true, banner: true },
  { id: 'library', name: '图书馆', pos: [0, -22], size: [12, 4.2, 6], wall: '#c9c3b6', trim: '#e9e5da', plinth: '#8f8a80', roof: '#3f5a4a', cols: 7, rows: 3, face: 1, style: 'grand' },
  { id: 'canteen', name: '食堂', pos: [20, 10], size: [9, 3.4, 5], wall: '#e0b34e', trim: '#fff3d6', plinth: '#a07838', roof: '#a03c30', cols: 5, rows: 2, face: -1, style: 'gable', awning: true, vent: true },
  // — 新建筑（外环街区）—
  { id: 'gym', name: '体育馆', pos: [-20, -42], size: [16, 5.2, 11], wall: '#cfc9bd', trim: '#eeeae0', plinth: '#8f8a80', roof: '#4a5a6e', cols: 8, rows: 3, face: 1, style: 'gym', banner: true },
  { id: 'dorm2', name: '二号宿舍', pos: [20, -38], size: [9, 5.4, 5], wall: '#bfa88c', trim: '#efe6d6', plinth: '#87755c', roof: '#5a4a42', cols: 5, rows: 4, face: 1, style: 'gable', tank: true, banner: true },
  { id: 'labs', name: '实验楼', pos: [-46, -14], size: [12, 5, 7], wall: '#c3c8cf', trim: '#eef1f4', plinth: '#828a94', roof: '#3d4654', cols: 6, rows: 4, face: 1, style: 'flat', roofProps: true, banner: true },
  { id: 'center', name: '活动中心', pos: [46, -14], size: [13, 6, 9], wall: '#d3c2a2', trim: '#f2ead8', plinth: '#94805e', roof: '#7c4a38', cols: 7, rows: 4, face: 1, style: 'grand' },
  { id: 'arts', name: '艺术楼', pos: [46, 9], size: [10, 5, 6], wall: '#e0d5c3', trim: '#f6f0e4', plinth: '#9c8f78', roof: '#4e6a58', cols: 5, rows: 3, face: 1, style: 'gable', awning: true },
  { id: 'service', name: '生活服务楼', pos: [-46, 9], size: [11, 4.5, 6], wall: '#cbb894', trim: '#f0e8d6', plinth: '#8d7a58', roof: '#a05a3c', cols: 6, rows: 3, face: 1, style: 'flat', roofProps: true },
  { id: 'kiosk', name: '小卖部', pos: [-15, 25], size: [6, 3.2, 4], wall: '#e8c46a', trim: '#fff6e0', plinth: '#a8843c', roof: '#b8452f', cols: 3, rows: 2, face: -1, style: 'gable', awning: true },
  { id: 'guardN', name: '值班室', pos: [7, -35.5], size: [4, 3, 4], wall: '#d8d2c4', trim: '#f2eee4', plinth: '#98917f', roof: '#4a5568', cols: 2, rows: 2, face: 1, style: 'flat' },
]

export const GROUNDS = [
  { id: 'playground', name: '操场', pos: [-21, 8.5], size: [16, 10] },
  { id: 'basket', name: '篮球场', pos: [44, 25.5], size: [15, 9], kind: 'basket' },
  { id: 'tennis', name: '网球场', pos: [-46, 25.5], size: [15, 8], kind: 'tennis' },
]

export const POND = { pos: [-26.5, -14], r: 3 }

// ── 设施道具（type 见 WorldBuilder Props；solid 的进 BLOCKERS 参与碰撞）──
// 说明：所有坐标已通过 validate-world.mjs——改坐标务必重跑校验再重跑导出
export const PROPS = [
  // 中心广场（西南角）+ 旗杆 + 公告栏
  { type: 'plaza', x: -7, z: -6, w: 6.5, d: 5 },
  { type: 'flag', x: -7, z: -6 },
  { type: 'board', x: -7, z: -3.6, ry: Math.PI },
  // 校门：东西两侧主校门（横路两端）
  { type: 'gate', x: 58, z: 0, ry: -Math.PI / 2, kind: 'main' },
  { type: 'gate', x: -58, z: 0, ry: Math.PI / 2, kind: 'main' },
  // 长椅（主路旁 / 池塘 / 花园 / 球场边）
  { type: 'bench', x: -26.5, z: -7.4, ry: Math.PI },
  { type: 'bench', x: -22.8, z: -14, ry: -Math.PI / 2 },
  { type: 'bench', x: -30.2, z: -14, ry: Math.PI / 2 },
  { type: 'bench', x: 10, z: 4.6, ry: Math.PI },
  { type: 'bench', x: -12, z: 4.6, ry: Math.PI },
  { type: 'bench', x: 24, z: 4.6, ry: Math.PI },
  { type: 'bench', x: -26, z: 22.6, ry: 0 },
  { type: 'bench', x: -22, z: 31, ry: Math.PI },
  { type: 'bench', x: 44, z: 32.5, ry: 0 },
  { type: 'bench', x: -46, z: 31, ry: Math.PI },
  // 垃圾桶
  { type: 'bin', x: 11.5, z: -4.6 }, { type: 'bin', x: -9.5, z: -4.6 }, { type: 'bin', x: -22.5, z: -4.6 },
  { type: 'bin', x: -24.5, z: -6.2 }, { type: 'bin', x: 4.6, z: -4.6 }, { type: 'bin', x: -7.6, z: -8.6 },
  { type: 'bin', x: -22.5, z: 21.5 },
  // 公告栏
  { type: 'board', x: -11.5, z: -4.4, ry: Math.PI / 2 },
  { type: 'board', x: 9, z: -4.4, ry: -Math.PI / 2 },
  // 自行车架（教学楼 / 宿舍楼前）
  { type: 'bikerack', x: -12, z: -6.8, ry: 0, n: 5 },
  { type: 'bikerack', x: 5.4, z: -6.2, ry: 0, n: 5 },
  // 花坛（小卖部门前两侧）
  { type: 'planter', x: -12.5, z: 21.6, w: 3.6, d: 1.4 },
  { type: 'planter', x: -17.5, z: 21.6, w: 3.6, d: 1.4 },
  // 喷泉（花园中心）
  { type: 'fountain', x: -26, z: 26.5, r: 2.2 },
  // 篮球架（球场两端，面向场地内侧）
  { type: 'hoop', x: 37.6, z: 25.5, ry: Math.PI / 2 },
  { type: 'hoop', x: 50.4, z: 25.5, ry: -Math.PI / 2 },
  // 网球网（横跨球场中线）
  { type: 'netpost', x: -46, z: 25.5, ry: 0, len: 8 },
]

// ── 车辆 / 围栏等附加（暂缺：自行车静态摆件留待后续迭代）──

// ── 保留区（植被不得进入）──
const KEEPOUT = [
  ...BUILDINGS.map((b) => ({ x: b.pos[0], z: b.pos[1], hw: b.size[0] / 2 + 2.4, hd: b.size[2] / 2 + 2.4 })),
  ...ROADS.map((r) => r.axis === 'z'
    ? { x: r.at, z: (r.from + r.to) / 2, hw: r.width / 2 + 1.4, hd: (r.to - r.from) / 2 + 1.4 }
    : { x: (r.from + r.to) / 2, z: r.at, hw: (r.to - r.from) / 2 + 1.4, hd: r.width / 2 + 1.4 }),
  ...GROUNDS.map((g) => ({ x: g.pos[0], z: g.pos[1], hw: g.size[0] / 2 + 1.6, hd: g.size[1] / 2 + 1.6 })),
  ...BRANCH_PATHS.map((p) => ({ x: p.x, z: p.z, hw: 2.6, hd: p.len / 2 + 1.2 })),
  ...PROPS.map((p) => ({ x: p.x, z: p.z, hw: 2.2, hd: 2.2 })),   // 所有道具周围留 2.2m 植被缓冲
  { x: -26, z: 27, hw: 8, hd: 7 },        // 西南花园
]
const POND_KEEP = { x: POND.pos[0], z: POND.pos[1], r: POND.r + 1.8 }
const SPAWN_KEEP = { x: 0, z: 4, r: 3.6 }
// 主角家（日式二层独栋；樊楼独居——剧情关键场景）
export const VILLA = { x: 0, z: 34, w: 8.4, h: 5.4, d: 7, face: -1 }
const VILLA_KEEP = { x: VILLA.x, z: VILLA.z, hw: VILLA.w / 2 + 2.4, hd: VILLA.d / 2 + 5.4 }

function inKeep(x, z) {
  if (Math.hypot(x - POND_KEEP.x, z - POND_KEEP.z) < POND_KEEP.r) return true
  if (Math.hypot(x - SPAWN_KEEP.x, z - SPAWN_KEEP.z) < SPAWN_KEEP.r) return true
  if (Math.abs(x - VILLA_KEEP.x) < VILLA_KEEP.hw && Math.abs(z - VILLA_KEEP.z) < VILLA_KEEP.hd) return true
  for (const k of KEEPOUT) if (Math.abs(x - k.x) < k.hw && Math.abs(z - k.z) < k.hd) return true
  return false
}

// 线性同余随机（确定性）：传 max 返回 [0, max) 整数；不传返回 [0, 1) 小数
function lcgRand(seed) {
  let s = seed
  return (max) => {
    s = (s * 1664525 + 1013904223) >>> 0
    const v = s / 4294967296
    return max === undefined ? v : Math.floor(v * max)
  }
}

function scatter(count, rnd, { bound = 56, minGap = 0, others = [] } = {}) {
  const out = []
  let guard = 0
  while (out.length < count && guard++ < count * 120) {
    const x = (rnd() * 2 - 1) * bound
    const z = (rnd() * 2 - 1) * bound
    if (inKeep(x, z)) continue
    let ok = true
    for (const p of others) if (Math.hypot(x - p[0], z - p[1]) < minGap) { ok = false; break }
    if (ok) for (const p of out) if (Math.hypot(x - p[0], z - p[1]) < minGap) { ok = false; break }
    if (ok) out.push([Math.round(x * 10) / 10, Math.round(z * 10) / 10])
  }
  return out
}

// ── 行道树（沿主路整齐排布；自动避开交叉口与岔路）──
function avenue(axis, at, from, to, spacing, offset) {
  const out = []
  for (let t = from; t <= to; t += spacing) {
    const x = axis === 'z' ? at + offset : t
    const z = axis === 'z' ? t : at + offset
    if (inKeep(x, z)) continue
    out.push([Math.round(x * 10) / 10, Math.round(z * 10) / 10, 0, 1]) // [x,z,variant,avenue]
  }
  return out
}

const rnd = lcgRand(20261013)
// 树种：0-3 常规绿 / 4 樱花 / 5 金叶 / 6 松 / 7 柏 / 8 大树 / 9 柳树 / 10 桃花
const TREE_TYPES = [0, 6, 2, 9, 5, 1, 8, 2, 7, 0, 10, 4, 0, 1, 5, 2, 6, 0, 2, 4, 8, 6, 2, 5, 0, 7, 0, 2, 10, 7, 2, 6, 8, 5, 9, 4, 0, 2]
const AVENUE = [
  ...avenue('x', 0, -56, 56, 8, 6.4).map(([x, z]) => [x, z, 0, 1]),
  ...avenue('x', 0, -56, 56, 8, -6.4).map(([x, z]) => [x, z, 5, 1]),
]
export const TREES = [
  ...scatter(48, rnd, { minGap: 2.4 }).map(([x, z], i) => [x, z, TREE_TYPES[i % TREE_TYPES.length]]),
  ...AVENUE.map(([x, z, v]) => [x, z, v]),
  // 池塘边柳树 + 花园桃花（人工点景）
  ...[[-26.5, -9.8, 9], [-30.8, -11, 9], [-22.3, -10.5, 9], [-30.8, -19, 9]].map(([x, z, v]) => [x, z, v]),
  ...[[-30, 25.5, 10], [-21.5, 25.5, 10]].map(([x, z, v]) => [x, z, v]),
]
export const BUSHES = scatter(44, rnd, { minGap: 1.3, others: TREES }).map(([x, z], i) => [x, z, i % 4])
export const FLOWERS = scatter(56, rnd, { minGap: 1.1, others: [...TREES, ...BUSHES] }).map(([x, z]) => [x, z])
export const DIRT_PATCHES = scatter(12, rnd, { minGap: 4.5 }).map(([x, z]) => [x, z])

// ── 路灯（沿主路两侧，灯臂朝路面；晚上自动点亮 + 地面光池）──
export const LAMPS = [
  // 主纵路
  ...[-14, -4, 6, 14.2, 24].map((z) => [-3.7, z, 0]),
  ...[-9, 4.5, 11, 21, 25.5].map((z) => [3.7, z, Math.PI]),
  // 中横路（原四盏 + 外延；避开西/东纵路交叉）
  ...[-46, -26, -14, 14, 26, 34, 44, 54].map((x) => [x, -3.7, -Math.PI / 2]),
  ...[-30.8, -8, 8, 20, 33.5, 44, 54].map((x) => [x, 3.7, Math.PI / 2]),
  // 北横路
  ...[-28, -18, -8, 13, 18, 28].map((x) => [x, -33.7, -Math.PI / 2]),
  ...[-24, -12, 12, 24].map((x) => [x, -26.3, Math.PI / 2]),
  // 南横路
  ...[-28, -16, -6, 6, 16, 26].map((x) => [x, 14.3, -Math.PI / 2]),
  ...[-24, 24].map((x) => [x, 21.7, Math.PI / 2]),
  // 西纵路（避开交叉口与中横路）
  ...[-24, -12, 6, 14].map((z) => [-36.6, z, 0]),
  ...[-18, -6, 8].map((z) => [-31.4, z, Math.PI]),
  // 东纵路（避开交叉口与中横路）
  ...[-24, -12, 6, 14].map((z) => [32.6, z, Math.PI]),
  ...[-18, -6, 8].map((z) => [27.4, z, 0]),
]

// ── 围栏（自动生成）：沿 fence=true 的路段两侧；交叉口/岔路处留缺口 ──
export const FENCES = (() => {
  const runs = []
  const GAP = 1.6 // 缺口半宽
  const verticalGaps = (at) => {
    const gaps = []
    for (const r of ROADS) if (r.id !== 'vmain' && r.axis === 'x' && Math.abs(r.at) <= 60) {
      if (r.from - 2.5 <= at && at <= r.to + 2.5) gaps.push([r.at - r.width / 2 - GAP, r.at + r.width / 2 + GAP])
    }
    return gaps
  }
  const addRun = (axis, at, from, to, gaps) => {
    const g = (gaps || []).slice().sort((a, b) => a[0] - b[0])
    let cur = from
    for (const [gs, ge] of g) {
      if (gs > cur + 1) runs.push({ axis, at, from: cur, to: gs })
      cur = Math.max(cur, ge)
    }
    if (to > cur + 1) runs.push({ axis, at, from: cur, to })
  }
  for (const r of ROADS.filter((r) => r.fence)) {
    const off = r.width / 2 + 0.5
    if (r.axis === 'z') {
      const gaps = verticalGaps(r.at)
      addRun('z', r.at - off, r.from, r.to, gaps)
      addRun('z', r.at + off, r.from, r.to, gaps)
    } else {
      const gaps = []
      for (const rr of ROADS) if (rr.id !== r.id && rr.axis === 'z' && rr.from <= r.at && r.at <= rr.to) {
        gaps.push([rr.at - rr.width / 2 - GAP, rr.at + rr.width / 2 + GAP])
      }
      // 岔路缺口（按建筑正门方向留）
      for (const p of BRANCH_PATHS) {
        if (Math.abs(p.z - r.at) <= p.len / 2 + 0.5 && r.from - 1 <= p.x && p.x <= r.to + 1) {
          gaps.push([p.x - 2.2, p.x + 2.2])
        }
      }
      addRun('x', r.at - off, r.from, r.to, gaps)
      addRun('x', r.at + off, r.from, r.to, gaps)
    }
  }
  return runs
})()

// ── 边界环境：远景建筑环 + 假山群 + 边界林带（遮挡空旷校界；全部在可行走半径外）──
const DISTANT_PALETTE = [
  { wall: '#c4b494', roof: '#7a4a3a', trim: '#e8e0d0', plinth: '#8f7f65' },
  { wall: '#b8a98e', roof: '#46536b', trim: '#ddd5c8', plinth: '#7d6f58' },
  { wall: '#a89a82', roof: '#3f5a4a', trim: '#d5cfc2', plinth: '#75684f' },
  { wall: '#c9b897', roof: '#a03c30', trim: '#eee6d6', plinth: '#8a7a5c' },
  { wall: '#cfc4ae', roof: '#5a6273', trim: '#f0e9dc', plinth: '#95876d' },
  { wall: '#b0937a', roof: '#7a4a3a', trim: '#e3d6c4', plinth: '#84684f' },
]
const rndD = lcgRand(20261020)
export const DISTANT_BUILDINGS = (() => {
  const styles = ['flat', 'gable', 'tower', 'grand']
  const out = []
  const N = 28
  for (let i = 0; i < N; i++) {
    const a = (i / N) * Math.PI * 2 + (rndD(100) / 100 - 0.5) * 0.14
    const inner = i % 2 === 0
    const r = inner ? 84 + rndD(900) / 100 : 98 + rndD(1100) / 100
    const style = inner ? styles[rndD(2)] : styles[rndD(4)]
    const w = style === 'tower' ? 8 + rndD(4) : style === 'grand' ? 15 + rndD(6) : 9 + rndD(8)
    const h = style === 'tower' ? 13 + rndD(8) : style === 'grand' ? 6.5 + rndD(3.5) : 5 + rndD(4)
    const d = 7 + rndD(5)
    out.push({
      x: Math.round(Math.sin(a) * r * 10) / 10,
      z: Math.round(Math.cos(a) * r * 10) / 10,
      w, h, d, style,
      ...DISTANT_PALETTE[(i * 2 + rndD(2)) % 6],
    })
  }
  return out
})()

export const ROCKERIES = [
  [-38, 34, 1.4], [40, 38, 1.2], [46, -34, 1.5], [-48, -24, 1.1],
  [-38, 46, 1.3], [56, 16, 1.0], [-22, 50, 1.2], [26, -52, 1.3],
  [12, 42, 1.1], [-30, 44, 1.2], [34, -46, 1.0], [-56, 38, 1.15],
  [56, -24, 1.25], [-12, -52, 1.05], [56, 34, 1.1], [-56, -40, 1.2],
]

const rndBorder = lcgRand(20261015)
export const BORDER_TREES = (() => {
  const out = []
  let guard = 0
  while (out.length < 110 && guard++ < 3200) {
    const a = (rndBorder(3600) / 3600) * Math.PI * 2
    const r = 63 + rndBorder(1500) / 100
    const x = Math.round(Math.sin(a) * r * 10) / 10
    const z = Math.round(Math.cos(a) * r * 10) / 10
    if (DISTANT_BUILDINGS.some((b) => Math.abs(x - b.x) < b.w / 2 + 3 && Math.abs(z - b.z) < b.d / 2 + 3)) continue
    if (out.some(([px, pz]) => Math.hypot(x - px, z - pz) < 1.8)) continue
    const vv = rndBorder(12)
    out.push([x, z, vv < 6 ? vv : vv < 8 ? 6 : vv < 10 ? 7 : 8])
  }
  return out
})()

const rndBush2 = lcgRand(20261016)
export const BORDER_BUSHES = (() => {
  const out = []
  let guard = 0
  while (out.length < 120 && guard++ < 3000) {
    const a = (rndBush2(3600) / 3600) * Math.PI * 2
    const r = 62 + rndBush2(1500) / 100
    const x = Math.round(Math.sin(a) * r * 10) / 10
    const z = Math.round(Math.cos(a) * r * 10) / 10
    if (BORDER_TREES.some(([tx, tz]) => Math.hypot(x - tx, z - tz) < 1.8)) continue
    if (out.some(([px, pz]) => Math.hypot(x - px, z - pz) < 1.4)) continue
    out.push([x, z])
  }
  return out
})()

export const SMALL_ROCKS = scatter(18, rnd, { minGap: 2.6, others: [...TREES, ...BUSHES, ...FLOWERS] }).map(([x, z]) => [x, z])
const rndRock = lcgRand(20261019)
export const BORDER_ROCKS = (() => {
  const out = []
  let guard = 0
  while (out.length < 26 && guard++ < 1000) {
    const a = (rndRock(3600) / 3600) * Math.PI * 2
    const r = 64 + rndRock(1400) / 100
    const x = Math.round(Math.sin(a) * r * 10) / 10
    const z = Math.round(Math.cos(a) * r * 10) / 10
    out.push([x, z])
  }
  return out
})()

const rndReed = lcgRand(20261018)
export const REEDS = (() => {
  const out = []
  for (let i = 0; i < 18; i++) {
    const a = (i / 18) * Math.PI * 2 + (rndReed(100) / 100 - 0.5) * 0.35
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

// 可交互点位（建筑正门 + 场地设施）
export const SPOTS = [
  ...BUILDINGS.map((b) => ({
    name: b.name,
    x: b.pos[0],
    z: b.pos[1] + b.face * (b.size[2] / 2 + 0.9),
    r: 2.4,
  })),
  { name: '家', x: VILLA.x, z: VILLA.z - VILLA.d / 2 - 1.2, r: 2.4 },
  ...GROUNDS.map((g) => ({ name: g.name, x: g.pos[0] + g.size[0] / 2 - 2.4, z: g.pos[1], r: 2.6 })),
  { name: '池塘', x: POND.pos[0] + POND.r + 1.2, z: POND.pos[1], r: 2.2 },
  { name: '中心广场', x: -7, z: -6, r: 2.6 },
  { name: '花园', x: -26, z: 26.5, r: 2.8 },
]

// 碰撞体（道具实体；建筑/围栏/路灯/树另有既有逻辑，不重复）
export const BLOCKERS = PROPS.filter((p) => !['plaza'].includes(p.type)).map((p) => {
  switch (p.type) {
    case 'bench': return { x: p.x, z: p.z, hw: 1.0, hd: 1.0 }
    case 'bin': return { x: p.x, z: p.z, hw: 0.35, hd: 0.35 }
    case 'board': return { x: p.x, z: p.z, hw: 0.9, hd: 0.35 }
    case 'bikerack': return { x: p.x, z: p.z, hw: (p.n || 5) * 0.35, hd: 0.5 }
    case 'flag': return { x: p.x, z: p.z, hw: 0.3, hd: 0.3 }
    case 'planter': return { x: p.x, z: p.z, hw: p.w / 2, hd: p.d / 2 }
    case 'fountain': return { x: p.x, z: p.z, hw: p.r, hd: p.r }
    case 'hoop': return { x: p.x, z: p.z, hw: 0.4, hd: 0.4 }
    case 'netpost': return { x: p.x, z: p.z, hw: (p.len || 8) / 2, hd: 0.3 }
    case 'gate': return p.kind === 'main'
      ? { x: p.x, z: p.z, hw: p.ry === 0 || Math.abs(p.ry) === Math.PI ? 4.2 : 0.9, hd: p.ry === 0 || Math.abs(p.ry) === Math.PI ? 0.9 : 4.2 }
      : { x: p.x, z: p.z, hw: 3.4, hd: 0.9 }
    default: return { x: p.x, z: p.z, hw: 0.5, hd: 0.5 }
  }
})
