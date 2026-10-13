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
  t.minFilter = THREE.LinearMipmapLinearFilter
  t.wrapS = t.wrapT = THREE.RepeatWrapping
  t.repeat.set(repeatX, repeatY)
  t.colorSpace = THREE.SRGBColorSpace
  return t
}

const rand = (n) => Math.floor(Math.random() * n)

// 32×32 草地：双层棋盘 + 草叶细节
export function grassTexture() {
  return canvasTexture(32, 32, (ctx) => {
    for (let y = 0; y < 32; y++) {
      for (let x = 0; x < 32; x++) {
        ctx.fillStyle = (x + y) % 2 === 0 ? '#54904b' : '#4a8743'
        ctx.fillRect(x, y, 1, 1)
      }
    }
    for (let i = 0; i < 90; i++) {
      ctx.fillStyle = ['#3f7239', '#5da054', '#437c3c', '#61a85a'][rand(4)]
      ctx.fillRect(rand(32), rand(32), 1, 2)
    }
  }, 30, 30)
}

// 32×32 石板路：2×2 石板 + 勾缝 + 磨损
export function pathTexture(horizontal = false) {
  return canvasTexture(32, 32, (ctx) => {
    ctx.fillStyle = '#a49c8d'
    ctx.fillRect(0, 0, 32, 32)
    for (let by = 0; by < 2; by++) {
      for (let bx = 0; bx < 2; bx++) {
        ctx.fillStyle = ['#c1b9ac', '#b8b0a3', '#bdb4a6', '#b3ab9d'][rand(4)]
        ctx.fillRect(bx * 16 + 1, by * 16 + 1, 14, 14)
        for (let i = 0; i < 5; i++) {
          ctx.fillStyle = 'rgba(122,114,102,0.45)'
          ctx.fillRect(bx * 16 + 1 + rand(14), by * 16 + 1 + rand(14), 2, 1)
        }
        for (let i = 0; i < 3; i++) {
          ctx.fillStyle = 'rgba(230,226,216,0.5)'
          ctx.fillRect(bx * 16 + 1 + rand(14), by * 16 + 1 + rand(14), 1, 1)
        }
      }
    }
  }, horizontal ? 14 : 1, horizontal ? 1 : 14)
}

