import { useEffect, useMemo, useRef } from 'react'
import { useFrame } from '@react-three/fiber'
import * as THREE from 'three'

const MOVE_SPEED = 6
const BOUND = 22

// W0 占位场景：程序化像素草地 + 三栋占位楼 + 可移动角色 + 跟随相机。
// 迭代 1 将替换为正式的像素贴图校园（教室 / 宿舍 / 图书馆 / 食堂）。
export default function CampusScene() {
  const player = useRef()
  const keys = useRef({})

  useEffect(() => {
    const down = (e) => {
      keys.current[e.code] = true
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

  // 程序化像素草地：16x16 棋盘 + 随机草点，NearestFilter 保持像素锐利
  const grassTexture = useMemo(() => {
    const size = 16
    const canvas = document.createElement('canvas')
    canvas.width = canvas.height = size
    const ctx = canvas.getContext('2d')
    for (let y = 0; y < size; y++) {
      for (let x = 0; x < size; x++) {
        ctx.fillStyle = (x + y) % 2 === 0 ? '#4f8f47' : '#477f40'
        ctx.fillRect(x, y, 1, 1)
      }
    }
    for (let i = 0; i < 24; i++) {
      const x = Math.floor(Math.random() * size)
      const y = Math.floor(Math.random() * size)
      ctx.fillStyle = '#3f7239'
      ctx.fillRect(x, y, 1, 1)
    }
    const tex = new THREE.CanvasTexture(canvas)
    tex.magFilter = THREE.NearestFilter
    tex.minFilter = THREE.NearestFilter
    tex.wrapS = tex.wrapT = THREE.RepeatWrapping
    tex.repeat.set(24, 24)
    return tex
  }, [])

  useFrame((state, delta) => {
    const p = player.current
    if (!p) return
    const dt = Math.min(delta, 0.05) // 切后台回来时防大跳
    const k = keys.current
    const ix = (k.KeyD || k.ArrowRight ? 1 : 0) - (k.KeyA || k.ArrowLeft ? 1 : 0)
    const iz = (k.KeyS || k.ArrowDown ? 1 : 0) - (k.KeyW || k.ArrowUp ? 1 : 0)

    if (ix !== 0 || iz !== 0) {
      const len = Math.hypot(ix, iz)
      const dirX = ix / len
      const dirZ = iz / len
      p.position.x = THREE.MathUtils.clamp(p.position.x + dirX * MOVE_SPEED * dt, -BOUND, BOUND)
      p.position.z = THREE.MathUtils.clamp(p.position.z + dirZ * MOVE_SPEED * dt, -BOUND, BOUND)
      // 平滑转向（处理角度环绕）
      let diff = Math.atan2(dirX, dirZ) - p.rotation.y
      diff = Math.atan2(Math.sin(diff), Math.cos(diff))
      p.rotation.y += diff * Math.min(1, dt * 12)
      // 占位"走路"上下摆动
      p.position.y = Math.abs(Math.sin(state.clock.elapsedTime * 10)) * 0.06
    } else {
      p.position.y *= 0.8
    }

    // 跟随相机（平滑插值）
    const cam = state.camera
    const t = Math.min(1, dt * 4)
    cam.position.x += (p.position.x - cam.position.x) * t
    cam.position.z += (p.position.z + 11 - cam.position.z) * t
    cam.position.y += (8 - cam.position.y) * t
    cam.lookAt(p.position.x, 1.2, p.position.z)
  })

  return (
    <>
      <ambientLight intensity={1.1} />
      <directionalLight position={[12, 18, 8]} intensity={1.6} />
      <fog attach="fog" args={['#10141c', 34, 72]} />

      {/* 草地 */}
      <mesh rotation={[-Math.PI / 2, 0, 0]}>
        <planeGeometry args={[80, 80]} />
        <meshLambertMaterial map={grassTexture} />
      </mesh>

      {/* 占位建筑（迭代 1 替换为像素贴图楼栋） */}
      <Building position={[-9, 1.6, -7]} size={[7, 3.2, 5]} color="#8a6f5a" />
      <Building position={[9, 1.2, -9]} size={[5, 2.4, 4]} color="#7a7f8a" />
      <Building position={[0, 1.8, -16]} size={[11, 3.6, 4]} color="#6d5f52" />

      {/* 角色（迭代 1 替换为像素精灵） */}
      <group ref={player} position={[0, 0, 4]}>
        <mesh position={[0, 0.55, 0]}>
          <boxGeometry args={[0.55, 0.9, 0.4]} />
          <meshLambertMaterial color="#e8b04a" />
        </mesh>
        <mesh position={[0, 1.18, 0]}>
          <boxGeometry args={[0.44, 0.44, 0.44]} />
          <meshLambertMaterial color="#f2d3a0" />
        </mesh>
        {/* 朝向标记（正面） */}
        <mesh position={[0, 0.55, 0.28]}>
          <boxGeometry args={[0.5, 0.16, 0.08]} />
          <meshLambertMaterial color="#2b6cb0" />
        </mesh>
      </group>
    </>
  )
}

function Building({ position, size, color }) {
  const [w, h, d] = size
  return (
    <group position={position}>
      <mesh>
        <boxGeometry args={[w, h, d]} />
        <meshLambertMaterial color={color} />
      </mesh>
      {/* 屋檐/顶层 */}
      <mesh position={[0, h / 2 + 0.12, 0]}>
        <boxGeometry args={[w + 0.5, 0.24, d + 0.5]} />
        <meshLambertMaterial color="#3b3f4a" />
      </mesh>
    </group>
  )
}
