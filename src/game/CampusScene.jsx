import { useEffect, useMemo, useRef } from 'react'
import { useFrame, useThree } from '@react-three/fiber'
import * as THREE from 'three'
import { BUILDINGS, TREES, BUSHES, FLOWERS, GROUNDS, POND, SPOTS, BRANCH_PATHS, FENCES, DIRT_PATCHES, LAMPS, DISTANT_BUILDINGS, ROCKERIES, BORDER_TREES, BORDER_BUSHES, SMALL_ROCKS, BORDER_ROCKS, REEDS } from './campus.js'
import {
  grassTexture, pathTexture, buildingTexture, signTexture,
  flowerTexture, trackTexture, characterSheet, doorTexture, stripesTexture, dirtTexture, skyTexture, lampGlowTexture, pondTexture, wallTexture, starOverlayTexture,
} from './textures.js'
import { useGameStore } from './store.js'

const MOVE_SPEED = 6
const BOUND = 30

// ── 昼夜配置（灯光/雾，逐帧按 nightMix 插值实现平滑过渡）──
const FX_DAY = {
  amb: 0.82, hemi: 0.46, dir: 1.42,
  hemiSky: new THREE.Color('#d8ecff'), hemiGnd: new THREE.Color('#3f5a36'), dirCol: new THREE.Color('#fff4da'),
  fog: new THREE.Color('#cfe3f4'), fogNear: 50, fogFar: 110,
}
const FX_NIGHT = {
  amb: 0.4, hemi: 0.42, dir: 0.55,
  hemiSky: new THREE.Color('#31426b'), hemiGnd: new THREE.Color('#141f19'), dirCol: new THREE.Color('#9fb8e8'),
  fog: new THREE.Color('#111a2c'), fogNear: 38, fogFar: 96,
}
// 清晨/黄昏暖色（时间驱动的金色时刻）
const WARM_DIR = new THREE.Color('#ff9a4a')
const WARM_FOG = new THREE.Color('#e0b28e')
const WARM_SKY = new THREE.Color('#ffcf9e')
const WHITE = new THREE.Color('#ffffff')
const LAMP_GLASS_OFF = new THREE.Color('#d5dbe4')
const LAMP_GLASS_ON = new THREE.Color('#ffd98a')

const smooth = (x) => x * x * (3 - 2 * x)
// 夜晚混合值：0=白天 1=夜晚（清晨 5-7 点天亮，黄昏 17:30-20:00 入夜）
function nightMixAt(h) {
  if (h < 5) return 1
  if (h < 7) return 1 - smooth((h - 5) / 2)
  if (h < 17.5) return 0
  if (h < 20) return smooth((h - 17.5) / 2.5)
  return 1
}
// 暖色强度：清晨 ~6:30 / 黄昏 ~18:30 达峰
function warmAt(h) {
  const w1 = Math.max(0, 1 - Math.abs(h - 6.5) / 1.8)
  const w2 = Math.max(0, 1 - Math.abs(h - 18.4) / 2.2)
  return Math.min(1, Math.max(w1, w2))
}
// 天空混合值：0=白天 1=夜晚（ref 由主组件逐帧更新，配合透明度交叉淡化两层天穹）

// 天空穹顶：内层为夜穹（星空+月亮），透明度随时钟交叉淡化；白天穹随时钟染上晨昏暖色；云缓漂、星空极慢旋转
function SkyDome({ mix, initialNight }) {
  const dayTex = useMemo(() => skyTexture('day'), [])
  const nightTex = useMemo(() => skyTexture('night'), [])
  const nightMat = useRef()
  const nightMesh = useRef()
  const dayMat = useRef()
  useFrame((_, dt) => {
    const m = mix.current
    if (nightMat.current) nightMat.current.opacity = THREE.MathUtils.clamp((m - 0.35) / 0.45, 0, 1)
    if (dayMat.current) {
      const h = useGameStore.getState().gameMinutes / 60
      const warm = warmAt(h) * (1 - m * 0.65)
      dayMat.current.color.lerpColors(WHITE, WARM_SKY, warm * 0.8)
    }
    // 星空极慢旋转（星点爬行即自然闪烁）+ 云飘动
    if (nightMesh.current) nightMesh.current.rotation.y += dt * 0.004
    dayTex.offset.x -= dt * 0.0045
  })
  return (
    <>
      <mesh>
        <sphereGeometry args={[170, 24, 16]} />
        <meshBasicMaterial ref={dayMat} map={dayTex} side={THREE.BackSide} fog={false} />
      </mesh>
      <mesh ref={nightMesh}>
        <sphereGeometry args={[168, 24, 16]} />
        <meshBasicMaterial
          ref={nightMat}
          map={nightTex}
          side={THREE.BackSide}
          fog={false}
          transparent
          opacity={initialNight ? 1 : 0}
          depthWrite={false}
        />
      </mesh>
    </>
  )
}

// 碰撞：建筑 AABB + 树干（小方框）
function collides(x, z) {
  for (const b of BUILDINGS) {
    const [bx, bz] = b.pos
    const [w, , d] = b.size
    if (Math.abs(x - bx) < w / 2 + 0.45 && Math.abs(z - bz) < d / 2 + 0.45) return true
  }
  for (const t of TREES) {
    if (Math.abs(x - t[0]) < 0.75 && Math.abs(z - t[1]) < 0.75) return true
  }
  for (const f of FENCES) {
    if (f.axis === 'z') {
      if (Math.abs(x - f.at) < 0.3 && z > f.from - 0.2 && z < f.to + 0.2) return true
    } else if (Math.abs(z - f.at) < 0.3 && x > f.from - 0.2 && x < f.to + 0.2) return true
  }
  for (const l of LAMPS) {
    if (Math.abs(x - l[0]) < 0.4 && Math.abs(z - l[1]) < 0.4) return true
  }
  return false
}

