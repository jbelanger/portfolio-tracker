'use client';

import React from 'react';
import { useRouter } from 'next/navigation';
import { IconButton } from '@mui/material';
import { Delete } from '@mui/icons-material';
import { Wallet } from '../types/Wallet';
import { WalletAPI } from '../api/WalletAPI';
import { useSnackbar } from '../context/SnackbarContext';

interface DeleteWalletButtonProps {
  portfolioId: number;
  wallet: Wallet;
  onDelete: () => void;
}

const DeleteWalletButton: React.FC<DeleteWalletButtonProps> = ({ portfolioId, wallet, onDelete }) => {
  const router = useRouter();
  const { showSnackbar } = useSnackbar();

  const handleDeleteWallet = async () => {
    const result = await WalletAPI.deleteWallet(portfolioId, wallet.id);  
    if (result.isOk()) {
      onDelete();
      showSnackbar('Wallet deleted successfully.', 'success');
      router.push('/');
    } else {
      console.error('Error deleting wallet:', result.error);
      showSnackbar('Error deleting wallet. Please try again.', 'error');
    }
  };

  return (
    <IconButton onClick={handleDeleteWallet}>
      <Delete />
    </IconButton>
  );
};

export default DeleteWalletButton;
