import { create } from 'zustand'

// 全局游戏状态（迭代 2 起接入时间/属性系统；当前承载交互提示与 HUD 信息）
export const useGameStore = create((set) => ({
  near: null, // 面前可交互的地点名
  day: '第 1 天 · 上午',
  toast: '',
  toastId: 0,
  setNear: (near) => set((s) => (s.near === near ? {} : { near })),
  showToast: (toast) => set({ toast, toastId: Date.now() }),
  clearToast: () => set({ toast: '' }),
}))
