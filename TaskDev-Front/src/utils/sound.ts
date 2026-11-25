/**
 * Gerador de efeitos sonoros minimalistas e fluidos utilizando a Web Audio API nativa.
 * Zero dependências externas e latência nula.
 */
class SoundFx {
  private ctx: AudioContext | null = null

  private getContext(): AudioContext | null {
    if (!this.ctx && typeof window !== 'undefined') {
      const AudioCtx = window.AudioContext || (window as any).webkitAudioContext
      if (AudioCtx) {
        this.ctx = new AudioCtx()
      }
    }
    if (this.ctx && this.ctx.state === 'suspended') {
      this.ctx.resume().catch(() => {})
    }
    return this.ctx
  }

  /**
   * Toca um efeito suave e agradável de arpejo de XP (+XP ganho)
   */
  playXpGain() {
    try {
      const ctx = this.getContext()
      if (!ctx) return
      const now = ctx.currentTime
      const osc = ctx.createOscillator()
      const gain = ctx.createGain()

      osc.type = 'sine'
      // Arpejo pentatônico ascendente cristalino
      osc.frequency.setValueAtTime(523.25, now) // C5
      osc.frequency.exponentialRampToValueAtTime(659.25, now + 0.06) // E5
      osc.frequency.exponentialRampToValueAtTime(783.99, now + 0.12) // G5

      gain.gain.setValueAtTime(0.08, now)
      gain.gain.exponentialRampToValueAtTime(0.001, now + 0.28)

      osc.connect(gain)
      gain.connect(ctx.destination)

      osc.start(now)
      osc.stop(now + 0.3)
    } catch {
      // Ignora silenciosamente se o áudio estiver desativado pelo navegador
    }
  }

  /**
   * Toca um acorde triunfante de subida de nível / novo rank
   */
  playLevelUp() {
    try {
      const ctx = this.getContext()
      if (!ctx) return
      const now = ctx.currentTime
      const notes = [523.25, 659.25, 783.99, 1046.50] // C5, E5, G5, C6
      notes.forEach((freq, i) => {
        const osc = ctx.createOscillator()
        const gain = ctx.createGain()
        osc.type = 'triangle'
        osc.frequency.setValueAtTime(freq, now + i * 0.05)
        const start = now + i * 0.05
        gain.gain.setValueAtTime(0.12, start)
        gain.gain.exponentialRampToValueAtTime(0.001, start + 0.45)
        osc.connect(gain)
        gain.connect(ctx.destination)
        osc.start(start)
        osc.stop(start + 0.5)
      })
    } catch {
      // Ignora silenciosamente se o áudio estiver desativado
    }
  }
}

export const soundFx = new SoundFx()
