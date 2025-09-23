'use client';

import React, { useState, useEffect } from 'react';
import { Box, Typography, Button, IconButton, Snackbar, Alert, Paper, Grid, Divider } from '@mui/material';
import { Delete, Sync, Visibility, UploadFile } from '@mui/icons-material';
import { WalletAPI } from '@/app/api/WalletAPI';
import { Wallet } from '@/app/types/types';
import { useRouter } from 'next/navigation';

const WalletsPage: React.FC = () => {
  const [wallets, setWallets] = useState<Wallet[]>([]);
  const [snackbarOpen, setSnackbarOpen] = useState(false);
  const [snackbarMessage, setSnackbarMessage] = useState('');
  const [snackbarSeverity, setSnackbarSeverity] = useState<'success' | 'error'>('success');
  const router = useRouter();
  const portfolioId = 1;

  useEffect(() => {
    const fetchWallets = async () => {
      const result = await WalletAPI.fetchWallets(portfolioId);
      if (result.isOk()) {
          const data = result.value;
          setWallets(data);
      } else {
        console.error('Error fetching wallets:', result.error);
        showSnackbar('Error fetching wallets. Please try again.', 'error');
      }
    };

    fetchWallets();
  }, []);

  const showSnackbar = (message: string, severity: 'success' | 'error' = 'success') => {
    setSnackbarMessage(message);
    setSnackbarSeverity(severity);
    setSnackbarOpen(true);
  };

  const handleViewTransactions = (walletId: number) => {
    router.push(`/wallets/${walletId}/transactions`);
  };

  const handleImportTransactions = (walletId: number) => {
    // Logic to handle importing transactions
    showSnackbar(`Importing transactions for wallet ${walletId}`, 'success');
  };

  const handleSync = (walletId: number) => {
    // Logic to handle syncing wallet
    showSnackbar(`Syncing wallet ${walletId}`, 'success');
  };

  const handleDeleteWallet = async (walletId: number) => {
    if (confirm('Are you sure you want to delete this wallet? This action cannot be undone.')) {
      try {
        await WalletAPI.deleteWallet(portfolioId, walletId);
        setWallets(wallets.filter(wallet => wallet.id !== walletId));
        showSnackbar('Wallet deleted successfully.', 'success');
      } catch (error) {
        console.error('Error deleting wallet:', error);
        showSnackbar('Error deleting wallet. Please try again.', 'error');
      }
    }
  };

  return (
    <Box sx={{ padding: '20px' }}>
      <Typography variant="h4" gutterBottom>
        Wallets
      </Typography>

      <Grid container spacing={3}>
        {wallets.map(wallet => (
          <Grid size={{ xs: 12, md: 6, lg: 4 }} key={wallet.id}>
            <Paper elevation={3} sx={{ padding: '20px', position: 'relative' }}>
              <Typography variant="h6">{wallet.name}</Typography>
              <Divider sx={{ marginY: '10px' }} />
              <Typography variant="body2">Balance: {wallet.balance}</Typography>
              <Typography variant="body2">Number of Transactions: {wallet.transactionCount}</Typography>

              <Box sx={{ marginTop: '15px', display: 'flex', justifyContent: 'space-between' }}>
                <Button
                  startIcon={<Visibility />}
                  variant="contained"
                  color="primary"
                  onClick={() => handleViewTransactions(wallet.id)}
                >
                  View Transactions
                </Button>
                <IconButton
                  onClick={() => handleImportTransactions(wallet.id)}
                  color="primary"
                >
                  <UploadFile />
                </IconButton>
                <IconButton
                  onClick={() => handleSync(wallet.id)}
                  color="secondary"
                >
                  <Sync />
                </IconButton>
                <IconButton
                  onClick={() => handleDeleteWallet(wallet.id)}
                  color="error"
                >
                  <Delete />
                </IconButton>
              </Box>
            </Paper>
          </Grid>
        ))}
      </Grid>

      <Snackbar
        open={snackbarOpen}
        autoHideDuration={6000}
        onClose={() => setSnackbarOpen(false)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert onClose={() => setSnackbarOpen(false)} severity={snackbarSeverity} sx={{ width: '100%' }}>
          {snackbarMessage}
        </Alert>
      </Snackbar>
    </Box>
  );
};

export default WalletsPage;
