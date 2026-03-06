'use client'

import { SessionProvider } from 'next-auth/react'
import { ThemeProvider, BaseStyles } from '@primer/react'

export default function Providers({ children }: { children: React.ReactNode }) {
  return (
    <SessionProvider>
      <ThemeProvider>
        <BaseStyles>{children}</BaseStyles>
      </ThemeProvider>
    </SessionProvider>
  )
}
