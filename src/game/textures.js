import * as THREE from 'three'

// 程序化像素纹理工具
// magFilter 保持 Nearest（近看是清晰像素画），minFilter 用 mipmap（远景稳定不闪烁）
export function canvasTexture(w, h, draw, repeatX = 1, repeatY = 1) {
  const c = document.createElement('canvas')
  c.width = w
  c.height = h
  const ctx = c.getContext('2d')
  draw(ctx, w, h)
  const t = new THREE.CanvasTexture(c)
  t.magFilter = THREE.NearestFilter
  // mip 层级之间线性过渡（消除移动时"虚影"跳变），层内保持最近邻（像素块感）
  t.minFilter = THREE.NearestMipmapLinearFilter
  t.anisotropy = 16
  t.wrapS = t.wrapT = THREE.RepeatWrapping
  t.repeat.set(repeatX, repeatY)
  t.colorSpace = THREE.SRGBColorSpace
  return t
}

const rand = (n) => Math.floor(Math.random() * n)

// 128×128 草地：4px 棋盘 + 草叶细节（rep 可按用途调整：地面 30 / 色块补丁 5）
// 图案尺度=4px 级（世界尺度 10cm ≈ 原 1px@32 版）——mipmap 降采样时格子结构可存活 2-3 级，远景不糊成纯色
export function grassTexture(rep = 30) {
  return canvasTexture(128, 128, (ctx) => {
    for (let y = 0; y < 128; y += 4) {
      for (let x = 0; x < 128; x += 4) {
        ctx.fillStyle = (x + y) % 8 === 0 ? '#54904b' : '#4a8743'
        ctx.fillRect(x, y, 4, 4)
      }
    }
    // 草叶（4×6 / 6×4 粗颗粒）
    for (let i = 0; i < 100; i++) {
      ctx.fillStyle = ['#3f7239', '#5da054', '#437c3c', '#61a85a'][rand(4)]
      ctx.fillRect(rand(122), rand(122), rand(2) ? 4 : 6, rand(2) ? 6 : 4)
    }
    // 草丛色块（8-14px，远景保留层次）
    for (let i = 0; i < 18; i++) {
      ctx.globalAlpha = 0.35 + Math.random() * 0.2
      ctx.fillStyle = ['#48853f', '#5c9c53', '#437c3c'][rand(3)]
      ctx.fillRect(rand(112), rand(112), 8 + rand(7), 8 + rand(7))
      ctx.globalAlpha = 1
    }
  }, rep, rep)
}

// 64×64 石板路：2×2 石板 + 勾缝 + 磨损（高密度版，世界布局不变、纹理密度 ×2）
export function pathTexture(horizontal = false, rep = 14) {
  return canvasTexture(64, 64, (ctx) => {
    ctx.fillStyle = '#a49c8d'
    ctx.fillRect(0, 0, 64, 64)
    for (let by = 0; by < 2; by++) {
      for (let bx = 0; bx < 2; bx++) {
        ctx.fillStyle = ['#c1b9ac', '#b8b0a3', '#bdb4a6', '#b3ab9d'][rand(4)]
        ctx.fillRect(bx * 32 + 2, by * 32 + 2, 28, 28)
        for (let i = 0; i < 10; i++) {
          ctx.fillStyle = 'rgba(122,114,102,0.45)'
          ctx.fillRect(bx * 32 + 2 + rand(28), by * 32 + 2 + rand(28), 4, 2)
        }
        for (let i = 0; i < 6; i++) {
          ctx.fillStyle = 'rgba(230,226,216,0.5)'
          ctx.fillRect(bx * 32 + 2 + rand(28), by * 32 + 2 + rand(28), 2, 2)
        }
      }
    }
  }, horizontal ? rep : 1, horizontal ? 1 : rep)
}

