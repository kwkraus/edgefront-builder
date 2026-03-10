'use client'

import { createContext, useCallback, useContext, useEffect, useState } from 'react'

type ColorMode = 'light' | 'dark'
const STORAGE_KEY = 'color-mode'

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

function getInitialMode(): ColorMode {
  if (typeof window === 'undefined') return 'light'
  const stored = localStorage.getItem(STORAGE_KEY)
  if (stored === 'dark' || stored === 'light') return stored
  return (document.documentElement.getAttribute('data-color-mode') as ColorMode) ?? 'light'
}

export default function ColorModeProvider({ children }: { children: React.ReactNode }) {
  // Always initialize to 'light' to match SSR output and avoid hydration mismatches.
  // The stored preference is applied client-side in a useEffect after mount.
  const [mode, setMode] = useState<ColorMode>('light')

  useEffect(() => {
    setMode(getInitialMode())
  }, [])

  useEffect(() => {
    document.documentElement.setAttribute('data-color-mode', mode)
    localStorage.setItem(STORAGE_KEY, mode)
  }, [mode])

  const toggle = useCallback(() => {
    setMode((prev) => (prev === 'dark' ? 'light' : 'dark'))
  }, [])

  return (
    <ColorModeContext.Provider value={{ mode, toggle }}>
      {children}
    </ColorModeContext.Provider>
  )
}
