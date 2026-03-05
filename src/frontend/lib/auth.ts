import { getServerSession as nextGetServerSession, type AuthOptions } from 'next-auth'
import AzureADProvider from 'next-auth/providers/azure-ad'

export const authOptions: AuthOptions = {
  providers: [
    AzureADProvider({
      clientId: process.env.AZURE_AD_CLIENT_ID!,
      clientSecret: process.env.AZURE_AD_CLIENT_SECRET!,
      tenantId: process.env.AZURE_AD_TENANT_ID!,
      authorization: {
        params: {
          scope: `openid profile email offline_access${process.env.AZURE_AD_CLIENT_ID ? ` api://${process.env.AZURE_AD_CLIENT_ID}/access_as_user` : ''}`,
        },
      },
    }),
  ],
  callbacks: {
    async jwt({ token, account }) {
      if (account?.access_token) {
        token.accessToken = account.access_token

        // Fetch Entra ID profile photo from Microsoft Graph
        try {
          const photoResponse = await fetch(
            'https://graph.microsoft.com/v1.0/me/photo/$value',
            { headers: { Authorization: `Bearer ${account.access_token}` } }
          )
          if (photoResponse.ok) {
            const arrayBuffer = await photoResponse.arrayBuffer()
            const base64 = Buffer.from(arrayBuffer).toString('base64')
            const contentType = photoResponse.headers.get('content-type') || 'image/jpeg'
            token.picture = `data:${contentType};base64,${base64}`
          }
        } catch {
          // No profile photo available — avatar will use initials fallback
        }
      }
      return token
    },
    async session({ session, token }) {
      session.accessToken = token.accessToken
      if (token.picture && session.user) {
        session.user.image = token.picture
      }
      return session
    },
  },
  pages: {
    signIn: '/login',
  },
}

/**
 * Wrapper around next-auth's getServerSession pre-bound to authOptions.
 * Use this in server components and API routes.
 */
export function getServerSession() {
  return nextGetServerSession(authOptions)
}
