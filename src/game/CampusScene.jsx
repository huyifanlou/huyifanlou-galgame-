import { useEffect, useMemo, useRef } from 'react'
import { useFrame, useThree } from '@react-three/fiber'
import * as THREE from 'three'
import { BUILDINGS, TREES, BUSHES, FLOWERS, GROUNDS, POND, SPOTS } from './campus.js'
import {
  grassTexture, pathTexture, buildingTexture, signTexture,
  flowerTexture, trackTexture, characterSheet,
} from './textures.js'
import { useGameStore } from './store.js'

const MOVE_SPEED = 6
const BOUND = 30

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
      return { yaw, pitch: Math.min(1.35, Math.max(0.3, num('pitch', 0.6))), dist: Math.min(40, Math.max(6, num('dist', 13))), spawn }
    } catch {
      return { yaw: 0, pitch: 0.6, dist: 13, spawn: { x: 0, z: 4 } }
    }
  })()
  const view = useRef(initView)
  const spawn = initView.spawn
  const gl = useThree((s) => s.gl)

  useEffect(() => {
    const down = (e) => {
      keys.current[e.code] = true
      if (e.code === 'KeyE') {
        const st = useGameStore.getState()
        if (st.near) st.showToast(`「${st.near}」内部将在后续迭代开放`)
      }
      if (e.code === 'KeyP') {
        useGameStore.getState().cyclePixel()
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

  const grass = useMemo(() => grassTexture(), [])
  const pathV = useMemo(() => pathTexture(false), [])
  const pathH = useMemo(() => pathTexture(true), [])
  const flowers = useMemo(() => flowerTexture(), [])
  const track = useMemo(() => trackTexture(), [])
  const spriteTex = useMemo(() => characterSheet(), [])

  useFrame((state, delta) => {
    const p = player.current
    if (!p) return
    const dt = Math.min(delta, 0.05)
    const k = keys.current
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

    // —— 像素小人：朝向 + 走路动画 + 面向相机 ——
    if (sprite.current) {
      let row = 0
      let frame = 0
      if (moving) {
        const mx = dirX * Math.cos(yaw) - dirZ * Math.sin(yaw)
        const mz = -dirX * Math.sin(yaw) - dirZ * Math.cos(yaw)
        if (Math.abs(mx) > Math.abs(mz)) row = mx > 0 ? 3 : 2
        else row = mz > 0 ? 1 : 0
        frame = 1 + (Math.floor(state.clock.elapsedTime * 8) % 2)
      }
      spriteTex.offset.set(frame / 3, (3 - row) / 4)
      const cam = state.camera
      sprite.current.rotation.y = Math.atan2(cam.position.x - p.position.x, cam.position.z - p.position.z)
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

  return (
    <>
      <ambientLight intensity={0.95} />
      <hemisphereLight args={['#d8ecff', '#3f5a36', 0.5]} />
      <directionalLight position={[14, 20, 10]} intensity={1.4} />
      <fog attach="fog" args={['#10141c', 36, 90]} />

      {/* 草地与十字道路 */}
      <mesh rotation={[-Math.PI / 2, 0, 0]}>
        <planeGeometry args={[100, 100]} />
        <meshLambertMaterial map={grass} />
      </mesh>
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.012, 0]}>
        <planeGeometry args={[4.6, 72]} />
        <meshLambertMaterial map={pathV} />
      </mesh>
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.016, 0]}>
        <planeGeometry args={[72, 4.6]} />
        <meshLambertMaterial map={pathH} />
      </mesh>

      {/* 花丛 */}
      {FLOWERS.map(([x, z], i) => (
        <mesh key={i} rotation={[-Math.PI / 2, 0, 0]} position={[x, 0.008, z]}>
          <planeGeometry args={[1.7, 1.7]} />
          <meshLambertMaterial map={flowers} />
        </mesh>
      ))}

      {/* 操场 */}
      {[{ key: 'pg', x: ground.pos[0], z: ground.pos[1] }].map((g) => (
        <mesh key={g.key} rotation={[-Math.PI / 2, 0, 0]} position={[g.x, 0.02, g.z]}>
          <planeGeometry args={[ground.size[0], ground.size[1]]} />
          <meshLambertMaterial map={track} />
        </mesh>
      ))}

      {/* 池塘 */}
      {[{ key: 'pnd', x: POND.pos[0], z: POND.pos[1] }].map((g) => (
        <group key={g.key} position={[g.x, 0, g.z]}>
          <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.008, 0]}>
            <circleGeometry args={[POND.r + 0.6, 26]} />
            <meshLambertMaterial color="#7d7a5a" />
          </mesh>
          <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.012, 0]}>
            <circleGeometry args={[POND.r, 24]} />
            <meshLambertMaterial color="#3e6ea6" />
          </mesh>
          <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0.6, 0.016, -0.4]}>
            <circleGeometry args={[POND.r * 0.5, 18]} />
            <meshLambertMaterial color="#4d82bd" />
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
        <Building key={b.id} def={b} />
      ))}

      {/* 树（三种形态）*/}
      {TREES.map(([tx, tz, v], i) => (
        <Tree key={i} x={tx} z={tz} variant={v} idx={i} />
      ))}

      {/* 灌木 */}
      {BUSHES.map(([x, z], i) => (
        <group key={i} position={[x, 0, z]}>
          <mesh position={[0, 0.24, 0]}>
            <boxGeometry args={[0.95, 0.5, 0.95]} />
            <meshLambertMaterial color={i % 2 ? '#3d7a3f' : '#356e37'} />
          </mesh>
          <mesh position={[0.12, 0.52, -0.08]}>
            <boxGeometry args={[0.55, 0.36, 0.55]} />
            <meshLambertMaterial color={i % 2 ? '#356e37' : '#2f6231'} />
          </mesh>
        </group>
      ))}

      {/* 角色影子 */}
      <mesh ref={shadow} rotation={[-Math.PI / 2, 0, 0]} position={[spawn.x, 0.03, spawn.z]}>
        <circleGeometry args={[0.42, 16]} />
        <meshBasicMaterial color="#000000" transparent opacity={0.22} />
      </mesh>

      {/* 角色：像素小人（樊楼·金发校服少年）*/}
      <group ref={player} position={[spawn.x, 0, spawn.z]}>
        <mesh ref={sprite} position={[0, 0.5, 0]}>
          <planeGeometry args={[1.15, 1.53]} />
          <meshBasicMaterial map={spriteTex} transparent alphaTest={0.4} side={THREE.DoubleSide} />
        </mesh>
      </group>
    </>
  )
}

