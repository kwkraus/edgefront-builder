'use client'

import { Suspense } from 'react'
import { useSearchParams } from 'next/navigation'
import { signIn } from 'next-auth/react'
import { Button } from '@primer/react'

/**
 * Inline Microsoft logo – four coloured squares matching the official brand.
 * Replaces the previous `ChromeIcon` from lucide-react.
 */
function MicrosoftIcon(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 21 21"
      fill="none"
      aria-hidden="true"
      {...props}
    >
      <rect x="1" y="1" width="9" height="9" fill="#F25022" />
      <rect x="11" y="1" width="9" height="9" fill="#7FBA00" />
      <rect x="1" y="11" width="9" height="9" fill="#00A4EF" />
      <rect x="11" y="11" width="9" height="9" fill="#FFB900" />
    </svg>
  )
}

const cardStyle: React.CSSProperties = {
  borderWidth: 'var(--borderWidth-thin, 1px)',
  borderStyle: 'solid',
  borderColor: 'var(--borderColor-default, var(--color-border-default))',
  borderRadius: 'var(--borderRadius-large, 12px)',
  backgroundColor: 'var(--bgColor-default, var(--color-canvas-default))',
  boxShadow: 'var(--shadow-resting-small, 0 1px 2px rgba(0,0,0,.04))',
}

const mutedTextStyle: React.CSSProperties = {
  color: 'var(--fgColor-muted, var(--color-fg-muted))',
}

function LoginForm() {
  const searchParams = useSearchParams()
  const callbackUrl = searchParams.get('callbackUrl') || '/series'

  return (
    <Button
      variant="default"
      size="large"
      block
      leadingVisual={() => <MicrosoftIcon />}
      onClick={() => signIn('azure-ad', { callbackUrl })}
    >
      Sign in with Microsoft
    </Button>
  )
}

export default function LoginPage() {
  return (
    <div className="flex min-h-[calc(100vh-4rem)] items-center justify-center">
      <div
        className="w-full max-w-sm space-y-6 px-8 py-10 text-center"
        style={cardStyle}
      >
        <div className="space-y-2">
          <h1 className="text-2xl font-bold tracking-tight">EdgeFront Builder</h1>
          <p className="text-sm" style={mutedTextStyle}>
            Sign in to manage your webinar series
          </p>
        </div>

        <Suspense
          fallback={
            <Button
              variant="default"
              size="large"
              block
              disabled
              leadingVisual={() => <MicrosoftIcon />}
            >
              Sign in with Microsoft
            </Button>
          }
        >
          <LoginForm />
        </Suspense>

        <p className="text-xs" style={mutedTextStyle}>
          Requires an active Entra ID account with access to EdgeFront Builder.
        </p>
      </div>
    </div>
  )
}
