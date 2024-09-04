// app/components/NewWalletDialog.tsx

'use client';

import React, { useState } from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions, Button, TextField } from '@mui/material';
import { useSnackbar } from '../context/SnackbarContext';

interface NewWalletDialogProps {
  open: boolean;
  onClose: () => void;
  onWalletCreated: () => void;
}

const NewWalletDialog: React.FC<NewWalletDialogProps> = ({ open, onClose, onWalletCreated }) => {
  const [walletName, setWalletName] = useState('');
  const { showSnackbar } = useSnackbar();

  const handleCreateWallet = async () => {
    if (!walletName.trim()) {
      showSnackbar('Wallet name cannot be empty.', 'error');
      return;
    }

    try {
      const response = await fetch('/api/wallets', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ name: walletName }),
      });

      if (response.ok) {
        showSnackbar('Wallet created successfully.', 'success');
        onWalletCreated();
        onClose();
      } else {
        showSnackbar('Error creating wallet. Please try again.', 'error');
      }
    } catch (error) {
      console.error('Error creating wallet:', error);
      showSnackbar('Error creating wallet. Please try again.', 'error');
    }
  };

  return (
    <Dialog open={open} onClose={onClose}>
      <DialogTitle>Create New Wallet</DialogTitle>
      <DialogContent>
        <TextField
          autoFocus
          fullWidth
          margin="dense"
          label="Wallet Name"
          value={walletName}
          onChange={(e) => setWalletName(e.target.value)}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} color="primary">Cancel</Button>
        <Button onClick={handleCreateWallet} color="primary">Create</Button>
      </DialogActions>
    </Dialog>
  );
};

export default NewWalletDialog;
