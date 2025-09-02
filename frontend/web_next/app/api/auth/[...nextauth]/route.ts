import NextAuth from 'next-auth';
import GoogleProvider from 'next-auth/providers/google';

// Extend the Session type to include apiToken
declare module "next-auth" {
  interface Session {
    apiToken?: string;
  }
}

const handler = NextAuth({
  pages: {
    signIn: '/auth/signin',
  },
  session: {
    strategy: "jwt",
  },
  secret: process.env.NEXTAUTH_SECRET as string,
  providers: [
    GoogleProvider({
      clientId: process.env.GOOGLE_CLIENT_ID!,
      clientSecret: process.env.GOOGLE_CLIENT_SECRET!,
      authorization: {
        params: {
          authorizationUrl: 'https://accounts.google.com/o/oauth2/v2/auth?prompt=consent&access_type=offline&response_type=code',
          scope: 'openid profile email'
        }
      }
    }),
  ],
  callbacks: {
    async signIn({ user, account, profile, email, credentials }) {

      // console.log(account);
      // console.log(user);
      // console.log(email);
      // console.log(profile);
      try {
        if (account) {
          var idToken = account.id_token;

          // // Sign in with your API using the Google account details
          const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/auth/google-signin`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify({
              email: user.email,
              name: user.name,
              idToken: idToken
              // Add more fields as needed
            }),
          });

          if (response.ok) {
            const data = await response.json();
            account.apiToken = data.token;// Store your API token in the JWT                         
            return true;
          }
          else
            console.log(response?.statusText);
        }
      }
      catch (ex) {
        console.log(ex);
      }
      return false;
    },
    async jwt({ token, user, account, profile, isNewUser }) {
      // Persist the OAuth access_token to the token right after signin
      if (account) {
        token.apiToken = account.apiToken;
      }
      return token
    },
    async session({ session, token, user }) {
      // Send properties to the client, like an access_token from a provider.
      // session.accessToken = token.accessToken;
      // session.idToken = token.idToken;
      session.apiToken = typeof token.apiToken === "string" ? token.apiToken : undefined;

      return session
    }
  },
});

export { handler as GET, handler as POST }