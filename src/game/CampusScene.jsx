import { useEffect, useMemo, useRef } from 'react'
import { useFrame, useThree } from '@react-three/fiber'
import * as THREE from 'three'
import { BUILDINGS, TREES } from './campus.js'
import { grassTexture, pathTexture, buildingTexture, signTexture } from './textures.js'
import { useGameStore } from './store.js'

const MOVE_SPEED = 6
const BOUND = 30

// 与建筑 AABB 碰撞检测（膨胀 0.45）
function collides(x, z) {
  for (const b of BUILDINGS) {
    const [bx, bz] = b.pos
    const [w, , d] = b.size
    if (Math.abs(x - bx) < w / 2 + 0.45 && Math.abs(z - bz) < d / 2 + 0.45) return true
  }
  return false
}

// 迭代 1（含体验修正）：正式像素校园 + 拖拽旋转视角 + 滚轮缩放 + 画质档位
export default function CampusScene() {
  const player = useRef()
  const shadow = useRef()
  const keys = useRef({})
  const drag = useRef(null)
  const view = useRef({ yaw: 0, pitch: 0.6, dist: 13 })
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

  useFrame((state, delta) => {
    const p = player.current
    if (!p) return
    const dt = Math.min(delta, 0.05)
    const k = keys.current
    const f = (k.KeyW || k.ArrowUp ? 1 : 0) - (k.KeyS || k.ArrowDown ? 1 : 0)
    const r = (k.KeyD || k.ArrowRight ? 1 : 0) - (k.KeyA || k.ArrowLeft ? 1 : 0)
    const yaw = view.current.yaw

    if (f !== 0 || r !== 0) {
      // 移动方向随视角（W = 屏幕深处方向）
      let dirX = -Math.sin(yaw) * f + Math.cos(yaw) * r
      let dirZ = -Math.cos(yaw) * f - Math.sin(yaw) * r
      const len = Math.hypot(dirX, dirZ)
      dirX /= len
      dirZ /= len
      const nx = THREE.MathUtils.clamp(p.position.x + dirX * MOVE_SPEED * dt, -BOUND, BOUND)
      if (!collides(nx, p.position.z)) p.position.x = nx
      const nz = THREE.MathUtils.clamp(p.position.z + dirZ * MOVE_SPEED * dt, -BOUND, BOUND)
      if (!collides(p.position.x, nz)) p.position.z = nz
      let diff = Math.atan2(dirX, dirZ) - p.rotation.y
      diff = Math.atan2(Math.sin(diff), Math.cos(diff))
      p.rotation.y += diff * Math.min(1, dt * 12)
      p.position.y = Math.abs(Math.sin(state.clock.elapsedTime * 10)) * 0.06
    } else {
      p.position.y *= 0.8
    }

    if (shadow.current) {
      shadow.current.position.x = p.position.x
      shadow.current.position.z = p.position.z
    }

    // 交互检测：站在建筑正门前 2.4 范围内
    let near = null
    for (const b of BUILDINGS) {
      const [bx, bz] = b.pos
      const fz = bz + b.face * (b.size[2] / 2 + 0.9)
      if (Math.hypot(p.position.x - bx, p.position.z - fz) < 2.4) {
        near = b.name
        break
      }
    }
    useGameStore.getState().setNear(near)

    // 跟随相机（球坐标：拖拽改变 yaw/pitch，滚轮改变距离）
    const { yaw: vy, pitch, dist } = view.current
    const cx = p.position.x + Math.sin(vy) * Math.cos(pitch) * dist
    const cz = p.position.z + Math.cos(vy) * Math.cos(pitch) * dist
    const cy = p.position.y + Math.sin(pitch) * dist
    const cam = state.camera
    const t = Math.min(1, dt * 5)
    cam.position.x += (cx - cam.position.x) * t
    cam.position.y += (cy - cam.position.y) * t
    cam.position.z += (cz - cam.position.z) * t
    cam.lookAt(p.position.x, 1.2, p.position.z)
  })

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

      {/* 建筑 */}
      {BUILDINGS.map((b) => (
        <Building key={b.id} def={b} />
      ))}

      {/* 树 */}
      {TREES.map(([tx, tz], i) => (
        <group key={i} position={[tx, 0, tz]}>
          <mesh position={[0, 0.5, 0]}>
            <boxGeometry args={[0.36, 1, 0.36]} />
            <meshLambertMaterial color="#6d4a2f" />
          </mesh>
          <mesh position={[0, 1.45, 0]}>
            <boxGeometry args={[1.5, 1.3, 1.5]} />
            <meshLambertMaterial color={i % 2 ? '#3f7a3f' : '#468a48'} />
          </mesh>
        </group>
      ))}

      {/* 角色影子 */}
      <mesh ref={shadow} rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.03, 4]}>
        <circleGeometry args={[0.42, 16]} />
        <meshBasicMaterial color="#000000" transparent opacity={0.22} />
      </mesh>

      {/* 角色（占位像素块，后续迭代替换为像素精灵） */}
      <group ref={player} position={[0, 0, 4]}>
        <mesh position={[0, 0.55, 0]}>
          <boxGeometry args={[0.55, 0.9, 0.4]} />
          <meshLambertMaterial color="#e8b04a" />
        </mesh>
        <mesh position={[0, 1.18, 0]}>
          <boxGeometry args={[0.44, 0.44, 0.44]} />
          <meshLambertMaterial color="#f2d3a0" />
        </mesh>
        <mesh position={[0, 0.55, 0.28]}>
          <boxGeometry args={[0.5, 0.16, 0.08]} />
          <meshLambertMaterial color="#2b6cb0" />
        </mesh>
      </group>
    </>
  )
}

function Building({ def }) {
  const [w, h, d] = def.size
  const [bx, bz] = def.pos
  const wallTex = useMemo(() => buildingTexture(def.wall, def.cols, def.rows), [def])
  const signTex = useMemo(() => signTexture(def.name), [def.name])
  const rotationY = def.face === 1 ? 0 : Math.PI

  return (
    <group position={[bx, h / 2, bz]} rotation={[0, rotationY, 0]}>
      <mesh>
        <boxGeometry args={[w, h, d]} />
        <meshLambertMaterial map={wallTex} />
      </mesh>
      <mesh position={[0, h / 2 + 0.16, 0]}>
        <boxGeometry args={[w + 0.7, 0.32, d + 0.7]} />
        <meshLambertMaterial color={def.roof} />
      </mesh>
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