// 迭代 1+：正式像素校园（像素小人角色 / 操场 / 池塘 / 植被生态 / 建筑多样化）
export default function CampusScene() {
  const player = useRef()
  const sprite = useRef()
  const shadow = useRef()
  const keys = useRef({})
  const drag = useRef(null)
  const camInit = useRef(false)
  const facing = useRef(Math.PI) // 角色朝向（世界角：0=+z 南，π=北）；默认背对镜头
  // 初始视角支持 URL 调试参数（录像取景 / 验收用）：
  //   ?yaw=1.57&pitch=1.2&dist=30   直接指定角度
  //   ?at=-21,7.5                   相机对准世界坐标点（玩家出生点 (0,4) 相对计算）
  const initView = (() => {
    try {
      const q = new URLSearchParams(window.location.search)
      const num = (k, d) => {
        const v = q.get(k)
        return v ? parseFloat(v) || d : d
      }
      const spawn = { x: num('px', 0), z: num('pz', 4) }
      let yaw = num('yaw', 0)
      const at = q.get('at')
      if (at) {
        const [ax, az] = at.split(',').map(Number)
        if (Number.isFinite(ax) && Number.isFinite(az)) {
          yaw = Math.atan2(spawn.x - ax, spawn.z - az)
        }
      }
      return { yaw, pitch: Math.min(1.35, Math.max(0.3, num('pitch', 0.38))), dist: Math.min(40, Math.max(6, num('dist', 13))), spawn, frozen: q.get('freeze') === '1' }
    } catch {
      return { yaw: 0, pitch: 0.38, dist: 13, spawn: { x: 0, z: 4 }, frozen: false }
    }
  })()
  const view = useRef(initView)
  const spawn = initView.spawn
  // 鼠标拖拽时冻结朝向（可看到角色各个面）；Q/E 转视角或移动时恢复"背对镜头"；?freeze=1 可初始冻结（验收取景用）
  const alignPaused = useRef(initView.frozen)
  const gl = useThree((s) => s.gl)
  const scene = useThree((s) => s.scene)

  // 阴影总开关：所有实体网格（Lambert 材质）投射并接收阴影；
  // 天空/光池/精灵等（Basic 材质）自动排除——透明贴片不会投出方块影
  useEffect(() => {
    scene.traverse((o) => {
      if (o.isMesh && o.material && o.material.isMeshLambertMaterial) {
        o.castShadow = true
        o.receiveShadow = true
      }
    })
  }, [scene])

  // 昼夜：单一混合值驱动灯光/雾/天穹交叉淡化（0=白天 1=夜晚），由游戏时钟逐帧计算
  const initialNight = useMemo(() => nightMixAt(useGameStore.getState().gameMinutes / 60) > 0.5, [])
  const mixRef = useRef(initialNight ? 1 : 0)
  const ambRef = useRef()
  const hemiRef = useRef()
  const dirRef = useRef()
  const fogRef = useRef()

  useEffect(() => {
    const down = (e) => {
      keys.current[e.code] = true
      if (e.code === 'KeyF') {
        const st = useGameStore.getState()
        if (st.near) st.showToast(`「${st.near}」内部将在后续迭代开放`)
      }
      if (e.code === 'KeyP') {
        useGameStore.getState().cyclePixel()
      }
      // T：时间快进 12 小时；0/1/2/3：时间速度（暂停/1×/6×/30×）；[ ]：±1 小时
      if (e.code === 'KeyT' && !e.repeat) {
        useGameStore.getState().advanceTime(720)
      }
      if (!e.repeat) {
        if (e.code === 'Digit0') useGameStore.getState().setSpeed(0)
        if (e.code === 'Digit1') useGameStore.getState().setSpeed(1)
        if (e.code === 'Digit2') useGameStore.getState().setSpeed(6)
        if (e.code === 'Digit3') useGameStore.getState().setSpeed(30)
        if (e.code === 'BracketLeft') useGameStore.getState().advanceTime(-60)
        if (e.code === 'BracketRight') useGameStore.getState().advanceTime(60)
      }
      // Q / E：每次旋转 90°（带平滑动画）；旋转中角色保持"背对镜头"，不露出其他面
      if (e.code === 'KeyQ' && !e.repeat) {
        const v = view.current
        v.yawTarget = (v.yawTarget ?? v.yaw) + Math.PI / 2
        alignPaused.current = false
      }
      if (e.code === 'KeyE' && !e.repeat) {
        const v = view.current
        v.yawTarget = (v.yawTarget ?? v.yaw) - Math.PI / 2
        alignPaused.current = false
      }
    }
    const up = (e) => {
      keys.current[e.code] = false
    }
    window.addEventListener('keydown', down)
    window.addEventListener('keyup', up)
    return () => {
      window.removeEventListener('keydown', down)
      window.removeEventListener('keyup', up)
    }
  }, [])

  // 鼠标拖拽旋转视角 + 滚轮缩放
  useEffect(() => {
    const canvas = gl.domElement
    const onDown = (e) => {
      drag.current = { x: e.clientX, y: e.clientY }
      alignPaused.current = true // 鼠标拖拽时冻结朝向：拖动相机可以看到角色的各个面
      try {
        canvas.setPointerCapture(e.pointerId)
      } catch {
        /* 忽略 */
      }
      canvas.style.cursor = 'grabbing'
    }
    const onMove = (e) => {
      if (!drag.current) return
      const dx = e.clientX - drag.current.x
      const dy = e.clientY - drag.current.y
      drag.current = { x: e.clientX, y: e.clientY }
      view.current.yaw -= dx * 0.005
      view.current.pitch = THREE.MathUtils.clamp(view.current.pitch + dy * 0.004, 0.3, 1.2)
    }
    const onUp = () => {
      drag.current = null
      canvas.style.cursor = 'grab'
    }
    const onWheel = (e) => {
      e.preventDefault()
      view.current.dist = THREE.MathUtils.clamp(view.current.dist + e.deltaY * 0.01, 6, 22)
    }
    canvas.style.cursor = 'grab'
    canvas.addEventListener('pointerdown', onDown)
    window.addEventListener('pointermove', onMove)
    window.addEventListener('pointerup', onUp)
    canvas.addEventListener('wheel', onWheel, { passive: false })
    return () => {
      canvas.removeEventListener('pointerdown', onDown)
      window.removeEventListener('pointermove', onMove)
      window.removeEventListener('pointerup', onUp)
      canvas.removeEventListener('wheel', onWheel)
    }
  }, [gl])

  const grass = useMemo(() => grassTexture(60), [])
  const pathV = useMemo(() => pathTexture(false), [])
  const pathH = useMemo(() => pathTexture(true), [])
  const flowers = useMemo(() => flowerTexture(), [])
  const track = useMemo(() => trackTexture(), [])
  const pondTex = useMemo(() => pondTexture(), [])
  const spriteTex = useMemo(() => characterSheet(), [])
  const doorTex = useMemo(() => doorTexture(), [])
  const awningTex = useMemo(() => stripesTexture(), [])
  const branchTex = useMemo(() => BRANCH_PATHS.map((p) => pathTexture(false, Math.max(1, Math.round(p.len / 2.6)))), [])
  const dirtTex = useMemo(() => dirtTexture(), [])
  // 路灯共享材质（玻璃颜色与地面光池随昼夜混合值变化）
  const lampGlass = useMemo(() => new THREE.MeshBasicMaterial({ color: '#d5dbe4' }), [])
  const lampPool = useMemo(
    () => new THREE.MeshBasicMaterial({ map: lampGlowTexture(), transparent: true, opacity: 0, depthWrite: false, blending: THREE.AdditiveBlending }),
    []
  )
  // 盒子世界围墙材质（手绘像素风景；无雾、不受灯光，颜色随昼夜染色）+ 夜空叠加层
  const wallMat = useMemo(() => new THREE.MeshBasicMaterial({ map: wallTexture(), fog: false }), [])
  const starMat = useMemo(
    () => new THREE.MeshBasicMaterial({ map: starOverlayTexture(), transparent: true, opacity: 0, fog: false, depthWrite: false, blending: THREE.AdditiveBlending }),
    []
  )

  useFrame((state, delta) => {
    const p = player.current
    if (!p) return
    const dt = Math.min(delta, 0.05)
    const k = keys.current

    // Q / E：以 90° 步进平滑旋转到目标角度
    const vt = view.current
    if (vt.yawTarget != null) {
      vt.yaw += (vt.yawTarget - vt.yaw) * Math.min(1, dt * 7)
      if (Math.abs(vt.yawTarget - vt.yaw) < 0.01) {
        vt.yaw = vt.yawTarget
        vt.yawTarget = null
      }
    }

    const f = (k.KeyW || k.ArrowUp ? 1 : 0) - (k.KeyS || k.ArrowDown ? 1 : 0)
    const r = (k.KeyD || k.ArrowRight ? 1 : 0) - (k.KeyA || k.ArrowLeft ? 1 : 0)
    const yaw = view.current.yaw
    const moving = f !== 0 || r !== 0
    let dirX = 0
    let dirZ = 0

    if (moving) {
      dirX = -Math.sin(yaw) * f + Math.cos(yaw) * r
      dirZ = -Math.cos(yaw) * f - Math.sin(yaw) * r
      const len = Math.hypot(dirX, dirZ)
      dirX /= len
      dirZ /= len
      const nx = THREE.MathUtils.clamp(p.position.x + dirX * MOVE_SPEED * dt, -BOUND, BOUND)
      if (!collides(nx, p.position.z)) p.position.x = nx
      const nz = THREE.MathUtils.clamp(p.position.z + dirZ * MOVE_SPEED * dt, -BOUND, BOUND)
      if (!collides(p.position.x, nz)) p.position.z = nz
      p.position.y = Math.abs(Math.sin(state.clock.elapsedTime * 10)) * 0.05
    } else {
      p.position.y *= 0.8
    }

    if (shadow.current) {
      shadow.current.position.x = p.position.x
      shadow.current.position.z = p.position.z
    }

    // —— 像素小人：移动时朝移动方向（能看到侧面/正面）；
    //    站立时平滑转回"背对镜头"；鼠标拖拽冻结朝向（可细看各个面）；Q/E 转视角后自动回背对 ——
    const camPos = state.camera.position
    const toCam = Math.atan2(camPos.x - p.position.x, camPos.z - p.position.z)
    if (moving) {
      alignPaused.current = false
      let turn = Math.atan2(dirX, dirZ) - facing.current
      turn = Math.atan2(Math.sin(turn), Math.cos(turn))
      facing.current += turn * Math.min(1, dt * 10)
    } else if (!alignPaused.current) {
      let alien = toCam + Math.PI - facing.current
      alien = Math.atan2(Math.sin(alien), Math.cos(alien))
      facing.current += alien * Math.min(1, dt * 4)
    }
    if (sprite.current) {
      sprite.current.rotation.y = toCam // 平面面向相机，朝向由贴图行体现
      let d = facing.current - (toCam + Math.PI)
      d = Math.atan2(Math.sin(d), Math.cos(d))
      let row
      if (Math.abs(d) < Math.PI / 4) row = 1 // 背面朝镜头
      else if (Math.abs(d) > (3 * Math.PI) / 4) row = 0 // 正面
      else row = d < 0 ? 3 : 2 // 3=右侧面 2=左侧面
      const frame = moving ? 1 + (Math.floor(state.clock.elapsedTime * 8) % 2) : 0
      spriteTex.offset.set(frame / 3, (3 - row) / 4)
    }

    // 交互检测（建筑正门 / 操场 / 池塘）
    let near = null
    for (const s of SPOTS) {
      if (Math.hypot(p.position.x - s.x, p.position.z - s.z) < s.r) {
        near = s.name
        break
      }
    }
    useGameStore.getState().setNear(near)

    // 跟随相机（球坐标；首帧直接吸附到目标位，避免开场漂移/截图定格在飞行中途）
    const { yaw: vy, pitch, dist } = view.current
    const cx = p.position.x + Math.sin(vy) * Math.cos(pitch) * dist
    const cz = p.position.z + Math.cos(vy) * Math.cos(pitch) * dist
    const cy = p.position.y + Math.sin(pitch) * dist
    const cam = state.camera
    if (!camInit.current) {
      cam.position.set(cx, cy, cz)
      camInit.current = true
    } else {
      const t = Math.min(1, dt * 5)
      cam.position.x += (cx - cam.position.x) * t
      cam.position.y += (cy - cam.position.y) * t
      cam.position.z += (cz - cam.position.z) * t
    }
    cam.lookAt(p.position.x, 1.1, p.position.z)
  })

  const ground = GROUNDS[0]

  // 时间推进 + 昼夜过渡：逐帧按游戏时钟计算灯光/雾（含晨昏暖色）
  useFrame((_, dt) => {
    useGameStore.getState().tick(dt)
    const h = useGameStore.getState().gameMinutes / 60
    const m = nightMixAt(h)
    const warm = warmAt(h) * (1 - m * 0.65)
    mixRef.current = m
    if (ambRef.current) ambRef.current.intensity = THREE.MathUtils.lerp(FX_DAY.amb, FX_NIGHT.amb, m)
    if (hemiRef.current) {
      hemiRef.current.intensity = THREE.MathUtils.lerp(FX_DAY.hemi, FX_NIGHT.hemi, m)
      hemiRef.current.color.lerpColors(FX_DAY.hemiSky, FX_NIGHT.hemiSky, m)
      hemiRef.current.groundColor.lerpColors(FX_DAY.hemiGnd, FX_NIGHT.hemiGnd, m)
    }
    if (dirRef.current) {
      dirRef.current.intensity = THREE.MathUtils.lerp(FX_DAY.dir, FX_NIGHT.dir, m)
      dirRef.current.color.lerpColors(FX_DAY.dirCol, FX_NIGHT.dirCol, m).lerp(WARM_DIR, warm * 0.75)
    }
    if (fogRef.current) {
      fogRef.current.color.lerpColors(FX_DAY.fog, FX_NIGHT.fog, m).lerp(WARM_FOG, warm * 0.45)
      fogRef.current.near = THREE.MathUtils.lerp(FX_DAY.fogNear, FX_NIGHT.fogNear, m)
      fogRef.current.far = THREE.MathUtils.lerp(FX_DAY.fogFar, FX_NIGHT.fogFar, m)
    }
    // 路灯：黄昏起渐亮（灯头玻璃变暖 + 地面光池淡入）
    lampGlass.color.lerpColors(LAMP_GLASS_OFF, LAMP_GLASS_ON, m)
    lampPool.opacity = m * 0.68
    // 围墙：昼夜染色（白天原色，夜晚压暗偏蓝，晨昏微暖）+ 夜空叠加层淡入
    wallMat.color.lerpColors(WHITE, WALL_NIGHT, m).lerp(WARM_FOG, warm * 0.25)
    starMat.opacity = m * 0.9
  })

  return (
    <>
      <ambientLight ref={ambRef} intensity={0.82} />
      <hemisphereLight ref={hemiRef} args={['#d8ecff', '#3f5a36', 0.46]} />
      <directionalLight
        ref={dirRef}
        position={[14, 20, 10]}
        intensity={1.42}
        castShadow
        shadow-mapSize-width={2048}
        shadow-mapSize-height={2048}
        shadow-camera-left={-48}
        shadow-camera-right={48}
        shadow-camera-top={48}
        shadow-camera-bottom={-48}
        shadow-camera-near={1}
        shadow-camera-far={90}
        shadow-bias={-0.0004}
        shadow-normalBias={0.5}
      />
      <fog ref={fogRef} attach="fog" args={['#cfe3f4', 46, 110]} />
      <SkyDome mix={mixRef} initialNight={initialNight} />

      {/* 草地与十字道路（草地 200×200 延伸到雾中，地平线无生硬接缝） */}
      <mesh rotation={[-Math.PI / 2, 0, 0]}>
        <planeGeometry args={[200, 200]} />
        <meshLambertMaterial map={grass} />
      </mesh>
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.012, 8.75]}>
        <planeGeometry args={[4.6, 54.5]} />
        <meshLambertMaterial map={pathV} />
      </mesh>
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.016, 0]}>
        <planeGeometry args={[72, 4.6]} />
        <meshLambertMaterial map={pathH} />
      </mesh>

      {/* 花丛 */}
      {FLOWERS.map(([x, z], i) => (
        <mesh key={i} rotation={[-Math.PI / 2, 0, 0]} position={[x, 0.012, z]}>
          <planeGeometry args={[1.7, 1.7]} />
          <meshLambertMaterial map={flowers} />
        </mesh>
      ))}

      {/* 土斑 */}
      {DIRT_PATCHES.map(([x, z], i) => (
        <mesh key={'dp' + i} rotation={[-Math.PI / 2, 0, 0]} position={[x, 0.01, z]}>
          <circleGeometry args={[2.4, 16]} />
          <meshLambertMaterial map={dirtTex} alphaTest={0.4} />
        </mesh>
      ))}

      {/* 岔路（通往各建筑） */}
      {BRANCH_PATHS.map((p, i) => (
        <mesh key={i} rotation={[-Math.PI / 2, 0, 0]} position={[p.x, 0.014, p.z]}>
          <planeGeometry args={[3, p.len]} />
          <meshLambertMaterial map={branchTex[i]} />
        </mesh>
      ))}

      {/* 栅栏（沿两条主路，缺口对齐路口与岔路） */}
      {FENCES.map((f, i) => (
        <FenceRun key={i} run={f} />
      ))}

      {/* 路灯（沿主路两侧，晚上随昼夜混合值自动点亮 + 地面光池） */}
      {LAMPS.map(([x, z, r], i) => (
        <StreetLamp key={'lp' + i} x={x} z={z} r={r} glassMat={lampGlass} poolMat={lampPool} />
      ))}

      {/* 操场 */}
      {[{ key: 'pg', x: ground.pos[0], z: ground.pos[1] }].map((g) => (
        <mesh key={g.key} rotation={[-Math.PI / 2, 0, 0]} position={[g.x, 0.02, g.z]}>
          <planeGeometry args={[ground.size[0], ground.size[1]]} />
          <meshLambertMaterial map={track} />
        </mesh>
      ))}

      {/* 池塘（像素贴图：石沿 + 波纹 + 睡莲） */}
      {[{ key: 'pnd', x: POND.pos[0], z: POND.pos[1] }].map((g) => (
        <group key={g.key} position={[g.x, 0, g.z]}>
          <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.012, 0]}>
            <circleGeometry args={[POND.r + 0.6, 40]} />
            <meshLambertMaterial map={pondTex} alphaTest={0.4} />
          </mesh>
          <mesh position={[POND.r - 0.5, 0.25, 0.9]}>
            <boxGeometry args={[0.9, 0.5, 0.7]} />
            <meshLambertMaterial color="#8d8d94" />
          </mesh>
          <mesh position={[-1.7, 0.2, POND.r - 0.4]}>
            <boxGeometry args={[0.7, 0.4, 0.6]} />
            <meshLambertMaterial color="#7a7a82" />
          </mesh>
        </group>
      ))}

      {/* 建筑 */}
      {BUILDINGS.map((b) => (
        <Building key={b.id} def={b} doorTex={doorTex} awningTex={awningTex} />
      ))}

      {/* 树（六种树冠配色：深浅绿 / 黄绿 / 樱花粉 / 金叶）*/}
      {TREES.map(([tx, tz, v], i) => (
        <Tree key={i} x={tx} z={tz} variant={v} />
      ))}

      {/* 边界环境：远景建筑环 + 假山群 + 边界林带（遮挡空旷校界） */}
      {DISTANT_BUILDINGS.map((b, i) => (
        <DistantBuilding key={'db' + i} b={b} />
      ))}
      {ROCKERIES.map(([x, z, s], i) => (
        <Rockery key={'rk' + i} x={x} z={z} s={s} />
      ))}
      {BORDER_TREES.map(([x, z, v], i) => (
        <Tree key={'bt' + i} x={x} z={z} variant={v} />
      ))}

      {/* 盒子世界围墙（四周高墙贴手绘像素风景：层叠山脉/森林/雾带 + 夜空叠加层） */}
      <BoxWalls mat={wallMat} starMat={starMat} />

      {/* 小石头（草地与环带点缀） */}
      {[...SMALL_ROCKS, ...BORDER_ROCKS].map(([x, z], i) => (
        <group key={'sr' + i} position={[x, 0, z]}>
          <mesh position={[0, 0.18 + (i % 2) * 0.1, 0]} rotation={[0, (i * 1.3) % 6.28, 0]}>
            <boxGeometry args={[0.5 + (i % 3) * 0.2, 0.36 + (i % 2) * 0.18, 0.45 + (i % 2) * 0.22]} />
            <meshLambertMaterial color={['#8d8d94', '#7b7b82', '#9aa0a6'][i % 3]} />
          </mesh>
        </group>
      ))}

      {/* 池塘芦苇（池沿细杆丛） */}
      {REEDS.map(([x, z, h, t], i) => (
        <group key={'rd' + i} position={[x, 0, z]}>
          <mesh position={[0, h / 2, 0]}>
            <boxGeometry args={[0.12, h, 0.12]} />
            <meshLambertMaterial color={['#7a8a4a', '#6a7a42', '#85925a'][t]} />
          </mesh>
          <mesh position={[0.16, (h * 0.8) / 2, 0.1]} rotation={[0, 0.6, 0.12]}>
            <boxGeometry args={[0.1, h * 0.8, 0.1]} />
            <meshLambertMaterial color={['#85925a', '#7a8a4a', '#6a7a42'][t]} />
          </mesh>
        </group>
      ))}

      {/* 灌木 */}
      {BUSHES.map(([x, z], i) => (
        <Bush key={i} x={x} z={z} i={i} />
      ))}

      {/* 边界灌木（校界外环带填充） */}
      {BORDER_BUSHES.map(([x, z], i) => (
        <Bush key={'bb' + i} x={x} z={z} i={i} />
      ))}

      {/* 角色影子 */}
      <mesh ref={shadow} rotation={[-Math.PI / 2, 0, 0]} position={[spawn.x, 0.03, spawn.z]}>
        <circleGeometry args={[0.42, 16]} />
        <meshBasicMaterial color="#000000" transparent opacity={0.22} />
      </mesh>

      {/* 角色：像素小人（樊楼·金发校服少年）*/}
      <group ref={player} position={[spawn.x, 0, spawn.z]}>
        <mesh ref={sprite} position={[0, 0.58, 0]}>
          <planeGeometry args={[1.12, 1.64]} />
          <meshBasicMaterial map={spriteTex} transparent alphaTest={0.4} side={THREE.DoubleSide} />
        </mesh>
      </group>
    </>
  )
}

