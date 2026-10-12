import { Canvas } from '@react-three/fiber'
import CampusScene from './game/CampusScene.jsx'

// 像素化渲染：以 1/4 分辨率渲染（dpr=0.25），再由 CSS 最近邻放大到全屏。
// 这是 HD-2D 观感的核心，改这个值即可调整像素颗粒大小。
const PIXEL_DPR = 0.25

export default function App() {
  return (
    <>
      <Canvas
        dpr={PIXEL_DPR}
        gl={{ antialias: false, powerPreference: 'high-performance' }}
        camera={{ fov: 46, near: 0.1, far: 200, position: [0, 8, 12] }}
      >
        <color attach="background" args={['#10141c']} />
        <CampusScene />
      </Canvas>

      {/* HUD：开发期提示（迭代 2 起替换为正式游戏 UI） */}
      <div style={hudStyle}>
        <div style={{ fontWeight: 700, fontSize: 18 }}>（游戏名待定）· 校园叙事 RPG</div>
        <div style={{ marginTop: 6, opacity: 0.85 }}>W0 占位场景 ｜ WASD / 方向键 移动</div>
      </div>
    </>
  )
}

const hudStyle = {
  position: 'fixed',
  left: 16,
  top: 14,
  color: '#e7e9ee',
  fontFamily: 'system-ui, "Microsoft YaHei", sans-serif',
  textShadow: '0 1px 2px rgba(0,0,0,.65)',
  pointerEvents: 'none',
  zIndex: 10
}
