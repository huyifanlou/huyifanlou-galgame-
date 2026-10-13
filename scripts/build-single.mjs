// 构建后处理：把 dist 打成"单文件 HTML"（双击即可离线打开，无需开发服务器）。
// 原理：Vite 产物是 ES module 脚本 + 外部资源引用，浏览器在 file:// 下不允许跨文件加载；
// 把脚本与样式全部内联进 index.html 后，单文件即可直接运行。
import { readFileSync, writeFileSync, existsSync } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'

const root = join(dirname(fileURLToPath(import.meta.url)), '..')
const dist = join(root, 'dist')
const indexPath = join(dist, 'index.html')

let html = readFileSync(indexPath, 'utf8')

const toFsPath = (ref) => join(dist, decodeURIComponent(ref).replace(/^\.?\//, ''))

// 1) 内联 module 脚本（全量打包、无外部 import，file:// 下可直接执行）
html = html.replace(
  /<script type="module"[^>]*src="([^"]+)"[^>]*><\/script>/g,
  (_, src) => {
    const file = toFsPath(src)
    if (!existsSync(file)) throw new Error('missing script: ' + src)
    const code = readFileSync(file, 'utf8').replace(/<\/script>/g, '<\\/script>')
    return `<script type="module">\n${code}\n</script>`
  }
)

// 2) 内联样式表
html = html.replace(
  /<link rel="stylesheet"[^>]*href="([^"]+)"[^>]*>/g,
  (_, href) => {
    const file = toFsPath(href)
    if (!existsSync(file)) throw new Error('missing stylesheet: ' + href)
    return `<style>\n${readFileSync(file, 'utf8')}\n</style>`
  }
)

// 3) 单文件内不需要 modulepreload
html = html.replace(/<link rel="modulepreload"[^>]*>/g, '')

writeFileSync(indexPath, html)
console.log('[build-single] 单文件已生成: dist/index.html（' + (html.length / 1024).toFixed(0) + ' KB，双击即玩）')