const TREE_CROWNS = ['#3f7a3f', '#4f9448', '#5da04f', '#6fae4b', '#d98fb0', '#c9a24a']

// 远景建筑（校区外天际线：低细节剪影建筑 + 屋顶檐 + 基座压地线 + 高楼屋顶设备；夜里零星亮窗）
function DistantBuilding({ b }) {
  const tex = useMemo(
    () => buildingTexture(b.wall, b.trim, b.plinth, 3, Math.max(2, Math.round(b.h / 1.7)), { lit: 0.16 }),
    [b]
  )
  return (
    <group position={[b.x, b.h / 2, b.z]}>
      <mesh>
        <boxGeometry args={[b.w, b.h, b.d]} />
        <meshLambertMaterial map={tex} />
      </mesh>
      {/* 基座压地线（避免夜里"浮空"感） */}
      <mesh position={[0, -b.h / 2 + 0.25, 0]}>
        <boxGeometry args={[b.w + 0.5, 0.5, b.d + 0.5]} />
        <meshLambertMaterial color={b.plinth} />
      </mesh>
      <mesh position={[0, b.h / 2 + 0.2, 0]}>
        <boxGeometry args={[b.w + 0.8, 0.4, b.d + 0.8]} />
        <meshLambertMaterial color={b.roof} />
      </mesh>
      {b.h > 6.5 && (
        <mesh position={[b.w / 4, b.h / 2 + 0.95, 0]}>
          <boxGeometry args={[1.5, 0.9, 1.2]} />
          <meshLambertMaterial color="#9aa2ac" />
        </mesh>
      )}
    </group>
  )
}

