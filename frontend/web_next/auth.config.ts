// import type { NextAuthConfig } from 'next-auth';
// import GoogleProvider from 'next-auth/providers/google';
 
// export const authConfig = {
//   pages: {
//     signIn: '/signin',
//   },
//   providers: [
//     GoogleProvider({
//       clientId: process.env.GOOGLE_CLIENT_ID!,
//       clientSecret: process.env.GOOGLE_CLIENT_SECRET!,
//     }),
//   ],
//   callbacks: {
//     async signIn({ user, account, profile }) {
//       // Send user information to your backend API
//       const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/auth/register-google`, {
//         method: 'POST',
//         headers: {
//           'Content-Type': 'application/json',
//         },
//         body: JSON.stringify({
//           email: user.email,
//           name: user.name,
//           googleId: account?.providerAccountId,
//         }),
//       });

//       if (response.ok) {
//         return true;
//       }

//       return false; // Return false to stop the sign-in process
//     },
//     async session({ session, token, user }) {
//       // Pass additional session data here if needed
//       return session;
//     },
//   }
// } satisfies NextAuthConfig;