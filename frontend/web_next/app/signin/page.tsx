'use client';

import React from 'react';
import { signIn, useSession } from 'next-auth/react';
import { useRouter } from 'next/navigation';
import { Button, Box, Typography } from '@mui/material';
import GoogleIcon from '@mui/icons-material/Google';

const SignInPage: React.FC = () => {
  const { data: session } = useSession();
  const router = useRouter();

  if (session) {
    router.push('/');
    return null;
  }

  const handleGoogleSignIn = async () => {
    await signIn('google');
  };

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', mt: 8 }}>
      <Typography variant="h4" gutterBottom>
        Sign In
      </Typography>
      <Button
        variant="contained"
        startIcon={<GoogleIcon />}
        onClick={handleGoogleSignIn}
        sx={{ mt: 2 }}
      >
        Sign in with Google
      </Button>
    </Box>
  );
};

export default SignInPage;
