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
    ctx.fillStyle = '#a49c8d' // 勾缝底色
    ctx.fillRect(0, 0, 32, 32)
    for (let by = 0; by < 2; by++) {
      for (let bx = 0; bx < 2; bx++) {
        ctx.fillStyle = ['#c1b9ac', '#b8b0a3', '#bdb4a6', '#b3ab9d'][rand(4)]
        ctx.fillRect(bx * 16 + 1, by * 16 + 1, 14, 14)
        // 石板磨损与高光
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

// 建筑外墙：窗户带窗框 + 楼层线 + 底部阴影，细节 2 倍
export function buildingTexture(wall, cols, rows) {
  const cell = 10
  return canvasTexture(cols * cell, rows * cell + 14, (ctx, w, h) => {
    ctx.fillStyle = wall
    ctx.fillRect(0, 0, w, h)

    // 楼层分隔线
    for (let r = 1; r < rows; r++) {
      ctx.fillStyle = 'rgba(0,0,0,0.12)'
      ctx.fillRect(0, r * cell + 4, w, 1)
    }
    // 底部基座阴影
    ctx.fillStyle = 'rgba(0,0,0,0.18)'
    ctx.fillRect(0, h - 8, w, 8)
    ctx.fillStyle = 'rgba(255,255,255,0.06)'
    ctx.fillRect(0, 0, w, 2)

    // 窗户（带窗框：外框亮、内芯暗或亮灯）
    for (let r = 0; r < rows; r++) {
      for (let c2 = 0; c2 < cols; c2++) {
        const lit = Math.random() < 0.3
        const wx = c2 * cell + 2
        const wy = r * cell + 3
        ctx.fillStyle = 'rgba(0,0,0,0.28)' // 窗框阴影
        ctx.fillRect(wx, wy, 6, 6)
        ctx.fillStyle = lit ? '#ffd98a' : '#2c3548'
        ctx.fillRect(wx + 1, wy + 1, 4, 4)
        ctx.fillStyle = lit ? '#ffe9b8' : '#3d4a63' // 内侧高光
        ctx.fillRect(wx + 1, wy + 1, 4, 1)
        ctx.fillStyle = lit ? '#e8b96a' : '#1f2836' // 内侧阴影
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
