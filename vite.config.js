import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  base: './', // 相对路径：配合 scripts/build-single.mjs 生成可双击打开的单文件
  plugins: [react()],
  server: {
    host: '127.0.0.1',
    port: 5173,
    open: false
  }
})
