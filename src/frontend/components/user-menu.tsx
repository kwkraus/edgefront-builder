'use client'

import { useEffect, useState } from 'react'
import { signOut, useSession } from 'next-auth/react'
import { SignOutIcon } from '@primer/octicons-react'
import { ActionList, ActionMenu, Avatar, Text } from '@primer/react'

function getInitials(name: string | null | undefined): string {
  if (!name) return '?'
  const parts = name.trim().split(/\s+/)
  if (parts.length === 1) return parts[0][0].toUpperCase()
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
}

export default function UserMenu() {
  const { data: session } = useSession()
  const [photoUrl, setPhotoUrl] = useState<string | null>(null)
  const [anchorFocused, setAnchorFocused] = useState(false)

  useEffect(() => {
    if (!session?.user || !session?.accessToken) return
    let revoked = false

    const baseUrl = process.env.NEXT_PUBLIC_BACKEND_API_BASE_URL ?? 'http://localhost:5000'
    fetch(`${baseUrl}/api/v1/me/photo`, {
      headers: { Authorization: `Bearer ${session.accessToken}` },
    })
      .then((res) => {
        if (!res.ok) return null
        return res.blob()
      })
      .then((blob) => {
        if (blob && !revoked) {
          setPhotoUrl(URL.createObjectURL(blob))
        }
      })
      .catch(() => {})

    return () => {
      revoked = true
      setPhotoUrl((prev) => {
        if (prev) URL.revokeObjectURL(prev)
        return null
      })
    }
  }, [session?.user, session?.accessToken])

  if (!session?.user) return null

  const { name, email } = session.user

  return (
    <ActionMenu>
      <ActionMenu.Anchor>
        <button
          type="button"
          aria-label={`User menu for ${name ?? 'current user'}`}
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '0.5rem',
            background: 'none',
            border: 'none',
            padding: '4px',
            cursor: 'pointer',
            color: 'inherit',
            lineHeight: 1,
            borderRadius: 'var(--borderRadius-medium, 6px)',
            outline: 'none',
            boxShadow: anchorFocused
              ? '0 0 0 2px var(--borderColor-focus, var(--color-accent-fg))'
              : 'none',
          }}
          onFocus={() => setAnchorFocused(true)}
          onBlur={() => setAnchorFocused(false)}
        >
          {photoUrl ? (
            <Avatar src={photoUrl} size={24} square={false} alt={name ?? 'User avatar'} />
          ) : (
            <span
              style={{
                display: 'flex',
                height: 24,
                width: 24,
                flexShrink: 0,
                alignItems: 'center',
                justifyContent: 'center',
                borderRadius: '50%',
                backgroundColor: 'var(--bgColor-accent-emphasis, var(--color-accent-emphasis))',
                fontSize: 10,
                fontWeight: 700,
                color: 'var(--fgColor-onEmphasis, var(--color-fg-on-emphasis))',
              }}
              aria-hidden="true"
            >
              {getInitials(name)}
            </span>
          )}
          <span style={{ fontSize: '0.875rem', fontWeight: 600, whiteSpace: 'nowrap' }}>
            {name}
          </span>
        </button>
      </ActionMenu.Anchor>

      <ActionMenu.Overlay width="medium" align="end">
        <ActionList>
          <ActionList.Group>
            <ActionList.GroupHeading className="px-3 py-2">
              <Text as="span" weight="semibold" size="small">
                {name}
              </Text>
              {email && (
                <Text as="p" size="small" className="m-0 mt-1 text-[color:var(--fgColor-muted)]">
                  {email}
                </Text>
              )}
            </ActionList.GroupHeading>
          </ActionList.Group>

          <ActionList.Divider />

          <ActionList.Item onSelect={() => signOut({ callbackUrl: '/login' })}>
            <ActionList.LeadingVisual>
              <SignOutIcon />
            </ActionList.LeadingVisual>
            Sign out
          </ActionList.Item>
        </ActionList>
      </ActionMenu.Overlay>
    </ActionMenu>
  )
}
