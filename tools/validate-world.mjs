// 世界布点校验器：确保不重叠、不压路、不侵入保留区、满足间距
// 用法：node validate-world.mjs   （零违规才算通过；改动 campus.js 后必须重跑）
import { BUILDINGS, GROUNDS, POND, ROADS, BRANCH_PATHS, PROPS, FENCES, LAMPS, TREES, BUSHES, FLOWERS, DIRT_PATCHES, SMALL_ROCKS, ROCKERIES, REEDS, BORDER_TREES, BORDER_BUSHES, BORDER_ROCKS, DISTANT_BUILDINGS, VILLA, SPOTS, BOUND } from './source/campus.js'

const errors = []
const warns = []
const rect = (x, z, hw, hd) => ({ x, z, hw, hd })
const overlap = (a, b, pad = 0) =>
  Math.abs(a.x - b.x) < a.hw + b.hw + pad && Math.abs(a.z - b.z) < a.hd + b.hd + pad

// —— 各实体轮廓 ——
const bFoot = BUILDINGS.map((b) => ({ id: b.id, kind: 'building', ...rect(b.pos[0], b.pos[1], b.size[0] / 2, b.size[2] / 2) }))
const gFoot = GROUNDS.map((g) => ({ id: g.id, kind: 'ground', ...rect(g.pos[0], g.pos[1], g.size[0] / 2, g.size[1] / 2) }))
const roadBands = ROADS.map((r) => r.axis === 'z'
  ? { id: r.id, kind: 'road', ...rect(r.at, (r.from + r.to) / 2, r.width / 2, (r.to - r.from) / 2) }
  : { id: r.id, kind: 'road', ...rect((r.from + r.to) / 2, r.at, (r.to - r.from) / 2, r.width / 2) })
const pondFoot = { id: 'pond', kind: 'pond', ...rect(POND.pos[0], POND.pos[1], POND.r + 0.6, POND.r + 0.6) }
const villaFoot = { id: 'villa', kind: 'villa', ...rect(VILLA.x, VILLA.z, VILLA.w / 2 + 1.2, VILLA.d / 2 + 4.6) }
const propFoot = PROPS.filter((p) => p.type !== 'plaza').map((p) => {
  const b = { bench: [1.0, 0.6], bin: [0.35, 0.35], board: [0.9, 0.35], flag: [0.3, 0.3], hoop: [0.4, 0.4],
    planter: [p.w / 2, p.d / 2], fountain: [p.r, p.r], netpost: [(p.len || 8) / 2, 0.3],
    gate: p.kind === 'main' ? (p.ry === 0 || Math.abs(p.ry) === Math.PI ? [4.2, 0.9] : [0.9, 4.2]) : [4.4, 0.9],
    arch: [3.8, 0.5],
    bikerack: [(p.n || 5) * 0.35, 0.5] }[p.type] || [0.5, 0.5]
  return { id: p.type + '@' + p.x + ',' + p.z, kind: 'prop:' + p.type, ...rect(p.x, p.z, b[0], b[1]) }
})

