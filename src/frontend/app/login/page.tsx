'use client'

import { Suspense } from 'react'
import { useSearchParams } from 'next/navigation'
import { signIn } from 'next-auth/react'
import { ChromeIcon } from 'lucide-react'

function LoginForm() {
  const searchParams = useSearchParams()
  const callbackUrl = searchParams.get('callbackUrl') || '/series'

  return (
    <button
      type="button"
      onClick={() => signIn('azure-ad', { callbackUrl })}
      className="inline-flex w-full items-center justify-center gap-3 rounded-md border bg-white px-4 py-2.5 text-sm font-medium text-gray-700 shadow-sm hover:bg-gray-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-ring transition-colors"
    >
      <ChromeIcon className="size-5 text-blue-600" aria-hidden="true" />
      Sign in with Microsoft
    </button>
  )
}

export default function LoginPage() {
  return (
    <div className="flex min-h-[calc(100vh-4rem)] items-center justify-center">
      <div className="w-full max-w-sm space-y-6 rounded-xl border bg-card px-8 py-10 shadow-sm text-center">
        <div className="space-y-2">
          <h1 className="text-2xl font-bold tracking-tight">EdgeFront Builder</h1>
          <p className="text-sm text-muted-foreground">
            Sign in to manage your webinar series
          </p>
        </div>

        <Suspense
          fallback={
            <button
              type="button"
              disabled
              className="inline-flex w-full items-center justify-center gap-3 rounded-md border bg-white px-4 py-2.5 text-sm font-medium text-gray-700 shadow-sm opacity-50"
            >
              <ChromeIcon className="size-5 text-blue-600" aria-hidden="true" />
              Sign in with Microsoft
            </button>
          }
        >
          <LoginForm />
        </Suspense>

        <p className="text-xs text-muted-foreground">
          Requires an active Entra ID account with access to EdgeFront Builder.
        </p>
      </div>
    </div>
  )
}
