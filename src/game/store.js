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

// 游戏时钟与昼夜相位：一天 24 小时（1440 分钟），起点 08:00
// 时间速度 = 每 1 真实秒推进的游戏分钟数；0=暂停。昼夜天空/灯光由时钟驱动（见 CampusScene）
const urlQ = (() => {
  try {
    return new URLSearchParams(window.location.search)
  } catch {
    return null
  }
})()
const urlTimeRaw = urlQ ? urlQ.get('time') : null
const urlTime = urlTimeRaw !== null && urlTimeRaw !== '' && Number.isFinite(parseFloat(urlTimeRaw)) ? parseFloat(urlTimeRaw) : null
// ?time=18.5 直接定表并按暂停验收；?phase=night 兼容旧验收参数（等价 time=21）
const initMinutes = urlTime !== null ? (((urlTime % 24) + 24) % 24) * 60 : urlQ && urlQ.get('phase') === 'night' ? 21 * 60 : 8 * 60
const initSpeed = urlTime !== null || (urlQ && urlQ.get('phase')) ? 0 : 6

// 全局游戏状态（迭代 2 日程系统在此基础上扩展属性/行动；当前承载时间、交互提示、画质与 HUD）
export const useGameStore = create((set) => ({
  near: null, // 面前可交互的地点名
  gameMinutes: initMinutes, // 当日分钟数（0-1440）
  dayNum: 1, // 第几天
  timeSpeed: initSpeed, // 每真实秒推进的游戏分钟数（0=暂停）
  toast: '',
  toastId: 0,
  pixelDpr: defaultDpr,
  setNear: (near) => set((s) => (s.near === near ? {} : { near })),
  showToast: (toast) => set({ toast, toastId: Date.now() }),
  clearToast: () => set({ toast: '' }),
  // 时间推进（由 CampusScene 每帧调用；按整数分钟变化节流，避免每帧全部重渲染）
  tick: (dt) =>
    set((s) => {
      if (!s.timeSpeed) return {}
      let m = s.gameMinutes + dt * s.timeSpeed
      let d = s.dayNum
      while (m >= 1440) {
        m -= 1440
        d += 1
      }
      if (Math.floor(m) === Math.floor(s.gameMinutes)) return {}
      return { gameMinutes: m, dayNum: d }
    }),
  setSpeed: (timeSpeed) =>
    set({
      timeSpeed,
      toast:
        timeSpeed === 0
          ? '时间已暂停（0/1/2/3 切换速度）'
          : `时间速度：${timeSpeed}×（1 秒 = ${timeSpeed} 游戏分钟）`,
      toastId: Date.now(),
    }),
  // 手动调整时间（T=+12h、[ ]=±1h）
  advanceTime: (min) =>
    set((s) => {
      let m = s.gameMinutes + min
      let d = s.dayNum
      while (m >= 1440) {
        m -= 1440
        d += 1
      }
      while (m < 0) {
        m += 1440
        if (d > 1) d -= 1
        else m = 0
      }
      return {
        gameMinutes: m,
        dayNum: d,
        toast: `时间 ${min > 0 ? '+' : ''}${min / 60} 小时（T 跳 12h ｜ [ ] ±1h）`,
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
