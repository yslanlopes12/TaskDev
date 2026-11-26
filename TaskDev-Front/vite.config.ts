import { fileURLToPath, URL } from 'node:url'
import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'
import { VitePWA } from 'vite-plugin-pwa'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const apiTarget = env.VITE_API_TARGET || env.VITE_API_URL || 'http://localhost:5092'
  const devPort = Number(env.VITE_PORT) || 5173

  return {
    plugins: [
      vue(),
      tailwindcss(),
      VitePWA({
        registerType: 'autoUpdate',
        includeAssets: ['favicon.ico', 'apple-touch-icon.png', 'icons/*.png'],
        manifest: {
          name: 'Task.dev',
          short_name: 'Task.dev',
          description: 'Gerenciamento de tarefas para desenvolvedores',
          theme_color: '#a69aeb',
          background_color: '#101114',
          display: 'standalone',
          orientation: 'portrait',
          start_url: '/',
          icons: [
            { src: 'icons/icon-192.png', sizes: '192x192', type: 'image/png' },
            { src: 'icons/icon-512.png', sizes: '512x512', type: 'image/png' },
            { src: 'icons/icon-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
          ],
        },
        workbox: {
          globPatterns: ['**/*.{js,css,html,ico,png,svg,woff2}'],
          // Tutorial screenshots are loaded on demand, not during PWA installation.
          globIgnores: ['docs/screens/**'],
          navigateFallbackDenylist: [/^\/api(?:\/|\?|$)/i],
        },
      }),
    ],
    resolve: {
      alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
    },
    server: {
      port: devPort,
      proxy: {
        '/api': {
          target: apiTarget,
          changeOrigin: true,
          secure: false,
          configure: (proxy) => {
            proxy.on('error', (err) => {
              console.warn(
                `[vite proxy warning] Não foi possível conectar ao backend em ${apiTarget}. Certifique-se de que a API ASP.NET Core está em execução. Erro:`,
                err.message,
              )
            })
          },
        },
      },
    },
  }
})
