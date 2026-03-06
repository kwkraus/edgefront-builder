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
  const token = await getToken({ req: request })
  if (!token?.refreshToken) {
    return new NextResponse(null, { status: 401 })
  }

  try {
    // Acquire a Graph-scoped access token via refresh_token grant
    const graphTokenRes = await fetch(
      `https://login.microsoftonline.com/${process.env.AZURE_AD_TENANT_ID}/oauth2/v2.0/token`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: new URLSearchParams({
          client_id: process.env.AZURE_AD_CLIENT_ID!,
          client_secret: process.env.AZURE_AD_CLIENT_SECRET!,
          grant_type: 'refresh_token',
          refresh_token: token.refreshToken,
          scope: 'https://graph.microsoft.com/User.Read',
        }),
      },
    )

    if (!graphTokenRes.ok) {
      return new NextResponse(null, { status: 204 })
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
