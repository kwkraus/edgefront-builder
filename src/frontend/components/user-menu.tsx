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

  useEffect(() => {
    if (!session?.user) return
    let revoked = false

    fetch('/api/me/photo')
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
  }, [session?.user])

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
          }}
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
                backgroundColor: '#2563eb',
                fontSize: 10,
                fontWeight: 700,
                color: '#fff',
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
