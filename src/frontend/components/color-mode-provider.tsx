'use client'

import { createContext, useCallback, useContext, useSyncExternalStore } from 'react'

type ColorMode = 'light' | 'dark'
const STORAGE_KEY = 'color-mode'

// Module-level external store for color mode
const listeners = new Set<() => void>()

function subscribe(callback: () => void): () => void {
  listeners.add(callback)
  return () => listeners.delete(callback)
}

function getSnapshot(): ColorMode {
  const stored = localStorage.getItem(STORAGE_KEY)
  if (stored === 'dark' || stored === 'light') return stored
  return (document.documentElement.getAttribute('data-color-mode') as ColorMode) ?? 'light'
}

// Server snapshot always returns 'light' to match SSR output and avoid hydration mismatches.
function getServerSnapshot(): ColorMode {
  return 'light'
}

function applyMode(mode: ColorMode): void {
  document.documentElement.setAttribute('data-color-mode', mode)
  localStorage.setItem(STORAGE_KEY, mode)
  listeners.forEach(cb => cb())
}

interface ColorModeContextValue {
  mode: ColorMode
  toggle: () => void
}

const ColorModeContext = createContext<ColorModeContextValue>({
  mode: 'light',
  toggle: () => {},
})

export function useColorMode() {
  return useContext(ColorModeContext)
}

export default function ColorModeProvider({ children }: { children: React.ReactNode }) {
  const mode = useSyncExternalStore(subscribe, getSnapshot, getServerSnapshot)

  const toggle = useCallback(() => {
    applyMode(mode === 'dark' ? 'light' : 'dark')
  }, [mode])

  return (
    <ColorModeContext.Provider value={{ mode, toggle }}>
      {children}
    </ColorModeContext.Provider>
  )
}
