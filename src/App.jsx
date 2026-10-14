import { Canvas } from '@react-three/fiber'
import CampusScene from './game/CampusScene.jsx'
import Hud from './game/Hud.jsx'
import { useGameStore } from './game/store.js'

export default function App() {
  // 画面清晰度：P 键在 低/标准/清晰/细腻 之间切换（见 store.js）
  const dpr = useGameStore((s) => s.pixelDpr)

  return (
    <>
      <Canvas
        dpr={dpr}
        shadows="soft"
        gl={{ antialias: true, powerPreference: 'high-performance' }}
        camera={{ fov: 46, near: 0.5, far: 260, position: [0, 8, 13] }}
      >
        <color attach="background" args={['#10141c']} />
        <CampusScene />
      </Canvas>
      <Hud />
    </>
  )
}
