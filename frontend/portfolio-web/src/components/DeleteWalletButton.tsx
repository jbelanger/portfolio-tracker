import React from 'react';
import { useNavigate } from 'react-router-dom';
import { IconButton } from '@mui/material';
import { Delete } from '@mui/icons-material';
import apiClient from '../api/axios';
import { Wallet } from '../types/Wallet';

interface DeleteWalletButtonProps {
  wallet: Wallet;
  onDelete: () => void;
  showSnackbar: (message: string, severity: 'success' | 'error') => void;
}

const DeleteWalletButton: React.FC<DeleteWalletButtonProps> = ({ wallet, onDelete, showSnackbar }) => {
  const navigate = useNavigate();

  const handleDeleteWallet = async () => {
    try {
      await apiClient.delete(`/portfolios/1/wallets/${wallet.id}`);
      onDelete();
      showSnackbar('Wallet deleted successfully.', 'success');
      navigate('/');
    } catch (error) {
      console.error('Error deleting wallet:', error);
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