// 建筑外墙（高密度版：cell=32，立面约 19px/世界单位，为原版 3.2 倍；图案世界尺度不变）
// opts.lit：亮窗概率（白天低≈0.05 素净、夜晚高≈0.55 暖灯），由 Building 随昼夜切换两套贴图
// 配色协调原则：玻璃冷蓝灰为主，窗帘/花箱用低饱和素色，避免"五颜六色"
export function buildingTexture(wall, trim, plinth, cols, rows, { banner = false, lit = 0.3 } = {}) {
  const cell = 32
  return canvasTexture(cols * cell, rows * cell + 40, (ctx, w, h) => {
    ctx.fillStyle = wall
    ctx.fillRect(0, 0, w, h)
    // 砖石噪点
    for (let i = 0; i < Math.floor((w * h) / 24); i++) {
      ctx.fillStyle = 'rgba(0,0,0,0.05)'
      ctx.fillRect(rand(w), rand(h), 6, 3)
      ctx.fillStyle = 'rgba(255,255,255,0.05)'
      ctx.fillRect(rand(w), rand(h), 3, 3)
    }
    // 檐口与楼层线（双线砖带 + 阴影线）
    ctx.fillStyle = trim
    ctx.fillRect(0, 0, w, 8)
    ctx.fillStyle = 'rgba(0,0,0,0.12)'
    ctx.fillRect(0, 8, w, 3)
    for (let r = 1; r < rows; r++) {
      ctx.fillStyle = trim
      ctx.fillRect(0, r * cell + 12, w, 7)
      ctx.fillStyle = 'rgba(0,0,0,0.15)'
      ctx.fillRect(0, r * cell + 19, w, 3)
    }
    // 基座
    ctx.fillStyle = plinth
    ctx.fillRect(0, h - 24, w, 24)
    ctx.fillStyle = 'rgba(0,0,0,0.18)'
    ctx.fillRect(0, h - 9, w, 9)
    // 窗户
    for (let r = 0; r < rows; r++) {
      for (let c2 = 0; c2 < cols; c2++) {
        const isLit = Math.random() < lit
        const kind = rand(10) // 0/1 素色窗帘 · 2 花箱 · 3 空调 · 其余普通
        const wx = c2 * cell + 7
        const wy = r * cell + 10
        ctx.fillStyle = trim
        ctx.fillRect(wx - 3, wy - 3, 26, 26) // 窗框
        ctx.fillStyle = isLit ? '#ffd98a' : '#2c3548'
        ctx.fillRect(wx, wy, 20, 20)
        if (isLit) {
          ctx.fillStyle = '#ffe9b8'
          ctx.fillRect(wx, wy, 20, 3)
        } else if (Math.random() < 0.35) {
          // 玻璃反光（左上斜光斑，冷灰蓝——素净的丰富感）
          ctx.fillStyle = 'rgba(170,195,225,0.30)'
          ctx.fillRect(wx + 2, wy + 2, 7, 3)
          ctx.fillRect(wx + 4, wy + 5, 5, 3)
        }
        if (kind <= 1 && !isLit) {
          ctx.fillStyle = '#a08b80' // 素色窗帘（低饱和暖灰）
          ctx.fillRect(wx, wy, 20, 7)
        }
        if (kind === 2 && !isLit) {
          ctx.fillStyle = '#3f6e35' // 窗台绿植（低饱和深绿）
          ctx.fillRect(wx, wy + 21, 20, 5)
        }
        ctx.fillStyle = 'rgba(0,0,0,0.3)'
        ctx.fillRect(wx, wy + 17, 20, 3)
        // 窗台阴影 + 空调外机
        ctx.fillStyle = 'rgba(0,0,0,0.22)'
        ctx.fillRect(wx, wy + 26, 20, 3)
        if (kind === 3) {
          ctx.fillStyle = '#9aa2ac'
          ctx.fillRect(wx + 3, wy + 26, 13, 10)
          ctx.fillStyle = 'rgba(0,0,0,0.25)'
          ctx.fillRect(wx + 3, wy + 33, 13, 3)
        }
      }
    }
    // 宣传横幅（红底黄字带，挂在二层窗带上方；校园气息）
    if (banner) {
      const by = cell + 44
      ctx.fillStyle = 'rgba(0,0,0,0.18)'
      ctx.fillRect(12, by + 26, w - 24, 3)
      ctx.fillStyle = '#c0392b'
      ctx.fillRect(12, by, w - 24, 26)
      ctx.fillStyle = '#f5d04e'
      for (let x = 26; x < w - 38; x += 22) ctx.fillRect(x, by + 7, 13, 12)
    }
  })
}

// 大门：门框 + 暖光气窗 + 双开木门 + 金属把手 + 踢脚
export function doorTexture() {
  return canvasTexture(32, 48, (ctx, w, h) => {
    ctx.fillStyle = '#efe7d8'
    ctx.fillRect(0, 0, w, h)
    ctx.fillStyle = '#6b4a34'
    ctx.fillRect(3, 3, w - 6, h - 3)
    // 气窗（暖光 + 窗棂）
    ctx.fillStyle = '#ffd98a'
    ctx.fillRect(6, 7, w - 12, 12)
    ctx.fillStyle = '#5a3c2a'
    ctx.fillRect(w / 2 - 1, 7, 2, 12)
    ctx.fillRect(6, 12, w - 12, 2)
    // 双开门板
    ctx.fillStyle = '#54382a'
    ctx.fillRect(6, 24, 8, 17)
    ctx.fillRect(w - 14, 24, 8, 17)
    ctx.fillStyle = '#3a2519'
    ctx.fillRect(w / 2 - 1, 24, 2, 18)
    // 把手
    ctx.fillStyle = '#d8b54a'
    ctx.fillRect(w / 2 - 5, 34, 2, 2)
    ctx.fillRect(w / 2 + 3, 34, 2, 2)
    // 踢脚线
    ctx.fillStyle = '#8a6a4a'
    ctx.fillRect(3, h - 3, w - 6, 3)
  })
}

// 雨棚条纹（暖白 / 砖红竖条纹）
export function stripesTexture(a = '#efe9dd', b = '#c9553f') {
  return canvasTexture(16, 16, (ctx) => {
    for (let x = 0; x < 16; x++) {
      ctx.fillStyle = x % 4 < 2 ? a : b
      ctx.fillRect(x, 0, 1, 16)
    }
  }, 4, 1)
}

// 招牌：320×80、加粗大字号（原生分辨率下清晰可读）
export function signTexture(text, bg = '#3a2f4a') {
  return canvasTexture(320, 80, (ctx, w, h) => {
    ctx.fillStyle = bg
    ctx.fillRect(0, 0, w, h)
    ctx.strokeStyle = 'rgba(255,235,180,0.55)'
    ctx.lineWidth = 2
    ctx.strokeRect(4, 4, w - 8, h - 8)
    ctx.strokeStyle = 'rgba(255,255,255,0.2)'
    ctx.lineWidth = 1
    ctx.strokeRect(9, 9, w - 18, h - 18)
    ctx.fillStyle = '#fff6e0'
    ctx.font = 'bold 52px "Microsoft YaHei", sans-serif'
    ctx.textAlign = 'center'
    ctx.textBaseline = 'middle'
    ctx.fillText(text, w / 2, h / 2 + 3)
  })
}