// 1) 建筑相互 / 建筑 vs 场地 / 建筑 vs 道路
for (let i = 0; i < bFoot.length; i++) {
  for (let j = i + 1; j < bFoot.length; j++) {
    if (overlap(bFoot[i], bFoot[j], 2.0)) errors.push(`建筑重叠：${bFoot[i].id} × ${bFoot[j].id}`)
  }
  for (const g of gFoot) if (overlap(bFoot[i], g, 1.5)) errors.push(`建筑压场地：${bFoot[i].id} × ${g.id}`)
  for (const r of roadBands) if (overlap(bFoot[i], r, 1.0)) errors.push(`建筑压路：${bFoot[i].id} × ${r.id}`)
  if (overlap(bFoot[i], pondFoot, 1.0)) errors.push(`建筑压池塘：${bFoot[i].id}`)
  if (overlap(bFoot[i], villaFoot, 1.0)) errors.push(`建筑压主角家：${bFoot[i].id}`)
}
// 2) 道路相互：平行道不重叠；交叉允许（本数据全为正交网格）
for (let i = 0; i < roadBands.length; i++) {
  for (let j = i + 1; j < roadBands.length; j++) {
    const a = roadBands[i], b = roadBands[j]
    const aV = a.hw < a.hd, bV = b.hw < b.hd
    if (aV !== bV) continue // 正交 → 交叉合法
    if (overlap(a, b, 0.5)) errors.push(`道路重叠：${a.id} × ${b.id}`)
  }
}
// 3) 场地 vs 道路 / 场地相互
for (const g of gFoot) {
  for (const r of roadBands) if (overlap(g, r, 1.0)) errors.push(`场压路：${g.id} × ${r.id}`)
  if (overlap(g, pondFoot, 0.5)) errors.push(`场地压池塘：${g.id}`)
  if (overlap(g, villaFoot, 1.0)) errors.push(`场地压主角家：${g.id}`)
}
for (let i = 0; i < gFoot.length; i++) for (let j = i + 1; j < gFoot.length; j++)
  if (overlap(gFoot[i], gFoot[j], 1.5)) errors.push(`场地重叠：${gFoot[i].id} × ${gFoot[j].id}`)
// 4) 道具 vs 建筑/道路/场地/道具
for (let i = 0; i < propFoot.length; i++) {
  const p = propFoot[i]
  for (const b of bFoot) if (overlap(p, b, 0.6)) errors.push(`道具压建筑：${p.id} × ${b.id}`)
  if (p.kind !== 'prop:gate' && p.kind !== 'prop:arch') for (const r of roadBands) if (overlap(p, r, 0.4)) errors.push(`道具压路：${p.id} × ${r.id}`)
  for (const g of gFoot) if (['fountain', 'flag', 'hoop', 'netpost'].includes(p.kind.split(':')[1]) && overlap(p, g, 0.3)) warns.push(`道具贴近场地（确认站位）：${p.id} × ${g.id}`)
  for (let j = i + 1; j < propFoot.length; j++) if (overlap(p, propFoot[j], 0.5)) errors.push(`道具重叠：${p.id} × ${propFoot[j].id}`)
}
// 5) 围栏不压路/建筑（围栏由 ROADS 自动生成，此处复核缺口有效）
for (const f of FENCES) {
  const line = f.axis === 'z' ? rect(f.at, (f.from + f.to) / 2, 0.15, (f.to - f.from) / 2) : rect((f.from + f.to) / 2, f.at, (f.to - f.from) / 2, 0.15)
  for (const r of roadBands) if (overlap(line, r, -0.2)) errors.push(`围栏压路：${f.axis}@${f.at} [${f.from},${f.to}] × ${r.id}`)
  for (const b of bFoot) if (overlap(line, b, 0.2)) errors.push(`围栏压建筑：${f.axis}@${f.at} × ${b.id}`)
}
// 6) 路灯：不压路面（距中心线 > 半宽 + 0.3）、不压建筑、不互相重叠
const lampFoot = LAMPS.map(([x, z], i) => ({ id: 'lamp' + i, kind: 'lamp', ...rect(x, z, 0.3, 0.3) }))
for (const l of lampFoot) {
  for (const r of roadBands) {
    const d = r.hw < r.hd ? Math.abs(l.x - r.x) : Math.abs(l.z - r.z)
    const half = r.hw < r.hd ? r.hw : r.hd
    const along = r.hw < r.hd ? (l.z > r.z - r.hd - 1 && l.z < r.z + r.hd + 1) : (l.x > r.x - r.hw - 1 && l.x < r.x + r.hw + 1)
    if (along && d < half + 0.3) errors.push(`路灯压路/贴路：${l.id} 距 ${r.id} 仅 ${d.toFixed(2)}m（要求 >${(half + 0.3).toFixed(2)}）`)
  }
  for (const b of bFoot) if (overlap(l, b, 0.3)) errors.push(`路灯压建筑：${l.id} × ${b.id}`)
}
for (let i = 0; i < lampFoot.length; i++) for (let j = i + 1; j < lampFoot.length; j++)
  if (overlap(lampFoot[i], lampFoot[j], 0.2)) errors.push(`路灯重叠：${lampFoot[i].id} × ${lampFoot[j].id}`)

