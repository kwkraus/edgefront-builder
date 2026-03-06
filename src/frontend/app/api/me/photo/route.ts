import { NextResponse } from 'next/server'
import { getToken } from 'next-auth/jwt'
import type { NextRequest } from 'next/server'

/**
 * GET /api/me/photo
 *
 * Proxies the user's Entra ID profile photo from Microsoft Graph.
 * The next-auth access_token is scoped to the backend API, so we use the
 * refresh_token to acquire a Graph-scoped token, then fetch the photo binary
 * and return it directly.
 *
 * Returns 204 No Content if the user has no photo set.
 */
export async function GET(request: NextRequest) {
  // Validate required Azure AD environment variables up-front so misconfiguration
  // produces a diagnosable 500 rather than a cryptic runtime error.
  const tenantId = process.env.AZURE_AD_TENANT_ID
  const clientId = process.env.AZURE_AD_CLIENT_ID
  const clientSecret = process.env.AZURE_AD_CLIENT_SECRET
  if (!tenantId || !clientId || !clientSecret) {
    console.error('[/api/me/photo] Missing required Azure AD environment variables (AZURE_AD_TENANT_ID / AZURE_AD_CLIENT_ID / AZURE_AD_CLIENT_SECRET)')
    return new NextResponse(null, { status: 500 })
  }

  const token = await getToken({ req: request })
  if (!token?.refreshToken) {
    return new NextResponse(null, { status: 401 })
  }

  try {
    // Acquire a Graph-scoped access token via refresh_token grant
    const graphTokenRes = await fetch(
      `https://login.microsoftonline.com/${tenantId}/oauth2/v2.0/token`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: new URLSearchParams({
          client_id: clientId,
          client_secret: clientSecret,
          grant_type: 'refresh_token',
          refresh_token: token.refreshToken,
          scope: 'https://graph.microsoft.com/User.Read',
        }),
      },
    )

    if (!graphTokenRes.ok) {
      const upstreamStatus = graphTokenRes.status
      const upstreamBody = await graphTokenRes.text().catch(() => '<unreadable>')
      // Log status + body (body will contain the OAuth error code, not secrets)
      console.error(`[/api/me/photo] Graph token exchange failed: HTTP ${upstreamStatus} — ${upstreamBody}`)
      // 400 (invalid_scope/bad_request) or 401/403 (auth/consent failure) →
      // surface as 401 so the caller knows auth is the issue.
      // 5xx or unexpected codes → 502 Bad Gateway (upstream failure).
      if (upstreamStatus === 400 || upstreamStatus === 401 || upstreamStatus === 403) {
        return new NextResponse(null, { status: 401 })
      }
      return new NextResponse(null, { status: 502 })
    }

    const { access_token: graphToken } = await graphTokenRes.json()

    const photoRes = await fetch(
      'https://graph.microsoft.com/v1.0/me/photo/$value',
      { headers: { Authorization: `Bearer ${graphToken}` } },
    )

    if (!photoRes.ok) {
      return new NextResponse(null, { status: 204 })
    }

    const photoBuffer = await photoRes.arrayBuffer()
    const contentType = photoRes.headers.get('content-type') || 'image/jpeg'

    return new NextResponse(Buffer.from(photoBuffer), {
      status: 200,
      headers: {
        'Content-Type': contentType,
        'Cache-Control': 'private, max-age=3600',
      },
    })
  } catch {
    return new NextResponse(null, { status: 204 })
  }
}
