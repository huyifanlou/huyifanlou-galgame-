import { create } from 'zustand'

// 画面清晰度档位（dpr = 渲染分辨率比例；1 = 原生分辨率；>1 为超采样抗闪烁）
const PIXEL_LEVELS = [0.5, 0.75, 1, 1.25, 1.5, 2]
const PIXEL_NAMES = { 0.5: '复古（像素大颗粒）', 0.75: '柔和', 1: '高清', 1.25: '高清+', 1.5: '超清', 2: '极清（超采样抗闪烁）' }

// 默认档位 = 屏幕缩放的 1.6 倍（取 0.5 的整数步），上限 2、下限 1.25：
// 高分辨率渲染后缩小显示 = 超采样，能同时提升清晰度并压制移动时的纹理闪烁
const defaultDpr = (() => {
  try {
    const base = (window.devicePixelRatio || 1) * 1.6
    return Math.min(2, Math.max(1.25, Math.round(base * 2) / 2))
  } catch {
    return 1.5
  }
})()

// 昼夜相位（T 键手动切换；?phase=night 供验收取景；迭代 2 的日程系统将自动驱动）
const urlPhase = (() => {
  try {
    return new URLSearchParams(window.location.search).get('phase') === 'night' ? 'night' : 'day'
  } catch {
    return 'day'
  }
})()

// 全局游戏状态（迭代 2 起接入时间/属性系统；当前承载交互提示、画质、昼夜与 HUD 信息）
export const useGameStore = create((set) => ({
  near: null, // 面前可交互的地点名
  day: urlPhase === 'night' ? '第 1 天 · 夜晚' : '第 1 天 · 白天',
  phase: urlPhase, // 'day' | 'night'（白天=蓝天白云，夜晚=星空月亮）
  toast: '',
  toastId: 0,
  pixelDpr: defaultDpr,
  setNear: (near) => set((s) => (s.near === near ? {} : { near })),
  showToast: (toast) => set({ toast, toastId: Date.now() }),
  clearToast: () => set({ toast: '' }),
  togglePhase: () =>
    set((s) => {
      const phase = s.phase === 'day' ? 'night' : 'day'
      return {
        phase,
        day: `第 1 天 · ${phase === 'night' ? '夜晚' : '白天'}`,
        toast: phase === 'night' ? '入夜了……（T 键切换昼夜）' : '天亮了（T 键切换昼夜）',
        toastId: Date.now(),
      }
    }),
  cyclePixel: () =>
    set((s) => {
      const i = PIXEL_LEVELS.indexOf(s.pixelDpr)
      const next = PIXEL_LEVELS[(i + 1) % PIXEL_LEVELS.length]
      return { pixelDpr: next, toast: `画面清晰度：${PIXEL_NAMES[next]}（P 键切换）`, toastId: Date.now() }
    }),
}))
