'use client'

import { useCallback, useState } from 'react'
import { useSession } from 'next-auth/react'
import Link from 'next/link'
import { Header, ActionMenu, ActionList, IconButton } from '@primer/react'
import { SunIcon, MoonIcon } from '@primer/octicons-react'
import UserMenu from '@/components/user-menu'

type ColorMode = 'light' | 'dark' | 'auto'

function useColorMode() {
  const [mode, setMode] = useState<ColorMode>(() => {
    if (typeof document === 'undefined') return 'auto'
    return (document.body.getAttribute('data-color-mode') as ColorMode) ?? 'auto'
  })

  const applyMode = useCallback((next: ColorMode) => {
    document.body.setAttribute('data-color-mode', next)
    setMode(next)
  }, [])

  return { mode, applyMode } as const
}

const colorModeLabels: Record<ColorMode, string> = {
  light: 'Light',
  dark: 'Dark',
  auto: 'System',
}

export default function AppHeader() {
  const { status } = useSession()
  const { mode, applyMode } = useColorMode()

  return (
    <Header style={{ position: 'sticky', top: 0, zIndex: 30, width: '100%' }}>
      <Header.Item>
        <Header.Link
          as={Link}
          href="/series"
          style={{ fontSize: '1rem', fontWeight: 600 }}
        >
          EdgeFront Builder
        </Header.Link>
      </Header.Item>

      <Header.Item>
        <Header.Link as={Link} href="/about" style={{ fontSize: '0.875rem' }}>
          About
        </Header.Link>
      </Header.Item>

      <Header.Item full />

      <Header.Item>
        <ActionMenu>
          <ActionMenu.Anchor>
            <IconButton
              icon={mode === 'dark' ? MoonIcon : SunIcon}
              aria-label={`Color mode: ${colorModeLabels[mode]}`}
              variant="invisible"
              size="small"
              style={{ color: 'inherit' }}
            />
          </ActionMenu.Anchor>
          <ActionMenu.Overlay align="end">
            <ActionList selectionVariant="single">
              {(['light', 'dark', 'auto'] as const).map((option) => (
                <ActionList.Item
                  key={option}
                  selected={mode === option}
                  onSelect={() => applyMode(option)}
                >
                  {colorModeLabels[option]}
                </ActionList.Item>
              ))}
            </ActionList>
          </ActionMenu.Overlay>
        </ActionMenu>
      </Header.Item>

      {status === 'authenticated' && (
        <Header.Item>
          <UserMenu />
        </Header.Item>
      )}
    </Header>
  )
}
