import type { Config } from 'tailwindcss'

export default {
  content: [
    './index.html',
    './src/**/*.{vue,ts,tsx}',
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        // Task.dev design system
        bg: '#101114',
        sidebar: '#141518',
        panel: '#191a1e',
        'panel-2': '#202126',
        hover: '#24252b',
        line: '#2b2c33',
        'line-soft': '#24252b',
        fg: '#e9e9ec',
        dim: '#a4a5ad',
        muted: '#777982',
        accent: '#a69aeb',
        'accent-bg': '#302a45',
        green: '#9cd9b7',
        yellow: '#edc887',
        red: '#e9a0a7',
        blue: '#a3bee9',
      },
      fontFamily: {
        sans: ['Inter', 'ui-sans-serif', 'system-ui', '-apple-system', 'Segoe UI', 'sans-serif'],
        mono: ['ui-monospace', 'SFMono-Regular', 'Consolas', 'monospace'],
      },
      borderRadius: {
        DEFAULT: '10px',
      },
    },
  },
  plugins: [],
} satisfies Config