// 建筑外墙：窗户带窗框 + 楼层线 + 基座阴影
export function buildingTexture(wall, cols, rows) {
  const cell = 10
  return canvasTexture(cols * cell, rows * cell + 14, (ctx, w, h) => {
    ctx.fillStyle = wall
    ctx.fillRect(0, 0, w, h)

    for (let r = 1; r < rows; r++) {
      ctx.fillStyle = 'rgba(0,0,0,0.12)'
      ctx.fillRect(0, r * cell + 4, w, 1)
    }
    ctx.fillStyle = 'rgba(0,0,0,0.18)'
    ctx.fillRect(0, h - 8, w, 8)
    ctx.fillStyle = 'rgba(255,255,255,0.06)'
    ctx.fillRect(0, 0, w, 2)

    for (let r = 0; r < rows; r++) {
      for (let c2 = 0; c2 < cols; c2++) {
        const lit = Math.random() < 0.3
        const wx = c2 * cell + 2
        const wy = r * cell + 3
        ctx.fillStyle = 'rgba(0,0,0,0.28)'
        ctx.fillRect(wx, wy, 6, 6)
        ctx.fillStyle = lit ? '#ffd98a' : '#2c3548'
        ctx.fillRect(wx + 1, wy + 1, 4, 4)
        ctx.fillStyle = lit ? '#ffe9b8' : '#3d4a63'
        ctx.fillRect(wx + 1, wy + 1, 4, 1)
        ctx.fillStyle = lit ? '#e8b96a' : '#1f2836'
        ctx.fillRect(wx + 1, wy + 4, 4, 1)
      }
    }
  })
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
export function flowerTexture() {
  return canvasTexture(32, 32, (ctx) => {
    for (let y = 0; y < 32; y++) {
      for (let x = 0; x < 32; x++) {
        ctx.fillStyle = (x + y) % 2 === 0 ? '#54904b' : '#4a8743'
        ctx.fillRect(x, y, 1, 1)
      }
    }
    const colors = ['#e8748a', '#f2d857', '#f4f2ea', '#c58ae0']
    for (let i = 0; i < 26; i++) {
      const x = 1 + rand(30)
      const y = 1 + rand(30)
      ctx.fillStyle = colors[rand(colors.length)]
      ctx.fillRect(x, y, 2, 2)
      ctx.fillStyle = '#3f7239'
      ctx.fillRect(x, y + 2, 1, 1)
    }
  })
}

// 操场：圆环跑道 + 内场 + 分道线 + 中线圆
export function trackTexture() {
  return canvasTexture(256, 160, (ctx, w, h) => {
    for (let y = 0; y < h; y += 4) {
      for (let x = 0; x < w; x += 4) {
        ctx.fillStyle = (x + y) % 8 === 0 ? '#4d8a45' : '#548f4b'
        ctx.fillRect(x, y, 4, 4)
      }
    }
    const rr = (x, y, ww, hh, r) => {
      ctx.beginPath()
      ctx.roundRect(x, y, ww, hh, r)
    }
    rr(6, 6, w - 12, h - 12, 46)
    ctx.fillStyle = '#b26240'
    ctx.fill()
    rr(24, 24, w - 48, h - 48, 32)
    ctx.fillStyle = '#5b9c51'
    ctx.fill()
    ctx.strokeStyle = 'rgba(244,240,226,0.85)'
    ctx.lineWidth = 2
    rr(12, 12, w - 24, h - 24, 40)
    ctx.stroke()
    rr(18, 18, w - 36, h - 36, 36)
    ctx.stroke()
    // 内场中线与中圈
    ctx.beginPath()
    ctx.moveTo(w / 2, 28)
    ctx.lineTo(w / 2, h - 28)
    ctx.stroke()
    ctx.beginPath()
    ctx.arc(w / 2, h / 2, 14, 0, Math.PI * 2)
    ctx.stroke()
  })
}

// 像素小人精灵表：樊楼（金发校服少年）4 方向 × 3 帧（待机/走1/走2）
// 单元格 24×32；字符：h 金发 / H 深金 / s 皮肤 / S 皮肤暗 / e 眼 / c 外套 / C 外套暗 / w 衬衫 / t 领带 / p 裤 / k 鞋
export function characterSheet() {
  const CW = 24
  const CH = 32
  const FULL_W = CW * 3
  const FULL_H = CH * 4
  const c = document.createElement('canvas')
  c.width = FULL_W
  c.height = FULL_H
  const ctx = c.getContext('2d')

  const COL = {
    hair: '#eccb5a', hairD: '#c9a234', skin: '#f5d9a8', skinD: '#dcbc8e',
    eye: '#2b3a58', coat: '#46567f', coatD: '#333f5e', shirt: '#f4f1e6',
    tie: '#b23a44', pants: '#2e3550', shoe: '#262932',
  }

  function draw(dir, frame, ox, oy) {
    const r = (x, y, w, h, col) => {
      ctx.fillStyle = col
      ctx.fillRect(ox + x, oy + y, w, h)
    }

    // —— 头 ——
    if (dir === 'up') {
      r(6, 1, 12, 2, COL.hair)
      r(4, 3, 16, 6, COL.hair)
      r(6, 4, 12, 1, COL.hairD)
      r(4, 7, 3, 3, COL.hair)
      r(17, 7, 3, 3, COL.hair)
    } else if (dir === 'side') {
      r(5, 1, 12, 2, COL.hair)
      r(4, 3, 11, 6, COL.hair)
      r(4, 7, 4, 3, COL.hair)
      r(12, 3, 7, 5, COL.skin)
      r(12, 3, 7, 1, COL.hair)
      r(16, 5, 2, 2, COL.eye)
    } else {
      r(6, 1, 12, 2, COL.hair)
      r(4, 3, 16, 4, COL.hair)
      r(4, 6, 3, 3, COL.hair)
      r(17, 6, 3, 3, COL.hair)
      r(7, 3, 10, 5, COL.skin)
      r(7, 3, 10, 1, COL.hair)
      r(8, 5, 2, 2, COL.eye)
      r(14, 5, 2, 2, COL.eye)
      r(11, 7, 2, 1, COL.skinD)
    }

    // —— 颈与躯干 ——
    r(11, 8, 2, 1, COL.skinD)
    if (dir === 'side') {
      r(6, 9, 10, 8, COL.coat)
      r(6, 9, 10, 1, COL.coatD)
      r(14, 10, 3, 6, COL.coatD)
      r(14, 16, 3, 2, COL.skin)
      r(6, 9, 1, 8, COL.coatD)
    } else {
      r(7, 9, 10, 8, COL.coat)
      r(7, 9, 1, 8, COL.coatD)
      r(16, 9, 1, 8, COL.coatD)
      // 手臂
      r(5, 9, 2, 8, COL.coat)
      r(17, 9, 2, 8, COL.coat)
      r(5, 16, 2, 2, COL.skin)
      r(17, 16, 2, 2, COL.skin)
      if (dir === 'up') {
        r(11, 9, 2, 7, COL.coatD)
      } else {
        r(11, 9, 2, 6, COL.shirt)
        r(11, 9, 2, 3, COL.tie)
      }
    }

    // —— 腿（frame 1/2 交替抬腿）——
    if (dir === 'side') {
      const frontLift = frame === 1
      const backLift = frame === 2
      r(12, 17, 3, frontLift ? 5 : 6, COL.pants)
      r(8, 17, 3, backLift ? 5 : 6, '#232a42')
      r(11, frontLift ? 22 : 23, 4, 2, COL.shoe)
      r(7, backLift ? 22 : 23, 4, 2, COL.shoe)
    } else {
      const leftLift = frame === 1
      const rightLift = frame === 2
      r(8, 17, 3, leftLift ? 5 : 6, COL.pants)
      r(13, 17, 3, rightLift ? 5 : 6, COL.pants)
      r(7, leftLift ? 22 : 23, 4, 2, COL.shoe)
      r(13, rightLift ? 22 : 23, 4, 2, COL.shoe)
    }
  }

  const rows = ['down', 'up', 'left', 'right']
  rows.forEach((dir, r) => {
    for (let f = 0; f < 3; f++) {
      const ox = f * CW
      const oy = r * CH
      if (dir === 'left') {
        ctx.save()
        ctx.translate(ox + CW, oy)
        ctx.scale(-1, 1)
        draw('side', f, 0, 0)
        ctx.restore()
      } else {
        draw(dir === 'right' ? 'side' : dir, f, ox, oy)
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