// 花丛补丁（草地底色 + 三色小花，可与草地自然衔接）
// 格宽对齐主草地：花丛面片 1.7u / 16 格 = 10.6cm ≈ 主草地 10.4cm，像素节奏一致
export function flowerTexture() {
  return canvasTexture(64, 64, (ctx) => {
    for (let y = 0; y < 64; y += 4) {
      for (let x = 0; x < 64; x += 4) {
        ctx.fillStyle = (x + y) % 8 === 0 ? '#54904b' : '#4a8743'
        ctx.fillRect(x, y, 4, 4)
      }
    }
    const colors = ['#e8748a', '#f2d857', '#f4f2ea', '#c58ae0']
    for (let i = 0; i < 18; i++) {
      const x = 2 + rand(56)
      const y = 2 + rand(54)
      ctx.fillStyle = colors[rand(colors.length)]
      ctx.fillRect(x, y, 4, 4)
      ctx.fillStyle = '#3f7239'
      ctx.fillRect(x + 1, y + 4, 2, 2)
    }
  })
}

// 土斑补丁（仅裸土本体，透明底）
// 不再携带贴片草地环（会与真实草地形成割裂）；边缘用像素抖动 + 稀疏碎土与主草地自然交融
// 世界尺度：面片 4.8u / 96px → 1px=5cm；土块半径≈1.25u，颗粒 2px=10cm 与主草地节奏一致
export function dirtTexture() {
  return canvasTexture(96, 96, (ctx) => {
    for (let y = 4; y < 92; y += 2) {
      for (let x = 4; x < 92; x += 2) {
        const d = Math.hypot(x - 48, y - 48) + (Math.random() * 7 - 3.5)
        let solid = 0
        if (d < 24) solid = 1 // 土块本体
        else if (d < 29) solid = Math.random() < 0.5 ? 1 : 0 // 像素抖动边缘
        else if (d < 34) solid = Math.random() < 0.12 ? 1 : 0 // 外围碎土点
        if (solid) {
          ctx.fillStyle = ['#8a6f4f', '#7d6448', '#938060'][rand(3)]
          ctx.fillRect(x, y, 2, 2)
        }
      }
    }
  })
}

// 操场：512×320（32px/世界单位，与草地同密度）——红色跑道环 + 白线 + 像素噪点；内场草地与主草地同节奏（4px 格，世界≈10cm）
export function trackTexture() {
  return canvasTexture(512, 320, (ctx, w, h) => {
    const rr = (x, y, ww, hh, r) => {
      ctx.beginPath()
      ctx.roundRect(x, y, ww, hh, r)
    }
    const grassFill = (x0, y0, x1, y1) => {
      for (let y = y0; y < y1; y += 4) {
        for (let x = x0; x < x1; x += 4) {
          ctx.fillStyle = (x + y) % 8 === 0 ? '#54904b' : '#4a8743'
          ctx.fillRect(x, y, 4, 4)
        }
      }
    }
    // 底：整片草地（跑道环外四角与草地同节奏）
    grassFill(0, 0, w, h)
    // 跑道红环
    rr(12, 12, w - 24, h - 24, 92)
    ctx.fillStyle = '#b26240'
    ctx.fill()
    // 红环像素噪点（裁剪到红环范围内，不洒到草地）
    ctx.save()
    rr(12, 12, w - 24, h - 24, 92)
    ctx.clip()
    for (let i = 0; i < 900; i++) {
      ctx.fillStyle = ['#a8593a', '#bd6f4a', '#9e5335', '#c47a52'][rand(4)]
      ctx.fillRect(rand(w), rand(h), 4, 4)
    }
    ctx.restore()
    // 白色分道线（内外两圈）
    ctx.strokeStyle = 'rgba(244,240,226,0.9)'
    ctx.lineWidth = 4
    rr(24, 24, w - 48, h - 48, 82)
    ctx.stroke()
    rr(40, 40, w - 80, h - 80, 70)
    ctx.stroke()
    // 内场草地（与主草地同节奏）
    rr(48, 48, w - 96, h - 96, 64)
    ctx.fillStyle = '#4a8743'
    ctx.fill()
    ctx.save()
    rr(48, 48, w - 96, h - 96, 64)
    ctx.clip()
    for (let y = 48; y < h - 48; y += 4) {
      for (let x = 48; x < w - 48; x += 4) {
        ctx.fillStyle = (x + y) % 8 === 0 ? '#54904b' : '#4a8743'
        ctx.fillRect(x, y, 4, 4)
      }
    }
    for (let i = 0; i < 130; i++) {
      ctx.fillStyle = ['#3f7239', '#5da054', '#437c3c'][rand(3)]
      ctx.fillRect(48 + rand(w - 96), 48 + rand(h - 96), rand(2) ? 6 : 4, rand(2) ? 6 : 4)
    }
    // 中线 + 中圈
    ctx.fillStyle = 'rgba(244,240,226,0.9)'
    ctx.fillRect(w / 2 - 2, 52, 4, h - 104)
    ctx.strokeStyle = 'rgba(244,240,226,0.9)'
    ctx.lineWidth = 4
    ctx.beginPath()
    ctx.arc(w / 2, h / 2, 28, 0, Math.PI * 2)
    ctx.stroke()
    ctx.restore()
  })
}

