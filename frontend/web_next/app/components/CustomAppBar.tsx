// 'use client';

import React from 'react';
import AccountCircle from '@mui/icons-material/AccountCircle';
import Settings from '@mui/icons-material/Settings';
import CodeIcon from '@mui/icons-material/Code'; // Placeholder for logo
import { AppBar, Toolbar, Typography, Box, Button, IconButton } from '@mui/material';
import { useRouter } from 'next/navigation';
import Link from 'next/link';

const CustomAppBar: React.FC = () => {
  const router = useRouter();

  const handleNavigation = (path: string) => {
    router.push(path);
  };

  return (
    <AppBar position="fixed" sx={{ background: 'linear-gradient(90deg, #007ACC, #1E1E1E)', padding: '0 16px', boxShadow: 'none' }}>
      <Toolbar disableGutters>
        {/* Logo and App Name */}
        <Box display="flex" alignItems="center" sx={{ marginRight: "2rem;" }}>
          <CodeIcon sx={{ fontSize: 36, mr: 2, color: '#23D18B' }} /> {/* Use your custom logo here */}
          <Typography variant="h6" sx={{ fontFamily: '"Fira Code", monospace', color: '#D4D4D4', fontWeight: 'bold' }}>
            CodeFlow
          </Typography>
        </Box>
        <Box display="flex" sx={{ flexGrow: 3 }}>
          <Button color="inherit" onClick={() => handleNavigation('/dashboard')}>
            Dashboard
          </Button>
          <Button color="inherit" onClick={() => handleNavigation('/wallets')}>
            Wallets
          </Button>
          <Button color="inherit" onClick={() => handleNavigation('/transactions')}>
            Transactions
          </Button>
          <Button color="inherit" onClick={() => handleNavigation('/settings')}>
            Settings
          </Button>
          <IconButton edge="end" color="inherit">
            <AccountCircle />
          </IconButton>
        </Box>
        {/* Right-aligned Icons */}
        <Box display="flex" sx={{ flexGrow: 0 }}>
          <Link href="/signin">Sign In</Link>
          <IconButton size="large" edge="end" color="inherit" aria-label="settings" sx={{ color: '#D4D4D4' }}>
            <Settings />
          </IconButton>
          <IconButton size="large" edge="end" color="inherit" aria-label="account" sx={{ color: '#D4D4D4' }}>
            <AccountCircle />
          </IconButton>
        </Box>
      </Toolbar>
    </AppBar>
  );
};

export default CustomAppBar;
