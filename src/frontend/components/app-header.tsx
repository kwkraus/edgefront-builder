'use client'

import { useSession } from 'next-auth/react'
import Link from 'next/link'
import { Header, IconButton } from '@primer/react'
import { SunIcon, MoonIcon } from '@primer/octicons-react'
import UserMenu from '@/components/user-menu'
import { useColorMode } from '@/components/color-mode-provider'

export default function AppHeader() {
  const { status } = useSession()
  const { mode, toggle } = useColorMode()

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
        <IconButton
          icon={mode === 'dark' ? SunIcon : MoonIcon}
          aria-label={`Switch to ${mode === 'dark' ? 'light' : 'dark'} mode`}
          variant="invisible"
          size="small"
          style={{ color: 'inherit' }}
          onClick={toggle}
        />
      </Header.Item>

      {status === 'authenticated' && (
        <Header.Item>
          <UserMenu />
        </Header.Item>
      )}
    </Header>
  )
}