// 池塘（像素贴图）：石砌池沿（分块+噪声）+ 深水/浅水波纹 + 睡莲 + 高光点
// 面片直径 7.2u / 256px = 35.5px/单位（与草地同密度）；环沿 r3.0-3.6u、水面 r3.0u，画布圆外透明
export function pondTexture() {
  const c = document.createElement('canvas')
  c.width = 256
  c.height = 256
  const ctx = c.getContext('2d')
  ctx.clearRect(0, 0, 256, 256)
  // 石沿底环
  ctx.fillStyle = '#8d8d94'
  ctx.beginPath()
  ctx.arc(128, 128, 128, 0, Math.PI * 2)
  ctx.fill()
  // 石沿分块 + 噪声
  for (let i = 0; i < 16; i++) {
    const a = (i / 16) * Math.PI * 2
    ctx.fillStyle = ['#9aa0a6', '#83838a', '#a9adb2', '#7b7b82'][rand(4)]
    ctx.fillRect(118 + Math.cos(a) * 118 - 10, 128 + Math.sin(a) * 118 - 10, 20, 20)
  }
  for (let i = 0; i < 420; i++) {
    const a = Math.random() * Math.PI * 2
    const rr = 108 + Math.random() * 18
    ctx.fillStyle = ['rgba(0,0,0,0.12)', 'rgba(255,255,255,0.10)'][rand(2)]
    ctx.fillRect(128 + Math.cos(a) * rr - 2, 128 + Math.sin(a) * rr - 2, 4, 4)
  }
  // 水面（深）
  ctx.fillStyle = '#3e6ea6'
  ctx.beginPath()
  ctx.arc(128, 128, 106, 0, Math.PI * 2)
  ctx.fill()
  // 水波纹（横向浅色短线 + 波光点）
  for (let i = 0; i < 60; i++) {
    const a = Math.random() * Math.PI * 2
    const rr = Math.sqrt(Math.random()) * 98
    const x = 128 + Math.cos(a) * rr
    const y = 128 + Math.sin(a) * rr
    ctx.fillStyle = ['#4d82bd', '#5a8fc8', '#4878b0'][rand(3)]
    ctx.fillRect(x - 8, y, 12 + rand(10), 3)
  }
  for (let i = 0; i < 26; i++) {
    const a = Math.random() * Math.PI * 2
    const rr = Math.sqrt(Math.random()) * 96
    ctx.fillStyle = 'rgba(190,215,240,0.75)'
    ctx.fillRect(128 + Math.cos(a) * rr, 128 + Math.sin(a) * rr, 3, 3)
  }
  // 中心更亮区（浅水感）
  ctx.fillStyle = '#4d82bd'
  ctx.beginPath()
  ctx.arc(136, 122, 34, 0, Math.PI * 2)
  ctx.fill()
  for (let i = 0; i < 16; i++) {
    ctx.fillStyle = 'rgba(190,215,240,0.6)'
    ctx.fillRect(110 + rand(52), 96 + rand(52), 3, 2)
  }
  // 睡莲（3 片）
  for (const [lx, ly, lr] of [
    [86, 156, 11],
    [168, 92, 9],
    [150, 172, 8],
  ]) {
    ctx.fillStyle = '#3f7a37'
    ctx.beginPath()
    ctx.arc(lx, ly, lr, 0.35, Math.PI * 2 + 0.1)
    ctx.closePath()
    ctx.fill()
    ctx.fillStyle = '#2f5f2a'
    ctx.fillRect(lx - 2, ly - lr + 2, 4, lr * 2 - 4)
    ctx.fillStyle = '#5da04f'
    ctx.fillRect(lx - lr + 3, ly - lr + 3, 6, 5)
  }
  const t = new THREE.CanvasTexture(c)
  t.magFilter = THREE.NearestFilter
  t.minFilter = THREE.NearestFilter
  t.generateMipmaps = false
  t.colorSpace = THREE.SRGBColorSpace
  return t
}