// 7) 植被：不压建筑/道路/场地/道具/池塘/家；树树间距
const vegChecks = [
  ['树', TREES, 0.9], ['灌木', BUSHES, 0.6], ['花', FLOWERS, 0.4], ['土斑', DIRT_PATCHES, 2.4], ['小石头', SMALL_ROCKS, 0.3],
]
for (const [label, list, r] of vegChecks) {
  for (const [x, z] of list) {
    const f = { x, z, hw: r, hd: r }
    for (const b of bFoot) if (overlap(f, b, 0.2)) errors.push(`${label}压建筑：(${x},${z}) × ${b.id}`)
    for (const rr of roadBands) if (overlap(f, rr, 0.2)) errors.push(`${label}压路：(${x},${z}) × ${rr.id}`)
    for (const g of gFoot) if (label !== '土斑' && overlap(f, g, -0.6)) errors.push(`${label}压场地：(${x},${z}) × ${g.id}`)
    if (overlap(f, pondFoot, -0.5)) errors.push(`${label}压池塘：(${x},${z})`)
    if (overlap(f, villaFoot, -1.0)) errors.push(`${label}压主角家：(${x},${z})`)
    for (const p of propFoot) if (label === '树' || label === '灌木') if (overlap(f, p, 0.2)) errors.push(`${label}压道具：(${x},${z}) × ${p.id}`)
  }
}
for (let i = 0; i < TREES.length; i++) for (let j = i + 1; j < TREES.length; j++) {
  const a = TREES[i], b = TREES[j]
  if (Math.hypot(a[0] - b[0], a[1] - b[1]) < 1.8) errors.push(`树过近：(${a[0]},${a[1]}) × (${b[0]},${b[1]})`)
}
// 8) 假山 vs 建筑/道路/场地
for (const [x, z, s] of ROCKERIES) {
  const f = { id: 'rockery@' + x + ',' + z, x, z, hw: 1.5 * s, hd: 1.5 * s }
  for (const b of bFoot) if (overlap(f, b, 0.5)) errors.push(`假山压建筑：${f.id} × ${b.id}`)
  for (const r of roadBands) if (overlap(f, r, 0.3)) errors.push(`假山压路：${f.id} × ${r.id}`)
  for (const g of gFoot) if (overlap(f, g, 0.3)) errors.push(`假山压场地：${f.id} × ${g.id}`)
}
// 9) 可行走半径内的边界植被（应为空——它们都在环带外）
for (const [x, z] of [...BORDER_TREES, ...BORDER_BUSHES, ...BORDER_ROCKS]) {
  if (Math.hypot(x, z) < BOUND - 2) errors.push(`边界植被侵入可行走区：(${x},${z}) r=${Math.hypot(x, z).toFixed(1)}`)
}
// 10) 远景建筑不互相重叠、不与边界树冲突、在环带内
for (let i = 0; i < DISTANT_BUILDINGS.length; i++) {
  const b = DISTANT_BUILDINGS[i]
  const f = { x: b.x, z: b.z, hw: b.w / 2, hd: b.d / 2 }
  const r = Math.hypot(b.x, b.z)
  if (r < 78) errors.push(`远景楼侵入内圈：${i} r=${r.toFixed(1)}`)
  if (r > 112) warns.push(`远景楼过远（雾外）：${i} r=${r.toFixed(1)}`)
  for (let j = i + 1; j < DISTANT_BUILDINGS.length; j++) {
    const c = DISTANT_BUILDINGS[j]
    if (overlap(f, { x: c.x, z: c.z, hw: c.w / 2, hd: c.d / 2 }, 2.0)) errors.push(`远景楼重叠：${i} × ${j}`)
  }
  for (const [tx, tz] of BORDER_TREES) {
    if (Math.abs(tx - b.x) < b.w / 2 + 2 && Math.abs(tz - b.z) < b.d / 2 + 2) errors.push(`边界树压远景楼：(${tx},${tz}) × ${i}`)
  }
}
// 11) 交互点：不在建筑碰撞体内
for (const s of SPOTS) {
  for (const b of bFoot) {
    if (Math.abs(s.x - b.x) < b.hw + 0.3 && Math.abs(s.z - b.z) < b.hd + 0.3) errors.push(`交互点在建筑内：${s.name}`)
  }
}

