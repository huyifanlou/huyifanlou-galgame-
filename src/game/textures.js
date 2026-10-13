import * as THREE from 'three'

// 程序化像素纹理工具：全部 NearestFilter，保持 HD-2D 的像素颗粒感
export function canvasTexture(w, h, draw, repeatX = 1, repeatY = 1) {
  const c = document.createElement('canvas')
  c.width = w
  c.height = h
  const ctx = c.getContext('2d')
  draw(ctx, w, h)
  const t = new THREE.CanvasTexture(c)
  t.magFilter = THREE.NearestFilter
  t.minFilter = THREE.NearestFilter
  t.wrapS = t.wrapT = THREE.RepeatWrapping
  t.repeat.set(repeatX, repeatY)
  t.colorSpace = THREE.SRGBColorSpace
  return t
}

const rand = (n) => Math.floor(Math.random() * n)

export function grassTexture() {
  return canvasTexture(16, 16, (ctx) => {
    for (let y = 0; y < 16; y++) {
      for (let x = 0; x < 16; x++) {
        ctx.fillStyle = (x + y) % 2 === 0 ? '#4f8f47' : '#477f40'
        ctx.fillRect(x, y, 1, 1)
      }
    }
    for (let i = 0; i < 26; i++) {
      ctx.fillStyle = '#3f7239'
      ctx.fillRect(rand(16), rand(16), 1, 1)
    }
  }, 34, 34)
}

export function pathTexture(horizontal = false) {
  return canvasTexture(16, 16, (ctx) => {
    ctx.fillStyle = '#b9b2a4'
    ctx.fillRect(0, 0, 16, 16)
    for (let i = 0; i < 40; i++) {
      ctx.fillStyle = ['#aca495', '#c4bdb0', '#a49c8d'][rand(3)]
      ctx.fillRect(rand(16), rand(16), 2, 2)
    }
    ctx.fillStyle = '#948c7d'
    ctx.fillRect(0, 7, 16, 1)
    ctx.fillRect(7, 0, 1, 16)
  }, horizontal ? 14 : 1, horizontal ? 1 : 14)
}

export function buildingTexture(wall, cols, rows) {
  const cell = 8
  return canvasTexture(cols * cell, (rows + 1) * cell, (ctx, w, h) => {
    ctx.fillStyle = wall
    ctx.fillRect(0, 0, w, h)
    ctx.fillStyle = 'rgba(0,0,0,0.15)'
    ctx.fillRect(0, h - 6, w, 6)
    for (let r = 0; r < rows; r++) {
      for (let c2 = 0; c2 < cols; c2++) {
        const lit = Math.random() < 0.32
        ctx.fillStyle = lit ? '#ffd98a' : '#2c3548'
        ctx.fillRect(c2 * cell + 2, r * cell + 3, 4, 4)
        ctx.fillStyle = 'rgba(0,0,0,0.35)'
        ctx.fillRect(c2 * cell + 2, r * cell + 3, 4, 1)
      }
    }
  })
}

export function signTexture(text, bg = '#3a2f4a') {
  return canvasTexture(224, 56, (ctx, w, h) => {
    ctx.fillStyle = bg
    ctx.fillRect(0, 0, w, h)
    ctx.strokeStyle = 'rgba(255,255,255,0.35)'
    ctx.strokeRect(2, 2, w - 4, h - 4)
    ctx.fillStyle = '#ffffff'
    ctx.font = 'bold 36px "Microsoft YaHei", sans-serif'
    ctx.textAlign = 'center'
    ctx.textBaseline = 'middle'
    ctx.fillText(text, w / 2, h / 2 + 2)
  })
}
