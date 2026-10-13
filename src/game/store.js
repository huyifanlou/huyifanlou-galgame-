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

// 全局游戏状态（迭代 2 起接入时间/属性系统；当前承载交互提示、画质与 HUD 信息）
export const useGameStore = create((set) => ({
  near: null, // 面前可交互的地点名
  day: '第 1 天 · 上午',
  toast: '',
  toastId: 0,
  pixelDpr: defaultDpr,
  setNear: (near) => set((s) => (s.near === near ? {} : { near })),
  showToast: (toast) => set({ toast, toastId: Date.now() }),
  clearToast: () => set({ toast: '' }),
  cyclePixel: () =>
    set((s) => {
      const i = PIXEL_LEVELS.indexOf(s.pixelDpr)
      const next = PIXEL_LEVELS[(i + 1) % PIXEL_LEVELS.length]
      return { pixelDpr: next, toast: `画面清晰度：${PIXEL_NAMES[next]}（P 键切换）`, toastId: Date.now() }
    }),
}))