// 12) 边界植被互距（避免重叠成一坨）
for (let i = 0; i < BORDER_TREES.length; i++) for (let j = i + 1; j < BORDER_TREES.length; j++) {
  const a = BORDER_TREES[i], b = BORDER_TREES[j]
  if (Math.hypot(a[0] - b[0], a[1] - b[1]) < 1.2) errors.push(`边界树过密：(${a[0]},${a[1]}) × (${b[0]},${b[1]})`)
}
for (const [bx, bz] of BORDER_BUSHES) {
  for (const [tx, tz] of BORDER_TREES) {
    if (Math.hypot(bx - tx, bz - tz) < 1.4) errors.push(`边界灌木贴树：(${bx},${bz}) × (${tx},${tz})`)
  }
}
// 13) 道具 / 路灯 不压围栏
for (const f of FENCES) {
  const line = f.axis === 'z' ? rect(f.at, (f.from + f.to) / 2, 0.2, (f.to - f.from) / 2) : rect((f.from + f.to) / 2, f.at, (f.to - f.from) / 2, 0.2)
  for (const p of propFoot) if (p.kind !== 'prop:gate' && p.kind !== 'prop:arch' && overlap(p, line, 0.1)) errors.push(`道具压围栏：${p.id} × 围栏${f.axis}@${f.at}`)
  for (const l of lampFoot) if (overlap(l, line, 0.0)) errors.push(`路灯压围栏：${l.id} × 围栏${f.axis}@${f.at}`)
}
// 14) 可达性：每栋建筑正门前 6m 内应有道路或岔路
const reachTargets = [...bFoot]
for (const b of reachTargets) {
  const def = BUILDINGS.find((x) => x.id === b.id)
  const fx = def.pos[0], fz = def.pos[1] + def.face * (def.size[2] / 2 + 5)
  let ok = false
  for (const r of roadBands) {
    const inside = Math.abs(fx - r.x) <= r.hw + 2 && Math.abs(fz - r.z) <= r.hd + 2
    if (inside) { ok = true; break }
  }
  if (!ok) for (const p of BRANCH_PATHS) {
    if (Math.hypot(fx - p.x, fz - p.z) <= p.len / 2 + 3) { ok = true; break }
  }
  if (!ok) warns.push(`可达性存疑：${b.id} 正门前 5m 未见道路/岔路`)
}

// —— 输出 ——
const counts = { 建筑: BUILDINGS.length, 场地: GROUNDS.length, 道具: PROPS.length, 路灯: LAMPS.length, 树: TREES.length, 灌木: BUSHES.length, 花: FLOWERS.length, 土斑: DIRT_PATCHES.length, 假山: ROCKERIES.length, 小石头: SMALL_ROCKS.length, 芦苇: REEDS.length, 边界树: BORDER_TREES.length, 边界灌木: BORDER_BUSHES.length, 边界石: BORDER_ROCKS.length, 远景楼: DISTANT_BUILDINGS.length, 围栏段: FENCES.length, 可交互点: SPOTS.length }
console.log('== 数量 ==', JSON.stringify(counts, null, 0))
if (warns.length) { console.log(`\n== 警告 ${warns.length} ==`); warns.slice(0, 20).forEach((w) => console.log(' ! ' + w)) }
if (errors.length) {
  console.log(`\n== 违规 ${errors.length} ==`)
  errors.slice(0, 40).forEach((e) => console.log(' ✗ ' + e))
  if (errors.length > 40) console.log(`… 其余 ${errors.length - 40} 条省略`)
  process.exit(1)
} else {
  console.log('\n✓ 校验通过：零违规')
}