// 灌木（校区内 + 边界环带共用）
function Bush({ x, z, i }) {
  return (
    <group position={[x, 0, z]}>
      <mesh position={[0, 0.24, 0]}>
        <boxGeometry args={[0.95, 0.5, 0.95]} />
        <meshLambertMaterial color={i % 2 ? '#3d7a3f' : '#356e37'} />
      </mesh>
      <mesh position={[0.12, 0.52, -0.08]}>
        <boxGeometry args={[0.55, 0.36, 0.55]} />
        <meshLambertMaterial color={i % 2 ? '#356e37' : '#2f6231'} />
      </mesh>
    </group>
  )
}

// 假山：灰岩堆叠（大小错落 + 轻转角度）+ 顶部/侧边灌木
function Rockery({ x, z, s }) {
  return (
    <group position={[x, 0, z]}>
      {[
        [0, 0.5, 0, 2.6, 1.0, 2.2, 0],
        [1.6, 0.4, 0.6, 1.6, 0.8, 1.4, 0.4],
        [-1.5, 0.35, -0.5, 1.4, 0.7, 1.2, -0.5],
        [0.4, 1.15, -0.3, 1.2, 0.6, 1.0, 0.9],
        [-0.6, 0.9, 0.5, 0.9, 0.5, 0.8, -0.8],
      ].map(([bx, by, bz, bw, bh, bd, ry], i) => (
        <mesh key={i} position={[bx * s, by * s, bz * s]} rotation={[0, ry, 0]}>
          <boxGeometry args={[bw * s, bh * s, bd * s]} />
          <meshLambertMaterial color={['#8d8d94', '#7b7b82', '#9aa0a6', '#6f6f76', '#84848b'][i % 5]} />
        </mesh>
      ))}
      <mesh position={[0.8 * s, 1.0 * s, 0.2 * s]}>
        <boxGeometry args={[0.9 * s, 0.7 * s, 0.9 * s]} />
        <meshLambertMaterial color="#3f7a3f" />
      </mesh>
      <mesh position={[-1.0 * s, 0.75 * s, -0.1 * s]}>
        <boxGeometry args={[0.7 * s, 0.55 * s, 0.7 * s]} />
        <meshLambertMaterial color="#356e37" />
      </mesh>
    </group>
  )
}