// 天空穹顶纹理（1024×512 等距圆柱投影，画布上沿=天顶、y≈256=地平线）
// 白天：蓝天分带渐变 + 像素云（水平方向可无缝平铺用于云飘动）
// 夜晚：深蓝分带渐变 + 星空 + 块状像素月亮
// 注意：天穹关闭 mipmap（星星只有 1-2 像素，会降采样消失；星点爬行即自然闪烁）
export function skyTexture(phase = 'day') {
  const c = document.createElement('canvas')
  c.width = 1024
  c.height = 512
  const ctx = c.getContext('2d')
  const W = 1024

  const bandGrad = (colors) => {
    // 天顶→地平线（y 0→256）分带渐变，地平线以下用最后一色填满
    const n = colors.length
    const step = 256 / n
    colors.forEach((col, i) => {
      ctx.fillStyle = col
      ctx.fillRect(0, Math.floor(i * step), W, Math.ceil(step) + 1)
    })
    ctx.fillStyle = colors[n - 1]
    ctx.fillRect(0, 256, W, 256)
  }

  const rnd = (n) => Math.floor(Math.random() * n)

  if (phase === 'day') {
    // 渐变下沉：默认视角只能看到地平线上方 0-6°（画布 y≈238-256），把亮蓝与云放在该带内
    bandGrad(['#3f86cf', '#4b93d6', '#599edc', '#68aae2', '#79b9e8', '#8cc6ee', '#9fd2f2', '#b2dcf5', '#c0e3f6', '#cfe8f8'])
    // 像素云（底影 + 白色主体；x±1024 重复保证平铺无缝）
    const CLOUD = [
      [3, 0, 10, 3],
      [1, 2, 16, 3],
      [0, 4, 20, 3],
      [2, 6, 14, 3],
      [6, 8, 7, 2],
    ]
    const drawCloud = (cx, cy, s) => {
      ctx.fillStyle = '#c9dcee'
      for (const [x, y, w, h] of CLOUD) ctx.fillRect(cx + x * s, cy + (y + 1) * s, w * s, h * s)
      ctx.fillStyle = '#ffffff'
      for (const [x, y, w, h] of CLOUD) ctx.fillRect(cx + x * s, cy + y * s, w * s, h * s)
    }
    // 云带：墙顶之上（约 25-45°仰角低带之上的可见区），确保任何常规视角可见
    for (let i = 0; i < 9; i++) {
      const s = 2 + rnd(3)
      const cx = rnd(W)
      const cy = Math.min(130 + rnd(62), 248 - 11 * s)
      for (const off of [-W, 0, W]) drawCloud(cx + off, cy, s)
    }
    for (let i = 0; i < 5; i++) {
      const s = 2 + rnd(2)
      const cx = rnd(W)
      const cy = 104 + rnd(30)
      for (const off of [-W, 0, W]) drawCloud(cx + off, cy, s)
    }
  } else {
    bandGrad(['#04060e', '#060a15', '#080f1e', '#0b1427', '#0f1b31', '#14223c', '#192a47', '#203352'])
    // 星星：密布于墙顶之上的可见区（140-198 ≈ 20-40°仰角）+ 更高考度纵深散布
    for (let i = 0; i < 220; i++) {
      ctx.fillStyle = ['#ffffff', '#dfe8ff', '#cfe0ff', '#ffeecf'][rnd(4)]
      const big = Math.random() < 0.1
      ctx.fillRect(rnd(W), 140 + rnd(58), big ? 2 : 1, big ? 2 : 1)
    }
    for (let i = 0; i < 110; i++) {
      ctx.fillStyle = ['#ffffff', '#dfe8ff', '#cfe0ff'][rnd(3)]
      ctx.fillRect(rnd(W), 118 + rnd(82), 1, 1)
    }
    for (let i = 0; i < 9; i++) {
      const x = 40 + rnd(W - 80)
      const y = 148 + rnd(48)
      ctx.fillStyle = '#ffffff'
      ctx.fillRect(x - 1, y, 3, 1)
      ctx.fillRect(x, y - 1, 1, 3)
      ctx.fillStyle = '#eef4ff'
      ctx.fillRect(x, y, 1, 1)
    }
    // 块状像素月亮（墙顶之上，约 30°仰角）+ 陨石坑
    const mx = 745
    const my = 172
    const R = 10
    const B = 2
    ctx.fillStyle = '#eef2df'
    for (let yy = -R; yy < R; yy += B) {
      for (let xx = -R; xx < R; xx += B) {
        if (Math.hypot(xx + B / 2, yy + B / 2) <= R) ctx.fillRect(mx + xx, my + yy, B, B)
      }
    }
    ctx.fillStyle = '#c9d4b8'
    ctx.fillRect(mx - 6, my - 4, 4, 4)
    ctx.fillRect(mx + 2, my + 2, 4, 4)
    ctx.fillRect(mx - 2, my + 6, 2, 2)
  }

  const t = new THREE.CanvasTexture(c)
  t.magFilter = THREE.NearestFilter
  t.minFilter = THREE.NearestFilter
  t.generateMipmaps = false
  t.wrapS = THREE.RepeatWrapping
  t.wrapT = THREE.ClampToEdgeWrapping
  t.colorSpace = THREE.SRGBColorSpace
  return t
}

// 路灯地面光池（8 级像素环渐变，加色混合；晚上随昼夜混合值淡入）
export function lampGlowTexture() {
  const c = document.createElement('canvas')
  c.width = 64
  c.height = 64
  const ctx = c.getContext('2d')
  for (let y = 0; y < 64; y++) {
    for (let x = 0; x < 64; x++) {
      const d = Math.hypot(x - 31.5, y - 31.5) / 32
      if (d > 1) continue
      const a = Math.round((1 - d) * 7) / 7 * 0.85
      if (a <= 0.01) continue
      ctx.fillStyle = `rgba(255,214,140,${a.toFixed(2)})`
      ctx.fillRect(x, y, 1, 1)
    }
  }
  const t = new THREE.CanvasTexture(c)
  t.magFilter = THREE.NearestFilter
  t.minFilter = THREE.NearestFilter
  t.generateMipmaps = false
  t.colorSpace = THREE.SRGBColorSpace
  return t
}

