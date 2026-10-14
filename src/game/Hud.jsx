import { useEffect } from 'react'
import { useGameStore } from './store.js'

// 构建版本号：每次重建手动+1（右下角常驻小字，用于肉眼确认浏览器加载的是哪一版）
export const BUILD_VERSION = 'v0.29'

const base = {
  position: 'fixed',
  color: '#e7e9ee',
  fontFamily: 'system-ui, "Microsoft YaHei", sans-serif',
  textShadow: '0 1px 2px rgba(0,0,0,.65)',
  pointerEvents: 'none',
  zIndex: 10,
}

export default function Hud() {
  const near = useGameStore((s) => s.near)
  const gameMinutes = useGameStore((s) => s.gameMinutes)
  const dayNum = useGameStore((s) => s.dayNum)
  const timeSpeed = useGameStore((s) => s.timeSpeed)
  const toast = useGameStore((s) => s.toast)
  const toastId = useGameStore((s) => s.toastId)
  const clearToast = useGameStore((s) => s.clearToast)

  // 时间刻度：第 N 天 · ☀/🌙 HH:MM · 速度
  const h = gameMinutes / 60
  const icon = h >= 5.5 && h < 18.5 ? '☀' : '🌙'
  const hh = String(Math.floor(h)).padStart(2, '0')
  const mm = String(Math.floor(gameMinutes % 60)).padStart(2, '0')
  const speedText = timeSpeed === 0 ? '暂停' : `${timeSpeed}×`

  useEffect(() => {
    if (!toast) return
    const t = setTimeout(clearToast, 2000)
    return () => clearTimeout(t)
  }, [toastId, toast, clearToast])

  return (
    <>
      {/* 左上：标题与操作说明 */}
      <div style={{ ...base, left: 16, top: 14 }}>
        <div style={{ fontWeight: 700, fontSize: 18 }}>（游戏名待定）· 校园叙事 RPG</div>
        <div style={{ marginTop: 6, opacity: 0.85 }}>
          WASD / 方向键 移动 ｜ F 交互 ｜ Q / E 转视角 ｜ 拖拽 / 滚轮 视角 ｜ P 画质 ｜ 0/1/2/3 时间速度 ｜ T +12h ｜ [ ] ±1h
        </div>
      </div>

      {/* 右上：时间刻度（第 N 天 + 时钟 + 昼夜图标 + 速度） */}
      <div style={{ ...base, right: 18, top: 14, fontSize: 15, opacity: 0.92 }}>
        第 {dayNum} 天 · {icon} {hh}:{mm} · {speedText}
      </div>

      {/* 底部中央：交互提示 */}
      {near && (
        <div
          style={{
            ...base,
            left: '50%',
            transform: 'translateX(-50%)',
            bottom: 74,
            fontSize: 16,
            background: 'rgba(16,20,28,.72)',
            padding: '6px 14px',
            borderRadius: 8,
            border: '1px solid rgba(255,255,255,.18)',
          }}
        >
          按 <b>F</b> 进入「{near}」
        </div>
      )}

      {/* 右下：构建版本号（可见即可确认页面为最新构建，避免浏览器缓存旧版） */}
      <div style={{ ...base, right: 14, bottom: 10, fontSize: 12, opacity: 0.55 }}>
        {BUILD_VERSION} · 2026-10-13
      </div>

      {/* 提示条 */}
      {toast && (
        <div
          style={{
            ...base,
            left: '50%',
            transform: 'translateX(-50%)',
            bottom: 122,
            fontSize: 15,
            opacity: 0.95,
          }}
        >
          {toast}
        </div>
      )}
    </>
  )
}