function Tree({ x, z, variant }) {
  if (variant === 6) {
    // 松树：三层锥形（植被多样性）
    return (
      <group position={[x, 0, z]}>
        <mesh position={[0, 0.4, 0]}>
          <boxGeometry args={[0.36, 0.8, 0.36]} />
          <meshLambertMaterial color="#6b4a2f" />
        </mesh>
        <mesh position={[0, 1.15, 0]}>
          <coneGeometry args={[1.05, 1.2, 6]} />
          <meshLambertMaterial color="#2f6231" flatShading />
        </mesh>
        <mesh position={[0, 1.85, 0]}>
          <coneGeometry args={[0.85, 1.1, 6]} />
          <meshLambertMaterial color="#356e37" flatShading />
        </mesh>
        <mesh position={[0, 2.5, 0]}>
          <coneGeometry args={[0.62, 1.0, 6]} />
          <meshLambertMaterial color="#3d7a3f" flatShading />
        </mesh>
      </group>
    )
  }
  if (variant === 7) {
    // 柏树：高瘦深绿
    return (
      <group position={[x, 0, z]}>
        <mesh position={[0, 0.3, 0]}>
          <boxGeometry args={[0.3, 0.6, 0.3]} />
          <meshLambertMaterial color="#5c4630" />
        </mesh>
        <mesh position={[0, 1.9, 0]}>
          <coneGeometry args={[0.55, 3.0, 6]} />
          <meshLambertMaterial color="#284f2b" flatShading />
        </mesh>
      </group>
    )
  }
  const tall = variant === 1
  const wide = variant === 2
  const trunkH = tall ? 1.5 : 1
  const crown = wide ? [2, 1.4, 2] : tall ? [1.4, 1.8, 1.4] : [1.5, 1.3, 1.5]
  return (
    <group position={[x, 0, z]}>
      <mesh position={[0, trunkH / 2, 0]}>
        <boxGeometry args={[0.36, trunkH, 0.36]} />
        <meshLambertMaterial color="#6d4a2f" />
      </mesh>
      <mesh position={[0, trunkH + crown[1] / 2 - 0.1, 0]}>
        <boxGeometry args={crown} />
        <meshLambertMaterial color={TREE_CROWNS[variant % TREE_CROWNS.length]} />
      </mesh>
    </group>
  )
}

