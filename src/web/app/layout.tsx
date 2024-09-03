// app/layout.tsx
'use client';

import React, { useState, useEffect } from 'react';
import { Box, CssBaseline, Toolbar, AppBar, Typography, IconButton } from '@mui/material';
import { AccountCircle } from '@mui/icons-material';
import LeftMenu from './components/LeftMenu';
import { Wallet } from './types/types';
import { useParams } from 'next/navigation';
import { SnackbarProvider } from './context/SnackbarContext';
import CustomAppBar from './components/CustomAppBar';
import { SessionProvider } from "next-auth/react";

interface LayoutProps {
  children: React.ReactNode;
}

const Layout: React.FC<LayoutProps> = ({ children }) => {
  const [selectedWallet, setSelectedWallet] = useState<Wallet | null>(null);
  const [menuCollapsed, setMenuCollapsed] = useState(false);
  const portfolioId = 1;//useParams();

  const handleSelectWallet = (wallet: Wallet | null) => {
    setSelectedWallet(wallet);
  };

  const showSnackbar = (message: string, severity: 'success' | 'error') => {
    console.log(message, severity);
  };

  const handleWalletDeleted = () => {
    showSnackbar('Wallet deleted successfully.', 'success');
  };

  useEffect(() => {
    const resizeGrid = () => {
      window.dispatchEvent(new Event('resize'));
    };
    resizeGrid();
    window.addEventListener('resize', resizeGrid);

    return () => {
      window.removeEventListener('resize', resizeGrid);
    };
  }, [menuCollapsed]);

  return (
    <html lang="en">
      <head>
        <meta charSet="UTF-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1.0" />
        <title>Watch My Coins</title>
      </head>
      <body>
        <SessionProvider>
          <SnackbarProvider>
            <Box sx={{ display: 'flex', height: '100vh' }}>
              <CssBaseline />
              <CustomAppBar />
              {/* <AppBar position="fixed" sx={{ zIndex: (theme) => theme.zIndex.drawer + 1 }}>
              <Toolbar>
                <Typography variant="h6" noWrap sx={{ flexGrow: 1 }}>
                  Watch My Coins <small>Portfolio Tracker</small>
                </Typography>
                <IconButton edge="end" color="inherit">
                  <AccountCircle />
                </IconButton>
              </Toolbar>
            </AppBar> */}
              {/* <LeftMenu portfolioId={Number(portfolioId)} onWalletDeleted={handleWalletDeleted} onSelectWallet={handleSelectWallet} /> */}
              <Box
                component="main"
                sx={{
                  flexGrow: 1,
                  bgcolor: 'background.default',
                  p: 3,
                  transition: 'margin-left 0.3s',
                }}
              >
                <Toolbar />

                {children}

              </Box>
            </Box>
          </SnackbarProvider>
        </SessionProvider>
      </body>
    </html>
  );
};

export default Layout;
