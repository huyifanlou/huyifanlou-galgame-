import { useEffect } from 'react'
import { useGameStore } from './store.js'

// 构建版本号：每次重建手动+1（右下角常驻小字，用于肉眼确认浏览器加载的是哪一版）
export const BUILD_VERSION = 'v0.13'

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
  const day = useGameStore((s) => s.day)
  const toast = useGameStore((s) => s.toast)
  const toastId = useGameStore((s) => s.toastId)
  const clearToast = useGameStore((s) => s.clearToast)

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
          WASD / 方向键 移动 ｜ F 交互 ｜ Q / E 转视角（每次90°） ｜ 拖拽旋转 ｜ 滚轮缩放 ｜ P 画质
        </div>
      </div>

      {/* 右上：时间（迭代 2 接入日程系统） */}
      <div style={{ ...base, right: 18, top: 16, fontSize: 15, opacity: 0.9 }}>{day}</div>

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
