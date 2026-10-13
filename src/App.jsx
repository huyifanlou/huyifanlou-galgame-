import { Canvas } from '@react-three/fiber'
import CampusScene from './game/CampusScene.jsx'
import Hud from './game/Hud.jsx'

// 像素化渲染：以 1/4 分辨率渲染（dpr=0.25），再由 CSS 最近邻放大到全屏。
// 这是 HD-2D 观感的核心，改这个值即可调整像素颗粒大小。
const PIXEL_DPR = 0.25

export default function App() {
  return (
    <>
      <Canvas
        dpr={PIXEL_DPR}
        gl={{ antialias: false, powerPreference: 'high-performance' }}
        camera={{ fov: 46, near: 0.1, far: 200, position: [0, 8.5, 12] }}
      >
        <color attach="background" args={['#10141c']} />
        <CampusScene />
      </Canvas>
      <Hud />
    </>
  )
}
