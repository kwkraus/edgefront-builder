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
      // Persist the access_token to the token right after sign in
      if (account?.access_token) {
        token.accessToken = account.access_token
      }
      return token
    },
    async session({ session, token }) {
      // Forward accessToken to the client session
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
