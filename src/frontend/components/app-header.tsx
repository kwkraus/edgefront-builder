'use client'

import { signOut, useSession } from 'next-auth/react'
import Link from 'next/link'

export default function AppHeader() {
  const { data: session } = useSession()

  return (
    <header className="border-b bg-background sticky top-0 z-30">
      <div className="mx-auto max-w-6xl px-4 py-3 flex items-center justify-between">
        <div className="flex items-center gap-6">
          <Link href="/series" className="font-semibold text-lg tracking-tight">
            EdgeFront Builder
          </Link>
          <Link
            href="/about"
            className="text-sm text-muted-foreground hover:text-foreground transition-colors"
          >
            About
          </Link>
        </div>
        {session && (
          <button
            onClick={() => signOut({ callbackUrl: '/login' })}
            className="text-sm text-muted-foreground hover:text-foreground transition-colors"
          >
            Sign out
          </button>
        )}
      </div>
    </header>
  )
}
