import { create } from 'zustand'

// 画面清晰度档位（dpr = 渲染分辨率比例，越小像素颗粒越大）
const PIXEL_LEVELS = [0.25, 0.4, 0.5, 0.75]
const PIXEL_NAMES = { 0.25: '低（颗粒感强）', 0.4: '标准', 0.5: '清晰', 0.75: '细腻' }

// 全局游戏状态（迭代 2 起接入时间/属性系统；当前承载交互提示、画质与 HUD 信息）
export const useGameStore = create((set) => ({
  near: null, // 面前可交互的地点名
  day: '第 1 天 · 上午',
  toast: '',
  toastId: 0,
  pixelDpr: 0.5,
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