// 盒子世界围墙（4 面，各取 8192 全景的 1/4 段；材质无雾不受灯光，颜色按昼夜染色）
// 高 34（顶约 20°仰角）；风景绘制在可见带（墙下部），夜空叠加层（starMat）夜晚淡入
const WALL_DIST = 82
const WALL_H = 34
const WALL_NIGHT = new THREE.Color('#3d4a66')
function BoxWalls({ mat, starMat }) {
  const geos = useMemo(
    () =>
      [0, 1, 2, 3].map((q) => {
        // 长度两侧各外扩 5（拐角互相交叠，杜绝角缝漏景）
        const g = new THREE.PlaneGeometry(WALL_DIST * 2 + 10, WALL_H)
        const uv = g.attributes.uv
        for (let i = 0; i < uv.count; i++) uv.setX(i, uv.getX(i) * 0.25 + q * 0.25)
        return g
      }),
    []
  )
  return (
    <>
      <mesh geometry={geos[0]} position={[0, WALL_H / 2 - 2, -WALL_DIST]} material={mat} />
      <mesh geometry={geos[1]} position={[0, WALL_H / 2 - 2, WALL_DIST]} rotation={[0, Math.PI, 0]} material={mat} />
      <mesh geometry={geos[2]} position={[-WALL_DIST, WALL_H / 2 - 2, 0]} rotation={[0, Math.PI / 2, 0]} material={mat} />
      <mesh geometry={geos[3]} position={[WALL_DIST, WALL_H / 2 - 2, 0]} rotation={[0, -Math.PI / 2, 0]} material={mat} />
      {/* 夜空叠加层（星星+月亮；夜晚淡入） */}
      <mesh geometry={geos[0]} position={[0, WALL_H / 2 - 2, -WALL_DIST + 0.6]} material={starMat} />
      <mesh geometry={geos[1]} position={[0, WALL_H / 2 - 2, WALL_DIST - 0.6]} rotation={[0, Math.PI, 0]} material={starMat} />
      <mesh geometry={geos[2]} position={[-WALL_DIST + 0.6, WALL_H / 2 - 2, 0]} rotation={[0, Math.PI / 2, 0]} material={starMat} />
      <mesh geometry={geos[3]} position={[WALL_DIST - 0.6, WALL_H / 2 - 2, 0]} rotation={[0, -Math.PI / 2, 0]} material={starMat} />
    </>
  )
}