function Tree({ x, z, variant, idx }) {
  const tall = variant === 1
  const wide = variant === 2
  const trunkH = tall ? 1.5 : 1
  const crown = wide ? [2, 1.4, 2] : tall ? [1.4, 1.8, 1.4] : [1.5, 1.3, 1.5]
  const greens = ['#3f7a3f', '#468a48', '#3a713c']
  return (
    <group position={[x, 0, z]}>
      <mesh position={[0, trunkH / 2, 0]}>
        <boxGeometry args={[0.36, trunkH, 0.36]} />
        <meshLambertMaterial color="#6d4a2f" />
      </mesh>
      <mesh position={[0, trunkH + crown[1] / 2 - 0.1, 0]}>
        <boxGeometry args={crown} />
        <meshLambertMaterial color={greens[(idx + variant) % 3]} />
      </mesh>
    </group>
  )
}

function Building({ def }) {
  const [w, h, d] = def.size
  const [bx, bz] = def.pos
  const wallTex = useMemo(() => buildingTexture(def.wall, def.cols, def.rows), [def])
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
        <meshLambertMaterial color="#7a6a58" />
      </mesh>,
      <mesh key="p1" position={[-(w / 2 - 0.9), -h / 2 + h * 0.42 + 0.5, d / 2 + 0.55]}>
        <boxGeometry args={[0.5, h * 0.84, 0.5]} />
        <meshLambertMaterial color="#c9bda6" />
      </mesh>,
      <mesh key="p2" position={[w / 2 - 0.9, -h / 2 + h * 0.42 + 0.5, d / 2 + 0.55]}>
        <boxGeometry args={[0.5, h * 0.84, 0.5]} />
        <meshLambertMaterial color="#c9bda6" />
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
      </mesh>
    )
  }

  if (def.wing) {
    const ww = 4.6
    const wh = h * 0.72
    extras.push(
      <group key="wing" position={[w / 2 + ww / 2, 0, 0.6]}>
        <mesh>
          <boxGeometry args={[ww, wh, d * 0.8]} />
          <meshLambertMaterial map={buildingTexture(def.wall, 3, 2)} />
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
        <meshLambertMaterial color="#c9553f" />
      </mesh>
    )
  }

  return (
    <group position={[bx, h / 2, bz]} rotation={[0, rotationY, 0]}>
      <mesh>
        <boxGeometry args={[w, h, d]} />
        <meshLambertMaterial map={wallTex} />
      </mesh>
      {extras}
      <mesh position={[0, h * 0.28, d / 2 + 0.04]}>
        <planeGeometry args={[3.3, 0.83]} />
        <meshLambertMaterial map={signTex} />
      </mesh>
      <mesh position={[0, -h / 2 + 0.85, d / 2 + 0.03]}>
        <planeGeometry args={[1.2, 1.7]} />
        <meshLambertMaterial color="#2b2733" />
      </mesh>
    </group>
  )
}