// 盒子世界围墙（12288×1024 全景画布，分 4 段供四面墙各取一段，每段画作差异化）
// 关键：可见视角带只覆盖墙下部（仰角 0-7° ≈ 画布 y 541-813 附近的条带），风景全部压缩绘制在该带内；
// 风景以上为天底渐变（与穹顶地平线色调衔接）。白天原色显示（fog=false），夜晚由材质 color 压暗。
export function wallTexture() {
  const c = document.createElement('canvas')
  c.width = 12288
  c.height = 1024
  const ctx = c.getContext('2d')
  const rnd = (n) => Math.floor(Math.random() * n)
  const H = 1024
  const QW = 3072
  const B = 4 // 像素块

  // 天底（像素分带渐变，与穹顶地平线色 #cfe3f4 衔接）
  const skyBands = ['#9fc2de', '#a8c8e2', '#b2cfe6', '#bcd6ea', '#c6ddee', '#cfe3f4']
  skyBands.forEach((col, i) => {
    ctx.fillStyle = col
    ctx.fillRect(0, Math.floor((i * 700) / skyBands.length), 12288, Math.ceil(700 / skyBands.length) + 1)
  })
  ctx.fillStyle = '#cfe3f4'
  ctx.fillRect(0, 700, 12288, H - 700)

  // 像素山脊（块状填充 + 可选雪顶，峰线随参数变化）
  const ridge = (x0, w, baseY, amp, color, snowAmt) => {
    const step = 36
    const pts = []
    let h = baseY - amp * 0.5
    for (let x = 0; x <= w + step; x += step) {
      h += (rnd(3) - 1) * (amp * 0.26)
      h = Math.max(baseY - amp, Math.min(baseY - amp * 0.06, h))
      pts.push(h)
    }
    const hAt = (x) => {
      const i = Math.min(pts.length - 1, Math.floor(x / step))
      const j = Math.min(pts.length - 1, i + 1)
      const t = (x - i * step) / step
      return pts[i] + (pts[j] - pts[i]) * t
    }
    for (let x = 0; x < w; x += B) {
      const y = Math.round(hAt(x) / B) * B
      ctx.fillStyle = color
      ctx.fillRect(x0 + x, y, B, H - y)
    }
    if (snowAmt > 0) {
      for (let x = 0; x < w; x += B) {
        const y = Math.round(hAt(x) / B) * B
        const th = baseY - amp * (1.45 - snowAmt * 0.5)
        if (y < th) {
          ctx.fillStyle = ['#eef3f6', '#dde8ee'][rnd(2)]
          ctx.fillRect(x0 + x, y, B, 16 + rnd(26))
        }
      }
    }
  }
  const mist = (x0, w, y, ht, alpha) => {
    ctx.fillStyle = `rgba(222,236,244,${alpha})`
    ctx.fillRect(x0, y, w, ht)
    for (let x = 0; x < w; x += 16) {
      if (rnd(2)) ctx.fillRect(x0 + x, y - 8, 16, 8)
    }
  }
  // 针叶林带（尖顶锯齿）
  const pineBand = (x0, w, topY, color) => {
    for (let x = 0; x < w; x += 22) {
      const th = 44 + rnd(52)
      ctx.fillStyle = color
      ctx.fillRect(x0 + x, topY - th + rnd(18), 22, H - (topY - th))
    }
  }
  // 阔叶林带（圆冠丛，混在松林间增加层次）
  const broadleafBand = (x0, w, topY, color) => {
    for (let x = 0; x < w; x += 40) {
      const th = 56 + rnd(46)
      ctx.fillStyle = color
      ctx.fillRect(x0 + x, topY - th, 40, H - (topY - th))
      ctx.fillStyle = color
      ctx.fillRect(x0 + x - 10, topY - th + 16, 60, 22)
    }
  }
  // 特色点缀：古塔（三重檐剪影）/ 远城剪影 / 飞鸟
  const pagoda = (cx, baseY, s) => {
    ctx.fillStyle = '#4d4a56'
    ctx.fillRect(cx - 5 * s, baseY - 9 * s, 10 * s, 9 * s)
    for (let t = 0; t < 3; t++) {
      const w = (16 - t * 3) * s
      ctx.fillStyle = ['#5a5866', '#514f5c', '#484652'][t]
      ctx.fillRect(cx - w / 2, baseY - (12 + t * 11) * s, w, 4 * s)
      ctx.fillStyle = '#666472'
      ctx.fillRect(cx - (w / 2 + 2 * s), baseY - (12 + t * 11) * s, w + 4 * s, 2 * s)
    }
    ctx.fillStyle = '#6d6b78'
    ctx.fillRect(cx - s, baseY - 46 * s, 2 * s, 6 * s)
  }
  const citySil = (cx, baseY) => {
    for (let i = 0; i < 6; i++) {
      const w = 22 + rnd(26)
      const h = 36 + rnd(70)
      ctx.fillStyle = ['#7c8a99', '#73808f', '#8593a2'][rnd(3)]
      ctx.fillRect(cx + i * 46, baseY - h, w, h)
    }
  }
  const birds = (cx, cy, n) => {
    for (let i = 0; i < n; i++) {
      const x = cx + rnd(240)
      const y = cy + rnd(60)
      ctx.fillStyle = '#5a6a78'
      ctx.fillRect(x, y, 5, 3)
      ctx.fillRect(x - 6, y - 3, 7, 3)
      ctx.fillRect(x + 5, y - 3, 7, 3)
    }
  }

  // 每面墙的差异化参数（山高/雪量/特色元素，避免"单一"）
  const QP = [
    { far: [624, 84], mid: [738, 76], near: [850, 70], snow: 1.15, extra: 'pagoda' },
    { far: [606, 104], mid: [726, 68], near: [858, 62], snow: 1.45, extra: 'birds' },
    { far: [640, 66], mid: [752, 82], near: [862, 74], snow: 0.75, extra: 'city' },
    { far: [614, 92], mid: [742, 64], near: [838, 80], snow: 1.0, extra: 'broadleaf' },
  ]
  QP.forEach((p, q) => {
    const x0 = q * QW
    ridge(x0, QW, p.far[0], p.far[1], '#8ea8b6', p.snow) // 远山（青灰+雪）
    mist(x0, QW, p.far[0] - 8, 14, 0.35)
    ridge(x0, QW, p.mid[0], p.mid[1], '#647f74', p.snow * 0.5) // 中山（青绿）
    mist(x0, QW, p.mid[0] - 8, 13, 0.3)
    ridge(x0, QW, p.near[0], p.near[1], '#46604d', 0) // 近山（深绿）
    pineBand(x0, QW, 850, '#354b3c') // 山脚针叶林（两层）
    pineBand(x0, QW, 908, '#2c4033')
    if (p.extra === 'pagoda') pagoda(x0 + 1240, 856, 2)
    if (p.extra === 'city') citySil(x0 + 1900, 700)
    if (p.extra === 'birds') birds(x0 + 700, 560, 6)
    if (p.extra === 'broadleaf') broadleafBand(x0, QW, 862, '#3d5a41')
  })

  const t = new THREE.CanvasTexture(c)
  t.magFilter = THREE.NearestFilter
  t.minFilter = THREE.NearestMipmapLinearFilter
  t.anisotropy = 8
  t.wrapS = THREE.ClampToEdgeWrapping
  t.wrapT = THREE.ClampToEdgeWrapping
  t.colorSpace = THREE.SRGBColorSpace
  return t
}