function Building({ def, doorTex, awningTex }) {
  const [w, h, d] = def.size
  const [bx, bz] = def.pos
  // 两套窗户贴图：白天素净（极少亮灯+冷灰反光）/ 夜晚暖灯（亮窗率高），随昼夜自动切换
  const wallTexs = useMemo(
    () => [
      buildingTexture(def.wall, def.trim, def.plinth, def.cols, def.rows, { banner: def.banner, lit: 0.05 }),
      buildingTexture(def.wall, def.trim, def.plinth, def.cols, def.rows, { banner: def.banner, lit: 0.55 }),
    ],
    [def]
  )
  const wingTexs = useMemo(
    () => [
      buildingTexture(def.wall, def.trim, def.plinth, 3, 2, { lit: 0.05 }),
      buildingTexture(def.wall, def.trim, def.plinth, 3, 2, { lit: 0.55 }),
    ],
    [def]
  )
  const wallMat = useRef()
  const wingMat = useRef()
  useFrame(() => {
    const night = nightMixAt(useGameStore.getState().gameMinutes / 60) > 0.45
    for (const [r, texs] of [
      [wallMat, wallTexs],
      [wingMat, wingTexs],
    ]) {
      const mt = r.current
      if (mt && mt.userData.night !== night) {
        mt.userData.night = night
        mt.map = night ? texs[1] : texs[0]
        mt.needsUpdate = true
      }
    }
  })
  const signTex = useMemo(() => signTexture(def.name), [def.name])
  const rotationY = def.face === 1 ? 0 : Math.PI
  const extras = []

  if (def.style === 'gable') {
    // 双坡屋顶：两块斜板
    const angle = 0.45
    const slope = d / 2 / Math.cos(angle) + 0.4
    const y = h / 2 + 0.3 + (d / 4) * Math.tan(angle) * 0.3
    extras.push(
      <mesh key="gs1" position={[0, y, d / 4 + 0.1]} rotation={[angle, 0, 0]}>
        <boxGeometry args={[w + 1, 0.24, slope]} />
        <meshLambertMaterial color={def.roof} />
      </mesh>,
      <mesh key="gs2" position={[0, y, -d / 4 - 0.1]} rotation={[-angle, 0, 0]}>
        <boxGeometry args={[w + 1, 0.24, slope]} />
        <meshLambertMaterial color={def.roof} />
      </mesh>,
      <mesh key="ridge" position={[0, y + (d / 4) * Math.tan(angle) * 0.65, 0]}>
        <boxGeometry args={[w + 1.1, 0.2, 0.4]} />
        <meshLambertMaterial color="#33363f" />
      </mesh>
    )
  } else if (def.style === 'grand') {
    extras.push(
      <mesh key="plinth" position={[0, -h / 2 + 0.25, 0]}>
        <boxGeometry args={[w + 1.8, 0.5, d + 1.8]} />
        <meshLambertMaterial color={def.plinth} />
      </mesh>,
      <mesh key="p1" position={[-(w / 2 - 0.9), -h / 2 + h * 0.42 + 0.5, d / 2 + 0.55]}>
        <boxGeometry args={[0.5, h * 0.84, 0.5]} />
        <meshLambertMaterial color={def.trim} />
      </mesh>,
      <mesh key="p2" position={[w / 2 - 0.9, -h / 2 + h * 0.42 + 0.5, d / 2 + 0.55]}>
        <boxGeometry args={[0.5, h * 0.84, 0.5]} />
        <meshLambertMaterial color={def.trim} />
      </mesh>,
      <mesh key="top" position={[0, h / 2 + 0.18, 0]}>
        <boxGeometry args={[w + 1.2, 0.32, d + 1.2]} />
        <meshLambertMaterial color={def.roof} />
      </mesh>,
      <mesh key="tier" position={[0, h / 2 + 0.72, 0]}>
        <boxGeometry args={[w * 0.55, 0.8, d * 0.55]} />
        <meshLambertMaterial color={def.roof} />
      </mesh>
    )
  } else {
    extras.push(
      <mesh key="roof" position={[0, h / 2 + 0.16, 0]}>
        <boxGeometry args={[w + 0.7, 0.32, d + 0.7]} />
        <meshLambertMaterial color={def.roof} />
      </mesh>,
      // 女儿墙（平屋顶四周矮墙）
      <mesh key="parapet" position={[0, h / 2 + 0.42, 0]}>
        <boxGeometry args={[w + 0.82, 0.22, d + 0.82]} />
        <meshLambertMaterial color={def.plinth} />
      </mesh>
    )
  }

  // 屋顶设备（空调机组 + 楼梯间 + 排气管）
  if (def.roofProps) {
    extras.push(
      <group key="rp" position={[0, h / 2 + 0.6, 0]}>
        <mesh position={[-w * 0.22, 0, -d * 0.12]}>
          <boxGeometry args={[1.7, 0.75, 1.2]} />
          <meshLambertMaterial color="#a8adb5" />
        </mesh>
        <mesh position={[w * 0.24, 0.03, d * 0.1]}>
          <boxGeometry args={[1, 0.55, 0.8]} />
          <meshLambertMaterial color="#9aa2ac" />
        </mesh>
        <mesh position={[w * 0.24, -0.28, d * 0.1]}>
          <boxGeometry args={[1.1, 0.16, 0.9]} />
          <meshLambertMaterial color="#7d838c" />
        </mesh>
      </group>
    )
  }
  if (def.vent) {
    extras.push(
      <group key="vent" position={[w * 0.28, h / 2 + 1.05, 0]}>
        <mesh>
          <boxGeometry args={[0.28, 2.0, 0.28]} />
          <meshLambertMaterial color="#8a9099" />
        </mesh>
        <mesh position={[0, 1.05, 0]}>
          <boxGeometry args={[0.62, 0.22, 0.62]} />
          <meshLambertMaterial color="#6f757e" />
        </mesh>
      </group>
    )
  }

  if (def.wing) {
    const ww = 4.6
    const wh = h * 0.72
    extras.push(
      <group key="wing" position={[w / 2 + ww / 2, 0, 0.6]}>
        <mesh>
          <boxGeometry args={[ww, wh, d * 0.8]} />
          <meshLambertMaterial ref={wingMat} map={wingTexs[0]} />
        </mesh>
        <mesh position={[0, wh / 2 + 0.14, 0]}>
          <boxGeometry args={[ww + 0.5, 0.28, d * 0.8 + 0.5]} />
          <meshLambertMaterial color={def.roof} />
        </mesh>
      </group>
    )
  }
  if (def.antenna) {
    extras.push(
      <group key="ant" position={[-w / 4, h / 2 + 0.9, 0]}>
        <mesh>
          <boxGeometry args={[0.14, 1.7, 0.14]} />
          <meshLambertMaterial color="#9aa2ac" />
        </mesh>
        <mesh position={[0, 0.95, 0]}>
          <boxGeometry args={[0.3, 0.24, 0.3]} />
          <meshLambertMaterial color="#c9553f" />
        </mesh>
      </group>
    )
  }
  if (def.tank) {
    extras.push(
      <mesh key="tank" position={[w / 4, h / 2 + 0.75, 0]}>
        <boxGeometry args={[1.7, 0.95, 1.3]} />
        <meshLambertMaterial color="#9aa2ac" />
      </mesh>
    )
  }
  if (def.awning) {
    extras.push(
      <mesh key="awn" position={[0, -h / 2 + 2.3, d / 2 + 0.55]}>
        <boxGeometry args={[w * 0.62, 0.24, 1.3]} />
        <meshLambertMaterial map={awningTex} />
      </mesh>
    )
  }

  return (
    <group position={[bx, h / 2, bz]} rotation={[0, rotationY, 0]}>
      <mesh>
        <boxGeometry args={[w, h, d]} />
        <meshLambertMaterial ref={wallMat} map={wallTexs[0]} />
      </mesh>
      {extras}
      <mesh position={[0, h * 0.28, d / 2 + 0.04]}>
        <planeGeometry args={[3.3, 0.83]} />
        <meshLambertMaterial map={signTex} />
      </mesh>
      {/* 大门（门框 + 气窗 + 台阶 + 门灯） */}
      <mesh position={[0, -h / 2 + 1.08, d / 2 + 0.035]}>
        <planeGeometry args={[1.5, 2.1]} />
        <meshLambertMaterial map={doorTex} />
      </mesh>
      <mesh position={[0, -h / 2 + 0.08, d / 2 + 0.5]}>
        <boxGeometry args={[1.9, 0.16, 0.9]} />
        <meshLambertMaterial color="#b8b0a2" />
      </mesh>
      <mesh position={[-1.05, -h / 2 + 2.32, d / 2 + 0.06]}>
        <boxGeometry args={[0.2, 0.28, 0.14]} />
        <meshBasicMaterial color="#ffdf9a" />
      </mesh>
      <mesh position={[1.05, -h / 2 + 2.32, d / 2 + 0.06]}>
        <boxGeometry args={[0.2, 0.28, 0.14]} />
        <meshBasicMaterial color="#ffdf9a" />
      </mesh>
      {/* 雨落管（正面两侧竖管） */}
      <mesh position={[-(w / 2 - 0.35), 0, d / 2 + 0.08]}>
        <boxGeometry args={[0.16, h - 0.5, 0.16]} />
        <meshLambertMaterial color={def.plinth} />
      </mesh>
      <mesh position={[w / 2 - 0.35, 0, d / 2 + 0.08]}>
        <boxGeometry args={[0.16, h - 0.5, 0.16]} />
        <meshLambertMaterial color={def.plinth} />
      </mesh>
      {/* 门前绿篱（两侧花坛） */}
      <mesh position={[-1.95, -h / 2 + 0.32, d / 2 + 0.55]}>
        <boxGeometry args={[1.3, 0.62, 0.55]} />
        <meshLambertMaterial color="#3d7a37" />
      </mesh>
      <mesh position={[1.95, -h / 2 + 0.32, d / 2 + 0.55]}>
        <boxGeometry args={[1.3, 0.62, 0.55]} />
        <meshLambertMaterial color="#3d7a37" />
      </mesh>
    </group>
  )
}

