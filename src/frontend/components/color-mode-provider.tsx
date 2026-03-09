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

export default function ColorModeProvider({ children }: { children: React.ReactNode }) {
  const [mode, setMode] = useState<ColorMode>('light')
  const [isInitialized, setIsInitialized] = useState(false)

  useEffect(() => {
    const stored = localStorage.getItem(STORAGE_KEY)
    const initialMode = stored === 'dark' || stored === 'light'
      ? stored
      : ((document.documentElement.getAttribute('data-color-mode') as ColorMode) ?? 'light')

    setMode(initialMode)
    setIsInitialized(true)
  }, [])

  useEffect(() => {
    if (!isInitialized) return

    document.documentElement.setAttribute('data-color-mode', mode)
    localStorage.setItem(STORAGE_KEY, mode)
  }, [isInitialized, mode])

  const toggle = useCallback(() => {
    setMode((prev) => (prev === 'dark' ? 'light' : 'dark'))
  }, [])

  return (
    <ColorModeContext value={{ mode, toggle }}>
      {children}
    </ColorModeContext>
  )
}