// 围墙夜空叠加层（透明底；星星 + 月亮绘制在天底区，夜晚由材质 opacity 淡入，加色混合）
export function starOverlayTexture() {
  const c = document.createElement('canvas')
  c.width = 12288
  c.height = 1024
  const ctx = c.getContext('2d')
  const rnd = (n) => Math.floor(Math.random() * n)
  // 星星（天底区 y 140-760：覆盖各俯角可见的夜空带）
  for (let i = 0; i < 1200; i++) {
    ctx.fillStyle = ['#ffffff', '#dfe8ff', '#cfe0ff', '#ffeecf'][rnd(4)]
    const big = Math.random() < 0.1
    ctx.fillRect(rnd(12288), 140 + rnd(620), big ? 3 : 1, big ? 3 : 1)
  }
  // 亮星（十字星光）
  for (let i = 0; i < 26; i++) {
    const x = 80 + rnd(12120)
    const y = 170 + rnd(540)
    ctx.fillStyle = '#ffffff'
    ctx.fillRect(x - 2, y, 5, 1)
    ctx.fillRect(x, y - 2, 1, 5)
    ctx.fillStyle = '#eef4ff'
    ctx.fillRect(x, y, 1, 1)
  }
  // 块状像素月亮
  const mx = 8850
  const my = 430
  const R = 30
  const BL = 4
  ctx.fillStyle = '#f2f6e6'
  for (let yy = -R; yy < R; yy += BL) {
    for (let xx = -R; xx < R; xx += BL) {
      if (Math.hypot(xx + BL / 2, yy + BL / 2) <= R) ctx.fillRect(mx + xx, my + yy, BL, BL)
    }
  }
  ctx.fillStyle = '#cdd8bc'
  ctx.fillRect(mx - 17, my - 12, 12, 12)
  ctx.fillRect(mx + 7, my + 4, 13, 10)
  const t = new THREE.CanvasTexture(c)
  t.magFilter = THREE.NearestFilter
  t.minFilter = THREE.NearestFilter
  t.generateMipmaps = false
  t.wrapS = THREE.ClampToEdgeWrapping
  t.wrapT = THREE.ClampToEdgeWrapping
  t.colorSpace = THREE.SRGBColorSpace
  return t
}