// 路灯：灯柱 + 灯臂 + 灯头（玻璃随昼夜变色）+ 地面光池（晚上亮起）
function StreetLamp({ x, z, r, glassMat, poolMat }) {
  return (
    <group position={[x, 0, z]} rotation={[0, r, 0]}>
      <mesh position={[0, 0.16, 0]}>
        <boxGeometry args={[0.42, 0.32, 0.42]} />
        <meshLambertMaterial color="#4a4f58" />
      </mesh>
      <mesh position={[0, 1.85, 0]}>
        <boxGeometry args={[0.16, 3.4, 0.16]} />
        <meshLambertMaterial color="#3a3f4a" />
      </mesh>
      <mesh position={[0.42, 3.48, 0]}>
        <boxGeometry args={[0.9, 0.12, 0.12]} />
        <meshLambertMaterial color="#3a3f4a" />
      </mesh>
      <mesh position={[0.8, 3.36, 0]}>
        <boxGeometry args={[0.46, 0.16, 0.36]} />
        <meshLambertMaterial color="#2f333c" />
      </mesh>
      <mesh position={[0.8, 3.26, 0]} material={glassMat}>
        <boxGeometry args={[0.38, 0.1, 0.3]} />
      </mesh>
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0.78, 0.02, 0]} material={poolMat}>
        <circleGeometry args={[2.1, 20]} />
      </mesh>
    </group>
  )
}

function FenceRun({ run }) {
  const { axis, at, from, to } = run
  const len = to - from
  const count = Math.max(1, Math.floor(len / 2.8))
  const posts = []
  for (let i = 0; i <= count; i++) posts.push(from + (len * i) / count)
  const mid = (from + to) / 2
  return (
    <group>
      {posts.map((t, i) => (
        <mesh key={i} position={axis === 'z' ? [at, 0.48, t] : [t, 0.48, at]}>
          <boxGeometry args={[0.14, 0.96, 0.14]} />
          <meshLambertMaterial color="#cfc9ba" />
        </mesh>
      ))}
      {[0.52, 0.86].map((y, i) => (
        <mesh key={'r' + i} position={axis === 'z' ? [at, y, mid] : [mid, y, at]}>
          <boxGeometry args={axis === 'z' ? [0.09, 0.09, len] : [len, 0.09, 0.09]} />
          <meshLambertMaterial color="#d8d3c6" />
        </mesh>
      ))}
    </group>
  )
}
