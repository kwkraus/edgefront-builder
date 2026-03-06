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
        // Store refresh token for Graph API photo proxy route
        if (account.refresh_token) {
          token.refreshToken = account.refresh_token
        }
      }
      // Strip any stale base64 profile photo that was embedded in old JWTs
      // to prevent oversized cookies (HTTP 431).
      if (typeof token.picture === 'string' && token.picture.startsWith('data:')) {
        delete token.picture
      }
      return token
    },
    async session({ session, token }) {
      session.accessToken = token.accessToken
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