// 像素小人精灵表：樊楼（金发校服少年）
// 4 方向 × 3 帧（待机/走1/走2）；单元格 26×38（含 1px 描边余量）
export function characterSheet() {
  const CW = 26
  const CH = 38
  const c = document.createElement('canvas')
  c.width = CW * 3
  c.height = CH * 4
  const ctx = c.getContext('2d')

  const K = {
    hair: '#eccb5a', hairD: '#c9a234', hairL: '#f6dd82',
    skin: '#f6dcae', skinD: '#e0c193',
    eyeW: '#f8f6ee', eye: '#2b3a58',
    coat: '#46567f', coatD: '#333f5e', coatL: '#55668f',
    shirt: '#f4f1e6', tie: '#b23a44', tieD: '#8c2c34',
    btn: '#d8d4c8', belt: '#2a2d3a', buckle: '#c9c2ae',
    pants: '#2e3550', pantsD: '#232838',
    shoe: '#23262e', shoeD: '#3a3f4a',
    line: '#262031',
  }

  // 画一个方向的一帧；r(x, y, w, h, color)
  function paint(dir, frame, r) {
    const side = dir === 'side'

    // ===== 双腿 =====
    if (side) {
      const fwd = frame === 1 ? 2 : 0
      const bwd = frame === 2 ? 2 : 0
      r(12 + fwd, 21, 4, 8, K.pants)
      r(8 - bwd, 21, 4, 8, K.pantsD)
      r(11 + fwd, 29, 6, 2, K.shoe)
      r(6 - bwd, 29, 6, 2, K.shoeD)
    } else {
      const liftA = frame === 1
      const liftB = frame === 2
      r(7, 21, 4, liftA ? 7 : 8, K.pants)
      r(12, 21, 4, liftB ? 7 : 8, K.pants)
      r(10, 21, 1, 8, K.pantsD)
      r(5, liftA ? 28 : 29, 6, 2, K.shoe)
      r(11, liftB ? 28 : 29, 6, 2, K.shoe)
    }

    // ===== 躯干（校服）=====
    if (side) {
      r(8, 9, 8, 4, K.coat)
      r(8, 13, 7, 5, K.coat)
      r(8, 18, 6, 3, K.coat)
      r(8, 9, 1, 12, K.coatD)
      r(14, 10, 3, 8, K.coat)
      r(14, 18, 3, 2, K.coatD)
      r(14, 20, 3, 2, K.skin)
      r(8, 19, 6, 2, K.belt)
    } else {
      r(5, 9, 13, 4, K.coat)
      r(6, 13, 11, 5, K.coat)
      r(7, 18, 9, 3, K.coat)
      r(5, 9, 1, 4, K.coatD)
      r(6, 13, 1, 5, K.coatD)
      r(7, 18, 1, 3, K.coatD)
      r(17, 9, 1, 4, K.coatL)
      r(16, 13, 1, 5, K.coatL)
      if (dir === 'up') {
        r(11, 9, 1, 10, K.coatD)
      } else {
        r(10, 9, 4, 5, K.shirt)
        r(11, 9, 2, 2, K.tie)
        r(11, 11, 2, 5, K.tie)
        r(11, 12, 1, 4, K.tieD)
        r(9, 9, 1, 3, K.coatD)
        r(14, 9, 1, 3, K.coatD)
        r(11, 17, 1, 1, K.btn)
      }
      r(7, 19, 9, 2, K.belt)
      r(11, 19, 2, 2, K.buckle)
      r(3, 9, 2, 8, K.coat)
      r(18, 9, 2, 8, K.coat)
      r(3, 17, 2, 2, K.coatD)
      r(18, 17, 2, 2, K.coatD)
      r(3, 19, 2, 2, K.skin)
      r(18, 19, 2, 2, K.skin)
    }

    // ===== 头 =====
    r(11, 8, 2, 1, K.skinD)
    if (dir === 'up') {
      r(8, 0, 8, 2, K.hair)
      r(7, 1, 10, 3, K.hair)
      r(6, 2, 12, 6, K.hair)
      r(6, 8, 12, 1, K.hairD)
      r(9, 1, 4, 1, K.hairL)
    } else if (side) {
      // 侧面头部收窄（头宽≈13px，与身体比例协调）
      r(6, 0, 8, 2, K.hair)
      r(5, 1, 8, 5, K.hair)
      r(4, 3, 4, 5, K.hair)
      r(11, 3, 6, 5, K.skin)
      r(11, 3, 6, 1, K.hair)
      r(13, 5, 1, 1, K.eyeW)
      r(13, 6, 1, 1, K.eye)
      r(16, 6, 1, 1, K.skinD)
      r(13, 7, 2, 1, K.skinD)
      r(6, 1, 3, 1, K.hairL)
    } else {
      r(8, 0, 8, 2, K.hair)
      r(7, 1, 10, 2, K.hair)
      r(6, 2, 12, 4, K.hair)
      r(6, 5, 2, 3, K.hair)
      r(16, 5, 2, 3, K.hair)
      r(9, 1, 4, 1, K.hairL)
      r(8, 4, 8, 5, K.skin)
      r(8, 4, 8, 1, K.hair)
      r(9, 4, 1, 1, K.hairD)
      r(11, 4, 1, 1, K.hairD)
      r(13, 4, 1, 1, K.hairD)
      r(15, 4, 1, 1, K.hairD)
      r(9, 5, 2, 1, K.hairD)
      r(13, 5, 2, 1, K.hairD)
      r(9, 6, 2, 2, K.eyeW)
      r(13, 6, 2, 2, K.eyeW)
      r(10, 6, 1, 2, K.eye)
      r(13, 6, 1, 2, K.eye)
      r(11, 7, 2, 1, K.skinD)
      r(11, 8, 2, 1, K.skinD)
    }
  }

  const rows = ['down', 'up', 'left', 'right']
  rows.forEach((dir, ri) => {
    for (let f = 0; f < 3; f++) {
      const ox = f * CW
      const oy = ri * CH
      const drawCell = () => {
        paint(dir === 'right' || dir === 'left' ? 'side' : dir, f, (x, y, w, h) => {
          ctx.fillStyle = K.line
          ctx.fillRect(x - 1, y - 1, w + 2, h + 2)
        })
        paint(dir === 'right' || dir === 'left' ? 'side' : dir, f, (x, y, w, h, col) => {
          ctx.fillStyle = col
          ctx.fillRect(x, y, w, h)
        })
      }
      if (dir === 'left') {
        ctx.save()
        ctx.translate(ox + CW - 1, oy + 1)
        ctx.scale(-1, 1)
        drawCell()
        ctx.restore()
      } else {
        ctx.save()
        ctx.translate(ox + 1, oy + 1)
        drawCell()
        ctx.restore()
      }
    }
  })

  const t = new THREE.CanvasTexture(c)
  t.magFilter = THREE.NearestFilter
  t.minFilter = THREE.NearestFilter
  t.generateMipmaps = false
  t.repeat.set(1 / 3, 1 / 4)
  t.offset.set(0, 3 / 4)
  t.colorSpace = THREE.SRGBColorSpace
  return t
}
