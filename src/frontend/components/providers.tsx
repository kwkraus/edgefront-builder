'use client'

import { SessionProvider } from 'next-auth/react'
import { ThemeProvider, BaseStyles } from '@primer/react'
import ColorModeProvider, { useColorMode } from '@/components/color-mode-provider'

function PrimerTheme({ children }: { children: React.ReactNode }) {
  const { mode } = useColorMode()
  return (
    <ThemeProvider colorMode={mode === 'dark' ? 'night' : 'day'}>
      <BaseStyles>{children}</BaseStyles>
    </ThemeProvider>
  )
}

export default function Providers({ children }: { children: React.ReactNode }) {
  return (
    <SessionProvider>
      <ColorModeProvider>
        <PrimerTheme>{children}</PrimerTheme>
      </ColorModeProvider>
    </SessionProvider>
  )
}
